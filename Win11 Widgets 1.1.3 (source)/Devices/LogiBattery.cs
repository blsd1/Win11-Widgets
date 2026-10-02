// Win11 Widgets for Rainmeter - Logitech battery reader (HID++ 2.0)
//	Author - ketamine
//	Reads name and battery of wireless Logitech devices connected through a
//	LIGHTSPEED / Unifying / Bolt receiver (or directly over USB), without G HUB.
//	LogiBattery.Read() returns one line per device: logitech|<kind>|<name>|<percent>|<charging|discharging|full>
//	(used by DevicesHelper.cs)
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

public static class LogiBattery
{
	[StructLayout(LayoutKind.Sequential)]
	struct SP_DEVICE_INTERFACE_DATA { public int cbSize; public Guid InterfaceClassGuid; public int Flags; public IntPtr Reserved; }

	[DllImport("hid.dll")] static extern void HidD_GetHidGuid(out Guid g);
	[DllImport("setupapi.dll", CharSet = CharSet.Unicode)] static extern IntPtr SetupDiGetClassDevs(ref Guid g, IntPtr e, IntPtr h, int flags);
	[DllImport("setupapi.dll")] static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr info, ref Guid g, int idx, ref SP_DEVICE_INTERFACE_DATA d);
	[DllImport("setupapi.dll", CharSet = CharSet.Unicode)] static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref SP_DEVICE_INTERFACE_DATA d, IntPtr detail, int size, out int req, IntPtr info);
	[DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
	[DllImport("hid.dll", CharSet = CharSet.Unicode)] static extern bool HidD_GetProductString(SafeFileHandle h, byte[] buf, int len);
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr sec, uint disp, uint flags, IntPtr tmpl);

	const byte SHORT = 0x10;

	// Set by DevicesHelper: returns true when a newer helper is waiting to take over, so Read() stops early.
	public static Func<bool> Cancel;

	public static List<string> HidPaths()
	{
		var list = new List<string>();
		Guid g; HidD_GetHidGuid(out g);
		IntPtr set = SetupDiGetClassDevs(ref g, IntPtr.Zero, IntPtr.Zero, 0x12); // PRESENT | DEVICEINTERFACE
		var d = new SP_DEVICE_INTERFACE_DATA(); d.cbSize = Marshal.SizeOf(d);
		for (int i = 0; SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref g, i, ref d); i++)
		{
			int req; SetupDiGetDeviceInterfaceDetail(set, ref d, IntPtr.Zero, 0, out req, IntPtr.Zero);
			IntPtr buf = Marshal.AllocHGlobal(req);
			Marshal.WriteInt32(buf, IntPtr.Size == 8 ? 8 : 6);
			if (SetupDiGetDeviceInterfaceDetail(set, ref d, buf, req, out req, IntPtr.Zero))
				list.Add(Marshal.PtrToStringUni(buf + 4));
			Marshal.FreeHGlobal(buf);
		}
		SetupDiDestroyDeviceInfoList(set);
		return list;
	}

	// Requests are sent as short HID++ reports (0x10) on the short collection;
	// responses arrive as long reports (0x11) on the long collection, errors as short reports.
	class Channel : IDisposable
	{
		FileStream shortFs, longFs;
		Task<int> shortRead, longRead;
		byte[] shortBuf = new byte[64], longBuf = new byte[64];
		static int swid;

		static FileStream Open(string path, int len)
		{
			var h = CreateFile(path, 0xC0000000, 3, IntPtr.Zero, 3, 0x40000000, IntPtr.Zero); // RW, share RW, OPEN_EXISTING, OVERLAPPED
			if (h.IsInvalid) throw new IOException("open failed");
			return new FileStream(h, FileAccess.ReadWrite, len, true);
		}
		public Channel(string shortPath, string longPath)
		{
			shortFs = Open(shortPath, 7);
			try { longFs = Open(longPath, 20); }
			catch { shortFs.Dispose(); throw; } // don't leak the short collection's handle
		}
		public void Dispose() { shortFs.Dispose(); longFs.Dispose(); }

		public byte[] Request(byte dev, byte feat, byte fn, params byte[] args)
		{
			var msg = new byte[7];
			// A new software id for every request: a late reply to an earlier request (e.g. from a mouse that
			// was asleep and woke up slowly) can then never be mistaken for the reply to this one.
			swid = swid % 15 + 1;
			msg[0] = SHORT; msg[1] = dev; msg[2] = feat; msg[3] = (byte)((fn << 4) | swid);
			Array.Copy(args, 0, msg, 4, Math.Min(args.Length, 3));
			if (shortRead == null) shortRead = shortFs.ReadAsync(shortBuf, 0, 64);
			if (longRead == null) longRead = longFs.ReadAsync(longBuf, 0, 64);
			shortFs.Write(msg, 0, 7);
			var deadline = DateTime.UtcNow.AddMilliseconds(1500); // a sleeping mouse can take a moment to wake up
			while (DateTime.UtcNow < deadline)
			{
				// Clamped: the deadline can pass between the loop check and here, and a negative timeout throws.
				int which = Task.WaitAny(new Task[] { shortRead, longRead }, TimeSpan.FromMilliseconds(Math.Max(0, (deadline - DateTime.UtcNow).TotalMilliseconds)));
				if (which < 0) return null;
				byte[] buf = which == 0 ? shortBuf : longBuf;
				int n = which == 0 ? shortRead.Result : longRead.Result;
				var copy = new byte[Math.Max(n, 20)]; Array.Copy(buf, copy, n);
				if (which == 0) shortRead = shortFs.ReadAsync(shortBuf, 0, 64); else longRead = longFs.ReadAsync(longBuf, 0, 64);
				if (n < 5 || copy[1] != dev) continue;
				if ((copy[2] == 0x8F || copy[2] == 0xFF) && copy[3] == feat && copy[4] == msg[3]) return null; // HID++ error
				if (copy[2] == feat && copy[3] == msg[3]) return copy;
			}
			return null;
		}
		public int Feature(byte dev, ushort id)
		{
			var r = Request(dev, 0x00, 0, (byte)(id >> 8), (byte)id);
			return (r == null || r[4] == 0) ? -1 : r[4];
		}
	}

	static string Name(Channel c, byte dev)
	{
		int f = c.Feature(dev, 0x0005);
		if (f < 0) return null;
		var r = c.Request(dev, (byte)f, 0);
		if (r == null) return null;
		int len = r[4];
		var sb = new StringBuilder();
		while (sb.Length < len)
		{
			var p = c.Request(dev, (byte)f, 1, (byte)sb.Length);
			if (p == null) break;
			for (int i = 4; i < 20 && sb.Length < len; i++) sb.Append((char)p[i]);
		}
		return sb.ToString().Trim('\0', ' ');
	}

	static string Kind(Channel c, byte dev)
	{
		int f = c.Feature(dev, 0x0005);
		if (f < 0) return "device";
		var r = c.Request(dev, (byte)f, 2);
		if (r == null) return "device";
		switch (r[4]) { case 0: return "keyboard"; case 3: case 4: case 5: return "mouse"; case 8: return "headset"; default: return "device"; }
	}

	// Approximate Li-ion discharge curve for BATTERY_VOLTAGE (0x1001) devices.
	static int VoltageToPercent(int mv)
	{
		int[,] curve = { { 4186, 100 }, { 4067, 90 }, { 3989, 80 }, { 3922, 70 }, { 3859, 60 }, { 3811, 50 }, { 3778, 40 }, { 3751, 30 }, { 3717, 20 }, { 3671, 10 }, { 3500, 0 } };
		if (mv >= curve[0, 0]) return 100;
		for (int i = 1; i < curve.GetLength(0); i++)
			if (mv >= curve[i, 0])
				return curve[i, 1] + (mv - curve[i, 0]) * (curve[i - 1, 1] - curve[i, 1]) / (curve[i - 1, 0] - curve[i, 0]);
		return 0;
	}

	// Returns percent and charging state, or null if the device has no battery feature.
	static Tuple<int, string> Battery(Channel c, byte dev)
	{
		int f = c.Feature(dev, 0x1004); // UNIFIED_BATTERY
		if (f >= 0)
		{
			// Devices with this feature are read only through it. (Falling back to the older battery
			// features below while the device was asleep sent them to this feature by mistake, which
			// answered with its capabilities: read as "15%, charging".)
			// Read until two consecutive valid readings agree.
			byte[] last = null;
			for (int attempt = 0; attempt < 4; attempt++)
			{
				var r = c.Request(dev, (byte)f, 1); // getStatus: charge %, level, charging status, external power
				if (r == null || r[4] > 100 || r[6] > 4) continue;
				if (last != null && last[4] == r[4] && last[6] == r[6])
				{
					string s = r[6] == 1 || r[6] == 2 ? "charging" : r[6] == 3 ? "full" : "discharging";
					return Tuple.Create((int)r[4], s);
				}
				last = r;
			}
			return null; // asleep / not answering: the helper keeps showing the last reading for a while
		}
		f = c.Feature(dev, 0x1000); // BATTERY_STATUS
		if (f >= 0)
		{
			var r = c.Request(dev, (byte)f, 0);
			if (r != null)
			{
				string s = r[6] == 1 || r[6] == 2 ? "charging" : r[6] == 3 ? "full" : "discharging";
				return Tuple.Create((int)r[4], s);
			}
		}
		f = c.Feature(dev, 0x1001); // BATTERY_VOLTAGE
		if (f >= 0)
		{
			var r = c.Request(dev, (byte)f, 0);
			if (r != null)
			{
				int mv = (r[4] << 8) | r[5];
				bool charging = (r[6] & 0x80) != 0;
				return Tuple.Create(VoltageToPercent(mv), charging ? "charging" : "discharging");
			}
		}
		return null;
	}

	// Logitech headsets charging over their USB cable (e.g. G733) show up as a separate
	// "<name> Battery Charger" device and are unreachable over wireless while cabled.
	// The level isn't available there, so they are reported as charging without a percentage.
	static List<string> Chargers(List<string> paths)
	{
		var output = new List<string>();
		var seen = new HashSet<string>();
		foreach (var path in paths)
		{
			string p = path.ToLowerInvariant();
			if (!p.Contains("vid_046d")) continue;
			var h = CreateFile(path, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
			if (h.IsInvalid) continue;
			var buf = new byte[256];
			string product = HidD_GetProductString(h, buf, buf.Length) ? Encoding.Unicode.GetString(buf).TrimEnd('\0') : "";
			h.Dispose();
			int i = product.IndexOf(" Battery Charger", StringComparison.OrdinalIgnoreCase);
			if (i <= 0) continue;
			string name = product.Substring(0, i).Trim();
			if (!name.StartsWith("Logitech", StringComparison.OrdinalIgnoreCase)) name = "Logitech " + name;
			if (seen.Add(name)) output.Add(string.Format("logitech|headset|{0}|-1|charging", name.Replace("|", "")));
		}
		return output;
	}

	public static List<string> Read()
	{
		var output = new List<string>();
		var seen = new HashSet<string>();
		var paths = HidPaths();
		foreach (var path in paths)
		{
			string p = path.ToLowerInvariant();
			// HID++ short-report collection of a Logitech receiver/device; its long collection is the matching "&col02".
			if (!p.Contains("vid_046d") || !p.Contains("&col01")) continue;
			string longPath = paths.Find(x => x.ToLowerInvariant() == p.Replace("&col01#", "&col02#").Replace("&0000#", "&0001#"));
			if (longPath == null) continue;
			Channel c;
			try { c = new Channel(path, longPath); } catch { continue; }
			using (c)
			{
				// Receivers: devices in slots 1-6. Devices connected directly by cable: index 0xFF.
				foreach (byte dev in new byte[] { 1, 2, 3, 4, 5, 6, 0xFF })
				{
					if (Cancel != null && Cancel()) return output; // the caller discards this partial list
					// A failed read (e.g. receiver unplugged mid-read) skips this device instead of losing all of them.
					try
					{
						var bat = Battery(c, dev);
						if (bat == null) continue;
						string name = Name(c, dev) ?? "Logitech device";
						if (!name.StartsWith("Logitech", StringComparison.OrdinalIgnoreCase)) name = "Logitech " + name;
						string kind = Kind(c, dev);
						if (kind == "headset") continue; // reported by HeadsetControl
						if (!seen.Add(name)) continue;
						output.Add(string.Format("logitech|{0}|{1}|{2}|{3}", kind, name.Replace("|", ""), bat.Item1, bat.Item2));
					}
					catch { }
				}
			}
		}
		output.AddRange(Chargers(paths));
		return output;
	}
}
