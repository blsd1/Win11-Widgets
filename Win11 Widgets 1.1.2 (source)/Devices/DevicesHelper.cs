// Win11 Widgets for Rainmeter - Devices helper
//	Author - ketamine
//
//	Small background program for the Devices widget (compiled on first use by deviceBattery.ps1).
//	Writes one line per connected wireless device to the output file:
//		<brand>|<kind>|<name>|<percent>|<charging|discharging|full|estimated|off>   (percent -1 = unknown)
//	- Logitech mice/keyboards via HID++ (LogiBattery.cs), no G HUB needed
//	- Wireless headsets via HeadsetControl
//	Batteries are read every --interval seconds; plugging a device in or out is picked up within
//	~2 seconds (cheap HID device list comparison). Single instance; exits when the Devices widget stops
//	updating its heartbeat file (widget unloaded or Rainmeter closed).
//
//	Arguments: --out <file> --interval <seconds> --charge-minutes <minutes> --headsetcontrol <exe>
//	           --print   (print the devices once and exit)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32.SafeHandles;

public static class DevicesHelper
{
	[DllImport("hid.dll", CharSet = CharSet.Unicode)] static extern bool HidD_GetProductString(SafeFileHandle h, byte[] buf, int len);
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr sec, uint disp, uint flags, IntPtr tmpl);

	static string outFile, headsetControl, stateFile;
	static int interval = 30, chargeMinutes = 180;
	static readonly Dictionary<string, string> usbNames = new Dictionary<string, string>();

	class Level { public int Value; public DateTime Time; }
	// last: last real reading while off the charging cable; charge: level + time when the cable was plugged in.
	static readonly Dictionary<string, Level> last = new Dictionary<string, Level>();
	static readonly Dictionary<string, Level> charge = new Dictionary<string, Level>();

	public static void Main(string[] args)
	{
		for (int i = 0; i < args.Length - 1; i++)
		{
			switch (args[i])
			{
				case "--out": outFile = args[i + 1]; break;
				case "--interval": interval = Math.Max(5, int.Parse(args[i + 1])); break;
				case "--charge-minutes": chargeMinutes = Math.Max(10, int.Parse(args[i + 1])); break;
				case "--headsetcontrol": headsetControl = args[i + 1]; break;
			}
		}
		string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Win11Widgets");
		Directory.CreateDirectory(dir);
		stateFile = Path.Combine(dir, "deviceLevels.txt");
		LoadState();

		if (args.Contains("--print")) { foreach (var line in GetDevices()) Console.WriteLine(line); return; }

		// A newer helper (e.g. after a skin refresh) asks the running one to exit, then takes over.
		var exitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\Win11Widgets-Devices-Exit");
		var refreshEvent = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\Win11Widgets-Devices-Refresh");
		var mutex = new Mutex(false, @"Local\Win11Widgets-Devices");
		exitEvent.Set();
		try { if (!mutex.WaitOne(5000)) return; }
		catch (AbandonedMutexException) { }
		exitEvent.Reset();

		// devices.lua touches <out>.alive every ~10 s while the widget is loaded.
		string alive = outFile + ".alive";
		DateTime started = DateTime.UtcNow;
		string lastText = null, lastDevices = DeviceSignature();
		DateTime nextRead = DateTime.MinValue;
		while (RainmeterRunning() && (DateTime.UtcNow - started < TimeSpan.FromSeconds(60) || (File.Exists(alive) && DateTime.UtcNow - File.GetLastWriteTimeUtc(alive) < TimeSpan.FromSeconds(60))))
		{
			if (DateTime.UtcNow >= nextRead)
			{
				string text = string.Join("\n", GetDevices());
				if (text != lastText && WriteOutput(text)) lastText = text;
				nextRead = DateTime.UtcNow.AddSeconds(interval);
			}
			// Wait 2 s; read right away when asked to refresh or when devices were plugged in/removed.
			int signal = WaitHandle.WaitAny(new WaitHandle[] { exitEvent, refreshEvent }, 2000);
			if (signal == 0) break;
			string devices = DeviceSignature();
			if (signal == 1 || devices != lastDevices)
			{
				if (devices != lastDevices) Thread.Sleep(1500); // let the driver finish setting the device up
				lastDevices = DeviceSignature();
				nextRead = DateTime.MinValue;
			}
		}
		mutex.ReleaseMutex();
	}

	// Cheap fingerprint of the connected HID devices.
	static string DeviceSignature()
	{
		try { return string.Join(";", LogiBattery.HidPaths()); } catch { return ""; }
	}

	static bool WriteOutput(string text)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(outFile));
			string tmp = outFile + ".tmp";
			File.WriteAllText(tmp, text);
			if (File.Exists(outFile)) File.Replace(tmp, outFile, null); else File.Move(tmp, outFile);
			return true;
		}
		catch { return false; } // briefly in use by Rainmeter: try again next time
	}

	// Name Windows shows for a USB device (e.g. "G733 Gaming Headset"), from its HID product string.
	static string UsbName(string vid, string pid)
	{
		string key = vid + "/" + pid;
		if (usbNames.ContainsKey(key)) return usbNames[key];
		string match = ("vid_" + vid.Substring(2) + "&pid_" + pid.Substring(2)).ToLowerInvariant();
		string name = null;
		foreach (var path in LogiBattery.HidPaths())
		{
			if (!path.ToLowerInvariant().Contains(match)) continue;
			using (var h = CreateFile(path, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero))
			{
				if (h.IsInvalid) continue;
				var buf = new byte[256];
				if (HidD_GetProductString(h, buf, buf.Length)) { name = Encoding.Unicode.GetString(buf).TrimEnd('\0'); break; }
			}
		}
		usbNames[key] = name;
		return name;
	}

	static List<string> HeadsetLines()
	{
		var lines = new List<string>();
		if (headsetControl == null || !File.Exists(headsetControl)) return lines;
		string json;
		try
		{
			var psi = new ProcessStartInfo(headsetControl, "-b -o json")
			{ UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
			using (var p = Process.Start(psi))
			{
				json = p.StandardOutput.ReadToEnd();
				p.WaitForExit(10000);
			}
		}
		catch { return lines; }

		// One block per device: from each device-level "status" (not the battery "BATTERY_..." status) to the next.
		var blocks = Regex.Split(json, "\\{\\s*\"status\":\\s*\"(?!BATTERY)").Skip(1);
		foreach (var b in blocks)
		{
			string device = Field(b, "device"), vendor = Field(b, "vendor");
			var battery = Regex.Match(b, "\"battery\":\\s*\\{\\s*\"status\":\\s*\"(\\w+)\",\\s*\"level\":\\s*(-?\\d+)");
			if (device == null || !battery.Success) continue;
			string name = device;
			// Some drivers cover several models (e.g. "Logitech G633/G635/G733/G933/G935"): use the name Windows shows instead.
			if (name.Contains("/"))
			{
				string usb = UsbName(Field(b, "id_vendor") ?? "0x0000", Field(b, "id_product") ?? "0x0000");
				if (!string.IsNullOrEmpty(usb)) name = vendor + " " + usb;
			}
			int level = int.Parse(battery.Groups[2].Value);
			string state = battery.Groups[1].Value == "BATTERY_AVAILABLE" ? "discharging"
				: battery.Groups[1].Value == "BATTERY_CHARGING" ? "charging" : "off";
			if (state == "off") level = -1;
			lines.Add(string.Format("{0}|headset|{1}|{2}|{3}", (vendor ?? "").ToLowerInvariant(), name.Replace("|", ""), level, state));
		}
		return lines;
	}

	static string Field(string block, string name)
	{
		var m = Regex.Match(block, "\"" + name + "\":\\s*\"([^\"]*)\"");
		return m.Success ? m.Groups[1].Value : null;
	}

	// Model number shared by all names of the same device, e.g. "G733" in "Logitech G733 Gaming Headset".
	static string Key(string kind, string name)
	{
		var model = name.Split(' ').FirstOrDefault(w => w.Any(char.IsDigit) && w.Any(char.IsLetter));
		return kind + "/" + (model ?? name);
	}

	static List<string> GetDevices()
	{
		var lines = new List<string>();
		try { lines.AddRange(LogiBattery.Read()); } catch { }
		lines.AddRange(HeadsetLines());

		// Merge entries of the same device (e.g. G733 on its cable: reported through the receiver and as
		// "... Battery Charger"). Known percentage wins; charging if any entry is charging.
		var merged = new List<string[]>();
		var byKey = new Dictionary<string, string[]>();
		var onCable = new HashSet<string>();
		var wireless = new Dictionary<string, string>();
		foreach (var line in lines)
		{
			var p = line.Split('|');
			if (p.Length < 5) continue;
			string key = Key(p[1], p[2]);
			if (p[3] == "-1" && p[4] == "charging") onCable.Add(key); else wireless[key] = p[4];
			string[] m;
			if (!byKey.TryGetValue(key, out m)) { byKey[key] = p; merged.Add(p); continue; }
			if (int.Parse(m[3]) < 0 && int.Parse(p[3]) >= 0) { m[3] = p[3]; m[2] = p[2]; }
			if (p[4] == "charging" || p[4] == "full") m[4] = "charging";
			else if (m[4] == "off" && p[4] != "off") m[4] = p[4];
		}

		// Headsets on their charging cable report a voltage the charger holds at maximum (always ~100%),
		// so their level is estimated from the last real reading before plugging in + time charging.
		var now = DateTime.UtcNow;
		var output = new List<string>();
		bool changed = false;
		foreach (var m in merged)
		{
			string key = Key(m[1], m[2]);
			if (onCable.Contains(key))
			{
				string w;
				if (wireless.TryGetValue(key, out w) && w == "discharging") { m[3] = "100"; m[4] = "full"; }
				else
				{
					if (!charge.ContainsKey(key) && last.ContainsKey(key))
					{
						charge[key] = new Level { Value = last[key].Value, Time = now };
						changed = true;
					}
					Level c;
					if (charge.TryGetValue(key, out c))
					{
						double minutes = (now - c.Time).TotalMinutes;
						m[3] = Math.Min(99, (int)(c.Value + minutes * 100 / chargeMinutes)).ToString();
						m[4] = "estimated";
					}
					else { m[3] = "-1"; m[4] = "charging"; } // no earlier reading to start from
				}
			}
			else
			{
				if (charge.Remove(key)) changed = true;
				int level = int.Parse(m[3]);
				Level l;
				if (level >= 0 && m[4] != "off" && (!last.TryGetValue(key, out l) || l.Value != level))
				{
					last[key] = new Level { Value = level, Time = now };
					changed = true;
				}
			}
			output.Add(string.Join("|", m));
		}
		if (changed) SaveState();

		// A device that doesn't answer for a moment (e.g. a mouse waking up from sleep) keeps its last
		// reading for up to 2 minutes instead of disappearing; a device that was switched off goes away after that.
		var current = new HashSet<string>();
		foreach (var line in output)
		{
			var p = line.Split('|');
			string key = Key(p[1], p[2]);
			current.Add(key);
			recent[key] = new KeyValuePair<string, DateTime>(line, now);
		}
		foreach (var kv in recent.ToList())
		{
			if (current.Contains(kv.Key)) continue;
			if (now - kv.Value.Value < TimeSpan.FromMinutes(2)) output.Add(kv.Value.Key);
			else recent.Remove(kv.Key);
		}
		return output;
	}

	// Last line and time each device was read (see end of GetDevices).
	static readonly Dictionary<string, KeyValuePair<string, DateTime>> recent = new Dictionary<string, KeyValuePair<string, DateTime>>();

	// State file lines: last|<key>|<level>|<utc ticks>  or  charge|<key>|<level>|<utc ticks>
	static void LoadState()
	{
		try
		{
			foreach (var line in File.ReadAllLines(stateFile))
			{
				var p = line.Split('|');
				if (p.Length != 4) continue;
				var l = new Level { Value = int.Parse(p[2]), Time = new DateTime(long.Parse(p[3]), DateTimeKind.Utc) };
				(p[0] == "charge" ? charge : last)[p[1]] = l;
			}
		}
		catch { }
	}

	static void SaveState()
	{
		try
		{
			var lines = last.Select(kv => "last|" + kv.Key + "|" + kv.Value.Value + "|" + kv.Value.Time.Ticks)
				.Concat(charge.Select(kv => "charge|" + kv.Key + "|" + kv.Value.Value + "|" + kv.Value.Time.Ticks));
			File.WriteAllLines(stateFile, lines.ToArray());
		}
		catch { }
	}
	// True while Rainmeter runs (checked at most every 10 s). When Rainmeter closes, e.g. for a Skin Installer
	// update, the helper exits right away instead of waiting for the heartbeat to go stale.
	static DateTime nextRainmeterCheck;
	static bool rainmeterRunning = true;
	static bool RainmeterRunning()
	{
		if (DateTime.UtcNow < nextRainmeterCheck) return rainmeterRunning;
		nextRainmeterCheck = DateTime.UtcNow.AddSeconds(10);
		try { rainmeterRunning = System.Diagnostics.Process.GetProcessesByName("Rainmeter").Length > 0; } catch { }
		return rainmeterRunning;
	}
}
