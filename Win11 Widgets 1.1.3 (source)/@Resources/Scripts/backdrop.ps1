# Win11 Widgets for Rainmeter - Backdrop launcher
#	Author - ketamine
#
#	Compiles Backdrop.cs into a small program (once, cached in %LOCALAPPDATA%\Win11Widgets; rebuilt
#	when the source changes) and runs it to give skin windows a blur/acrylic backdrop or remove it.
#
#	Usage:
#		backdrop.ps1 -Style <none|blur|acrylic> -Tint AARRGGBB -Width W -Height H -Radius R -Skin <skin .ini path>
#		backdrop.ps1 -Style none -All      remove the backdrop from every Win11 Widgets skin

param(
	[string]$Style = 'none',
	[string]$Tint = '00000000',
	[string]$Width = '0',
	[string]$Height = '0',
	[string]$Radius = '0',
	[string[]]$Skin = @(),
	[switch]$All
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$source = Join-Path $here 'Backdrop.cs'

# Build the helper (only when missing or when the source changed).
$hash = (Get-FileHash $source -Algorithm SHA256).Hash.Substring(0, 12)
$cacheDir = Join-Path $env:LOCALAPPDATA 'Win11Widgets'
$exe = Join-Path $cacheDir "Backdrop-$hash.exe"
if (-not (Test-Path $exe)) {
	# Every skin starts this at once after a refresh, so only one of them builds; the rest wait for it.
	$mutex = New-Object Threading.Mutex($false, 'Local\Win11Widgets-Backdrop-Build')
	try { [void]$mutex.WaitOne() }
	catch [Threading.AbandonedMutexException] { }   # previous owner was killed mid-build: the mutex is ours now
	try {
		if (-not (Test-Path $exe)) {
			New-Item -ItemType Directory -Force $cacheDir | Out-Null
			# Built under a temporary name (must end in .exe) and renamed when complete, so a build that is cut
			# short (e.g. Settings stopping a running style change) never leaves a broken exe under the final name.
			$tmp = Join-Path $cacheDir "Backdrop-$hash.$PID.tmp.exe"
			try {
				Add-Type -Path $source -OutputAssembly $tmp -OutputType ConsoleApplication
				Move-Item -Force $tmp $exe
			}
			finally { if (Test-Path $tmp) { Remove-Item -Force $tmp -ErrorAction SilentlyContinue } }
			# Remove older builds and leftover temporary ones.
			Get-ChildItem $cacheDir -Filter 'Backdrop-*.exe' | Where-Object FullName -ne $exe |
				Remove-Item -ErrorAction SilentlyContinue
		}
	}
	finally { $mutex.ReleaseMutex() }
}

if ($All) {
	# Every skin file of this suite (the helper only touches the ones that are open).
	$root = Split-Path -Parent (Split-Path -Parent $here)
	$Skin = Get-ChildItem $root -Recurse -Filter '*.ini' | Where-Object { $_.FullName -notlike '*\@Resources\*' } |
		ForEach-Object FullName
}
if ($Skin.Count -gt 0) { & $exe $Style $Tint $Width $Height $Radius @Skin }
