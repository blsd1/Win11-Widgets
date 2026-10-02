# Win10 Widgets for Rainmeter - Weather location search
#	Looks up a user-supplied location (city, address or postal code) with
#	OpenStreetMap Nominatim. Searches the user's own country first (based on
#	IP), then worldwide.
#
#	Output (one line):
#		OK|<latitude>|<longitude>|<city>|<country>
#		NOTFOUND
#		ERROR

param([string]$Query)

$ErrorActionPreference = 'Stop'
# Output UTF-8 (without BOM) to match OutputType=UTF8 in the skin; redirected output uses the OEM codepage otherwise.
[Console]::OutputEncoding = New-Object Text.UTF8Encoding $false
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$headers = @{ 'User-Agent' = 'Win10Widgets-Rainmeter/1.0 (weather location search)' }

# Removes accents so the name can be saved in the (ANSI) skin file, e.g. "Košice" -> "Kosice".
function Remove-Diacritics([string]$s) {
	$normalized = $s.Normalize([Text.NormalizationForm]::FormD)
	$sb = New-Object Text.StringBuilder
	foreach ($c in $normalized.ToCharArray()) {
		if ([Globalization.CharUnicodeInfo]::GetUnicodeCategory($c) -ne 'NonSpacingMark') { [void]$sb.Append($c) }
	}
	# Letters that do not decompose into base letter + accent (e.g. "Łódź" -> "Lodz").
	$map = @{ [char]0x141 = 'L'; [char]0x142 = 'l'; [char]0xD8 = 'O'; [char]0xF8 = 'o'; [char]0xDF = 'ss'; [char]0x110 = 'D'; [char]0x111 = 'd';
		[char]0xC6 = 'AE'; [char]0xE6 = 'ae'; [char]0x152 = 'OE'; [char]0x153 = 'oe'; [char]0xDE = 'Th'; [char]0xFE = 'th' }
	$out = New-Object Text.StringBuilder
	foreach ($c in $sb.ToString().ToCharArray()) {
		if ($map.ContainsKey($c)) { [void]$out.Append($map[$c]) } else { [void]$out.Append($c) }
	}
	return ($out.ToString() -replace '[|"]', '')
}

function Find-Place([string]$countryCode) {
	$url = 'https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&addressdetails=1&accept-language=en&q=' + [uri]::EscapeDataString($Query)
	if ($countryCode) { $url += '&countrycodes=' + $countryCode }
	# (Stored in a variable first: Windows PowerShell does not enumerate a JSON array piped straight from Invoke-RestMethod.)
	$results = Invoke-RestMethod -Uri $url -Headers $headers -TimeoutSec 15
	return $results | Select-Object -First 1
}

$Query = $Query.Trim()
if ($Query.Length -lt 2) { 'NOTFOUND'; exit }

try {
	$country = ''
	try { $country = (Invoke-RestMethod -Uri 'https://ipinfo.io/country' -TimeoutSec 5).ToString().Trim().ToLower() } catch { }

	$place = $null
	if ($country) { $place = Find-Place $country }
	if (-not $place) {
		# Nominatim allows at most one request per second.
		Start-Sleep -Milliseconds 1100
		$place = Find-Place ''
	}
	if (-not $place) { 'NOTFOUND'; exit }

	$a = $place.address
	$city = @($a.city, $a.town, $a.village, $a.municipality, $a.suburb, $a.county, $a.state, $place.name) |
		Where-Object { $_ } | Select-Object -First 1
	$lat = ([double]$place.lat).ToString([Globalization.CultureInfo]::InvariantCulture)
	$lon = ([double]$place.lon).ToString([Globalization.CultureInfo]::InvariantCulture)

	'OK|{0}|{1}|{2}|{3}' -f $lat, $lon, (Remove-Diacritics $city), (Remove-Diacritics $a.country)
}
catch {
	'ERROR'
}
