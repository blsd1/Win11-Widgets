-- Win11 Widgets for Rainmeter - Extra disks
--	Finds connected drives (other than the main #Disk#), shows a disk monitor
--	for each one, and re-lays out the widget when the set of drives changes.
--	See "Performance Templates\extraDisksTemplate.ini".
--
--	Each skin variant defines #ExtraDiskLayout0# .. #ExtraDiskLayout5#, one per
--	number of extra drives, e.g.:
--		d=160,61|11,161 net=160,111 gpu=11,111 size=361,212
--	d = extra disk positions, net = network monitor, gpu = GPU monitor,
--	size = widget width,height.

local LETTERS = 'CDEFGHIJKLMNOPQRSTUVWXYZ'
-- FreeDiskSpace "Type" values: 1 = removed, 3 = removable, 4 = fixed, 5 = network, 6 = CD-ROM.
local NETWORK_DRIVE = 5
local MAX_DISKS = 5

local function parsePairs(s)
	local list = {}
	for x, y in (s or ''):gmatch('(%-?%d+)%s*,%s*(%-?%d+)') do
		table.insert(list, { x = tonumber(x), y = tonumber(y) })
	end
	return list
end

local function parseLayout(s)
	return {
		disks = parsePairs((s or ''):match('d=(%S+)')),
		net = parsePairs((s or ''):match('net=(%S+)'))[1],
		gpu = parsePairs((s or ''):match('gpu=(%S+)'))[1],
		size = parsePairs((s or ''):match('size=(%S+)'))[1],
	}
end

function Initialize()
	layouts = {}
	for n = 0, MAX_DISKS do
		local s = SKIN:GetVariable('ExtraDiskLayout' .. n)
		if s and s ~= '' then layouts[n] = parseLayout(s) end
	end
	maxDisks = 0
	while layouts[maxDisks + 1] do maxDisks = maxDisks + 1 end
	mainDisk = (SKIN:GetVariable('Disk') or ''):upper()
	current = SKIN:GetVariable('ExtraDisks') or ''
	applied = false
end

-- Shows the monitors for the drives saved in #ExtraDisks#.
local function apply()
	local i = 0
	for drive in current:gmatch('[^|]+') do
		i = i + 1
		if i > maxDisks then break end
		SKIN:Bang('!SetOption', 'MeasureXDisk' .. i, 'PerfMonInstance', drive)
		SKIN:Bang('!SetOption', 'XDLabel' .. i, 'Text', 'Disk (' .. drive .. ')')
		SKIN:Bang('!EnableMeasure', 'MeasureXDisk' .. i)
		SKIN:Bang('!EnableMeasure', 'MeasureXDiskScale' .. i)
		SKIN:Bang('!ShowMeterGroup', 'XDisk' .. i)
		SKIN:Bang('!UpdateMeterGroup', 'XDisk' .. i)
	end
end

local function detect()
	local found = {}
	for c in LETTERS:gmatch('.') do
		if #found >= maxDisks then break end
		local drive = c .. ':'
		if drive ~= mainDisk then
			local total = SKIN:GetMeasure('MeasureDriveTotal' .. c):GetValue()
			local driveType = SKIN:GetMeasure('MeasureDriveType' .. c):GetValue()
			if total > 0 and driveType ~= NETWORK_DRIVE then
				table.insert(found, drive)
			end
		end
	end
	return found
end

local function write(key, value)
	SKIN:Bang('!WriteKeyValue', 'Variables', key, value)
end

function Update()
	if not layouts[0] then return 'no layout' end

	if not applied then
		apply()
		applied = true
	end

	local found = detect()
	local key = table.concat(found, '|')
	if key ~= current then
		local layout = layouts[#found]
		current = key
		write('ExtraDisks', key)
		-- Positions are saved (not set at runtime) so relative meter positions are right on load.
		for i, pos in ipairs(layout.disks) do
			write('XDX' .. i, pos.x)
			write('XDY' .. i, pos.y)
		end
		if layout.net then
			write('GraphLeftPadding4', layout.net.x)
			write('GraphTopPadding4', layout.net.y)
		end
		if layout.gpu then
			write('GraphLeftPadding5', layout.gpu.x)
			write('GraphTopPadding5', layout.gpu.y)
		end
		if layout.size then
			write('BackgroundWidth', layout.size.x)
			write('BackgroundHeight', layout.size.y)
		end
		SKIN:Bang('!Refresh')
	end

	return key
end
