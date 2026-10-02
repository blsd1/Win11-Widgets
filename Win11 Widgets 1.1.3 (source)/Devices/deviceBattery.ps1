# Win11 Widgets for Rainmeter - Devices helper launcher
#	Author - ketamine
#
#	Compiles DevicesHelper.cs + LogiBattery.cs into a small background program (once, cached in
#	%LOCALAPPDATA%\Win11Widgets; rebuilt when the source changes) and starts it. The helper writes the
#	connected wireless devices to %TEMP%\Win11Widgets\devices.txt, which devices.lua reads.
#
#	Usage:
#		deviceBattery.ps1 [-Interval 30] [-ChargeMinutes 180]   start (or restart) the helper
#		deviceBattery.ps1 -Refresh                               ask the running helper to read now
#		deviceBattery.ps1 -Print                                 print the devices once

param(
	[switch]$Refresh,
	[switch]$Print,
	[int]$Interval = 30,
	[int]$ChargeMinutes = 180
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $env:TEMP 'Win11Widgets\devices.txt'

if ($Refresh) {
	try { [Threading.EventWaitHandle]::OpenExisting('Local\Win11Widgets-Devices-Refresh').Set() | Out-Null; exit }
	catch { }   # helper not running: start it below
}

# Build the helper (only when missing or when the source changed).
$sources = @((Join-Path $here 'LogiBattery.cs'), (Join-Path $here 'DevicesHelper.cs'))
# Build name from a checksum over both source files, so a change in either one triggers a rebuild.
$sourceHashes = ($sources | ForEach-Object { (Get-FileHash $_ -Algorithm SHA256).Hash }) -join ''
$hash = (Get-FileHash -InputStream ([IO.MemoryStream]::new([Text.Encoding]::ASCII.GetBytes($sourceHashes))) -Algorithm SHA256).Hash.Substring(0, 12)
$cacheDir = Join-Path $env:LOCALAPPDATA 'Win11Widgets'
$exe = Join-Path $cacheDir "DevicesHelper-$hash.exe"
if (-not (Test-Path $exe)) {
	# A refresh can start this again while a build is still running, so only one of them builds at a time.
	$mutex = New-Object Threading.Mutex($false, 'Local\Win11Widgets-Devices-Build')
	try { [void]$mutex.WaitOne() }
	catch [Threading.AbandonedMutexException] { }   # previous owner was killed mid-build: the mutex is ours now
	try {
		if (-not (Test-Path $exe)) {
			New-Item -ItemType Directory -Force $cacheDir | Out-Null
			# Built under a temporary name (must end in .exe) and renamed when complete, so a build that is cut
			# short never leaves a broken exe under the final name.
			$tmp = Join-Path $cacheDir "DevicesHelper-$hash.$PID.tmp.exe"
			try {
				Add-Type -Path $sources -OutputAssembly $tmp -OutputType ConsoleApplication -ReferencedAssemblies System.Core
				Move-Item -Force $tmp $exe
			}
			finally { if (Test-Path $tmp) { Remove-Item -Force $tmp -ErrorAction SilentlyContinue } }
			# Remove older builds and leftover temporary ones (ones still running are skipped and cleaned up next time).
			Get-ChildItem $cacheDir -Filter 'DevicesHelper-*.exe' | Where-Object FullName -ne $exe |
				ForEach-Object { try { [IO.File]::Delete($_.FullName) } catch { } }
		}
	}
	finally { $mutex.ReleaseMutex() }
}

# HeadsetControl is run from a copy next to the helper, so nothing inside the skin folder is ever in use
# (the Skin Installer has to move that folder when updating). A missing bundled copy only means no headsets.
$headsetControl = Join-Path $cacheDir 'headsetcontrol.exe'
$bundled = Join-Path $here '..\@Resources\Tools\headsetcontrol.exe'
if ((Test-Path $bundled) -and (-not (Test-Path $headsetControl) -or (Get-FileHash $bundled).Hash -ne (Get-FileHash $headsetControl).Hash)) {
	try { Copy-Item -Force $bundled $headsetControl } catch { }
}

$arguments = '--out "{0}" --interval {1} --charge-minutes {2} --headsetcontrol "{3}"' -f $out, $Interval, $ChargeMinutes, $headsetControl
if ($Print) {
	& $exe --print --charge-minutes $ChargeMinutes --headsetcontrol $headsetControl
	exit
}
# A running helper is replaced by the new one (it exits when the new one starts).
# Started from its own folder: a working directory inside the skin folder would block Skin Installer updates.
Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -WorkingDirectory $cacheDir
