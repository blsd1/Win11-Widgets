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
	New-Item -ItemType Directory -Force $cacheDir | Out-Null
	Add-Type -Path $sources -OutputAssembly $exe -OutputType ConsoleApplication -ReferencedAssemblies System.Core
	# Remove older builds (ones still running are skipped and cleaned up next time).
	Get-ChildItem $cacheDir -Filter 'DevicesHelper-*.exe' | Where-Object FullName -ne $exe |
		ForEach-Object { try { [IO.File]::Delete($_.FullName) } catch { } }
}

# HeadsetControl is run from a copy next to the helper, so nothing inside the skin folder is ever in use
# (the Skin Installer has to move that folder when updating).
$headsetControl = Join-Path $cacheDir 'headsetcontrol.exe'
$bundled = Join-Path $here '..\@Resources\Tools\headsetcontrol.exe'
if (-not (Test-Path $headsetControl) -or (Get-FileHash $bundled).Hash -ne (Get-FileHash $headsetControl).Hash) {
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
