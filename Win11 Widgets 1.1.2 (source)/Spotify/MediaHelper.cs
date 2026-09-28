// Win11 Widgets for Rainmeter - Media helper
//	Author - ketamine
//
//	Small background program for the Spotify widget (compiled on first use by mediaHelper.ps1).
//	Reads the Windows media session of Spotify (the same info the Windows 11 media flyout shows) and
//	writes it to the output file:
//		status=playing|paused|none, title=, artist=, album=, position=<s>, duration=<s>, cover=<png path>
//	Commands (playpause / next / previous) are written by the widget to <out>.cmd and are sent to the
//	same Spotify session (not to whatever player Windows considers current).
//	Single instance; exits when the widget stops updating its heartbeat file <out>.alive.
//
//	Arguments: --out <file> --app <app id part, e.g. Spotify, or "any" for the current media session>
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Windows.Foundation;
using Windows.Media.Control;
using Windows.Storage.Streams;

public static class MediaHelper
{
	static string outFile, cmdFile, aliveFile, app = "Spotify";
	static string lastKey, cover = "";
	static int coverSlot;

	static T Wait<T>(IAsyncOperation<T> op)
	{
		while (op.Status == AsyncStatus.Started) Thread.Sleep(5);
		return op.GetResults();
	}

	public static void Main(string[] args)
	{
		for (int i = 0; i < args.Length - 1; i++)
		{
			if (args[i] == "--out") outFile = args[i + 1];
			if (args[i] == "--app") app = args[i + 1];
		}
		string dir = Path.GetDirectoryName(outFile);
		Directory.CreateDirectory(dir);
		cmdFile = outFile + ".cmd";
		aliveFile = outFile + ".alive";

		// A newer helper (e.g. after a skin refresh) asks the running one to exit, then takes over.
		var exitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\Win11Widgets-Media-Exit");
		var mutex = new Mutex(false, @"Local\Win11Widgets-Media");
		exitEvent.Set();
		try { if (!mutex.WaitOne(5000)) return; }
		catch (AbandonedMutexException) { }
		exitEvent.Reset();

		// React to button presses right away.
		var commandEvent = new AutoResetEvent(false);
		var watcher = new FileSystemWatcher(dir, Path.GetFileName(cmdFile)) { NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName };
		watcher.Created += delegate { commandEvent.Set(); };
		watcher.Changed += delegate { commandEvent.Set(); };
		watcher.EnableRaisingEvents = true;

		var manager = Wait(GlobalSystemMediaTransportControlsSessionManager.RequestAsync());
		DateTime started = DateTime.UtcNow;
		string lastText = null;
		while (RainmeterRunning() && (DateTime.UtcNow - started < TimeSpan.FromSeconds(60) ||
			(File.Exists(aliveFile) && DateTime.UtcNow - File.GetLastWriteTimeUtc(aliveFile) < TimeSpan.FromSeconds(60))))
		{
			GlobalSystemMediaTransportControlsSession session = null;
			try { session = FindSession(manager); } catch { }

			if (File.Exists(cmdFile)) { RunCommand(session); session = FindSession(manager); }

			string text;
			try { text = Snapshot(session); } catch { text = "status=none"; }
			if (text != lastText && Write(text)) lastText = text;

			int signal = WaitHandle.WaitAny(new WaitHandle[] { exitEvent, commandEvent }, 1000);
			if (signal == 0) break;
		}
		mutex.ReleaseMutex();
	}

	static GlobalSystemMediaTransportControlsSession FindSession(GlobalSystemMediaTransportControlsSessionManager manager)
	{
		if (app.Equals("any", StringComparison.OrdinalIgnoreCase)) return manager.GetCurrentSession();
		return manager.GetSessions().FirstOrDefault(s =>
			s.SourceAppUserModelId.IndexOf(app, StringComparison.OrdinalIgnoreCase) >= 0);
	}

	static void RunCommand(GlobalSystemMediaTransportControlsSession session)
	{
		string command = "";
		try { command = File.ReadAllText(cmdFile).Trim().ToLowerInvariant(); File.Delete(cmdFile); } catch { return; }
		if (session == null) return;
		try
		{
			if (command == "playpause") Wait(session.TryTogglePlayPauseAsync());
			else if (command == "next") Wait(session.TrySkipNextAsync());
			else if (command == "previous") Wait(session.TrySkipPreviousAsync());
			Thread.Sleep(300); // give the player a moment to update its state
		}
		catch { }
	}

	static string Snapshot(GlobalSystemMediaTransportControlsSession session)
	{
		if (session == null) return "status=none";
		var props = Wait(session.TryGetMediaPropertiesAsync());
		var playback = session.GetPlaybackInfo();
		var timeline = session.GetTimelineProperties();
		bool playing = playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

		// Cover art: saved only when the song changes, alternating file names so Rainmeter reloads it.
		string key = props.Title + "\n" + props.Artist;
		if (key != lastKey)
		{
			lastKey = key;
			cover = "";
			if (props.Thumbnail != null)
			{
				try
				{
					var stream = Wait(props.Thumbnail.OpenReadAsync());
					var reader = new DataReader(stream);
					uint n = Wait(reader.LoadAsync((uint)stream.Size));
					var bytes = new byte[n];
					reader.ReadBytes(bytes);
					coverSlot = 1 - coverSlot;
					string path = Path.Combine(Path.GetDirectoryName(outFile), "cover" + coverSlot + ".png");
					File.WriteAllBytes(path, bytes);
					cover = path;
				}
				catch { }
			}
		}

		// The player only reports its position now and then; count forward while playing.
		double duration = (timeline.EndTime - timeline.StartTime).TotalSeconds;
		double position = timeline.Position.TotalSeconds;
		if (playing) position += (DateTimeOffset.Now - timeline.LastUpdatedTime).TotalSeconds;
		position = Math.Max(0, duration > 0 ? Math.Min(position, duration) : position);

		var sb = new StringBuilder();
		sb.Append("status=").Append(playing ? "playing" : "paused").Append('\n');
		sb.Append("title=").Append(Clean(props.Title)).Append('\n');
		sb.Append("artist=").Append(Clean(props.Artist)).Append('\n');
		sb.Append("album=").Append(Clean(props.AlbumTitle)).Append('\n');
		sb.Append("position=").Append((int)position).Append('\n');
		sb.Append("duration=").Append((int)duration).Append('\n');
		sb.Append("cover=").Append(cover);
		return sb.ToString();
	}

	static string Clean(string s) { return (s ?? "").Replace('\n', ' ').Replace('\r', ' ').Replace('"', '\''); }

	// Writes to a temp file and moves it into place, so Rainmeter never reads a half-written file.
	static bool Write(string text)
	{
		try
		{
			string tmp = outFile + ".tmp";
			File.WriteAllText(tmp, text, new UTF8Encoding(false));
			if (File.Exists(outFile)) File.Replace(tmp, outFile, null); else File.Move(tmp, outFile);
			return true;
		}
		catch { return false; }
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
