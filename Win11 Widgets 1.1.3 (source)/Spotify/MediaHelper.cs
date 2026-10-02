// Win11 Widgets for Rainmeter - Media helper
//	Author - ketamine
//
//	Small background program for the Spotify widget (compiled on first use by mediaHelper.ps1).
//	Reads the Windows media session of Spotify (the same info the Windows 11 media flyout shows) and
//	writes it to the output file:
//		updated=<unix time>, status=playing|paused|none, title=, artist=, album=, position=<s>, duration=<s>, cover=<png path>
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
	static byte[] coverBytes;
	static TimeSpan coverSince;
	static readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

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
		// Grace window (no heartbeat needed) on start and after the PC wakes up: the heartbeat file is stale
		// after sleep/hibernate until media.lua touches it again (every ~10 s).
		TimeSpan graceStart = clock.Elapsed, lastPass = clock.Elapsed, lastWrite = TimeSpan.Zero;
		DateTime lastPassUtc = DateTime.UtcNow;
		string lastText = null;
		int failures = 0;
		while (RainmeterRunning())
		{
			// A loop pass takes ~1 s; a much longer gap means the PC was asleep. Checked on both clocks, as the
			// monotonic one may or may not count time spent asleep.
			TimeSpan gap = clock.Elapsed - lastPass, wallGap = DateTime.UtcNow - lastPassUtc;
			if (gap > TimeSpan.FromSeconds(15) || wallGap > TimeSpan.FromSeconds(15)) graceStart = clock.Elapsed;
			lastPass = clock.Elapsed;
			lastPassUtc = DateTime.UtcNow;
			if (clock.Elapsed - graceStart >= TimeSpan.FromSeconds(60) &&
				!(File.Exists(aliveFile) && DateTime.UtcNow - File.GetLastWriteTimeUtc(aliveFile) < TimeSpan.FromSeconds(60)))
				break;

			GlobalSystemMediaTransportControlsSession session = null;
			try { session = FindSession(manager); } catch { }

			if (File.Exists(cmdFile))
			{
				RunCommand(session);
				try { session = FindSession(manager); } catch { session = null; }
			}

			// A failed read (e.g. the player is busy switching tracks) keeps the last good state for a few
			// passes instead of flashing "none", which would also reset the play/pause icon.
			string text;
			try { text = Snapshot(session); failures = 0; }
			catch { text = lastText != null && ++failures <= 5 ? lastText : "status=none"; }
			// Also rewritten every 5 s with a fresh "updated=" time, so media.lua can tell the helper is alive
			// (and restart it if not) even while nothing changes, e.g. when paused.
			if ((text != lastText || clock.Elapsed - lastWrite >= TimeSpan.FromSeconds(5)) &&
				Write("updated=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "\n" + text))
			{
				lastText = text;
				lastWrite = clock.Elapsed;
			}

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

		// Cover art: saved only when it changes, alternating file names so Rainmeter reloads it.
		// The player often updates the title before the thumbnail, so it is re-read for a few seconds after a
		// track change.
		string key = props.Title + "\n" + props.Artist;
		if (key != lastKey) { lastKey = key; coverSince = clock.Elapsed; }
		if (clock.Elapsed - coverSince < TimeSpan.FromSeconds(5))
		{
			if (props.Thumbnail == null) { cover = ""; coverBytes = null; }
			else
			{
				try
				{
					byte[] bytes;
					using (var stream = Wait(props.Thumbnail.OpenReadAsync()))
					using (var reader = new DataReader(stream))
					{
						uint n = Wait(reader.LoadAsync((uint)stream.Size));
						bytes = new byte[n];
						reader.ReadBytes(bytes);
					}
					if (bytes.Length > 0 && (coverBytes == null || !bytes.SequenceEqual(coverBytes)))
					{
						coverSlot = 1 - coverSlot;
						string path = Path.Combine(Path.GetDirectoryName(outFile), "cover" + coverSlot + ".png");
						File.WriteAllBytes(path, bytes);
						cover = path;
						coverBytes = bytes;
					}
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
