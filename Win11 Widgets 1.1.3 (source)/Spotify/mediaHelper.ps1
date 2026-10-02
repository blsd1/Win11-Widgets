# Win11 Widgets for Rainmeter - Media helper launcher
#	Author - ketamine
#
#	Compiles MediaHelper.cs into a small background program (once, cached in %LOCALAPPDATA%\Win11Widgets;
#	rebuilt when the source changes) with the C# compiler that comes with Windows, and starts it.
#	The helper writes what Spotify is playing to %TEMP%\Win11Widgets\media.txt, which media.lua reads.
#
#	Usage: mediaHelper.ps1 [-App Spotify]     (-App any = whatever player Windows shows as current)

param([string]$App = 'Spotify')

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $env:TEMP 'Win11Widgets\media.txt'

$source = Join-Path $here 'MediaHelper.cs'
$hash = (Get-FileHash $source -Algorithm SHA256).Hash.Substring(0, 12)
$cacheDir = Join-Path $env:LOCALAPPDATA 'Win11Widgets'
$exe = Join-Path $cacheDir "MediaHelper-$hash.exe"
if (-not (Test-Path $exe)) {
	# Several Spotify skins can start this at once, so only one of them builds; the rest wait for it.
	$mutex = New-Object Threading.Mutex($false, 'Local\Win11Widgets-Media-Build')
	try { [void]$mutex.WaitOne() } catch [Threading.AbandonedMutexException] { }
	try {
		if (-not (Test-Path $exe)) {
			New-Item -ItemType Directory -Force $cacheDir | Out-Null
			# The Windows media session API is described by the .winmd files that ship with Windows.
			$framework = [Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()
			$winmd = Join-Path $env:SystemRoot 'System32\WinMetadata'
			$references = @(
				(Join-Path $winmd 'Windows.Media.winmd'),
				(Join-Path $winmd 'Windows.Foundation.winmd'),
				(Join-Path $winmd 'Windows.Storage.winmd'),
				(Join-Path $framework 'System.Runtime.dll')
			) | ForEach-Object { "/r:$_" }
			# Built under a temporary name and renamed when done, so a half-written exe is never started.
			$tmp = "$exe.$PID.tmp"
			& (Join-Path $framework 'csc.exe') /nologo /target:winexe /optimize "/out:$tmp" $references $source | Out-Null
			if (-not (Test-Path $tmp)) { throw 'MediaHelper could not be compiled.' }
			Move-Item -Force $tmp $exe
		}
	}
	finally { $mutex.ReleaseMutex() }
}
# Remove older builds on every start (ones still running are locked and skipped until next time).
Get-ChildItem $cacheDir -Filter 'MediaHelper-*.exe' | Where-Object FullName -ne $exe |
	ForEach-Object { try { [IO.File]::Delete($_.FullName) } catch { } }

# A running helper is replaced by the new one (it exits when the new one starts).
# Started from its own folder: a working directory inside the skin folder would block Skin Installer updates.
Start-Process -FilePath $exe -ArgumentList ('--out "{0}" --app "{1}"' -f $out, $App) -WorkingDirectory $cacheDir
