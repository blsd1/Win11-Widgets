-- Win11 Widgets for Rainmeter - Devices
--	Author - ketamine
--	Shows one row per connected wireless device (written to %TEMP%\Win11Widgets\devices.txt by the deviceBattery.ps1 helper)
--	and resizes the card to fit. Devices that are off / not connected are hidden.

local ICONS = {
	headset = '[\\xE7F6]',
	mouse = '[\\xE962]',
	keyboard = '[\\xE765]',
	device = '[\\xE772]',
}

function Initialize()
	devicesFile = (os.getenv('TEMP') or '') .. '\\Win11Widgets\\devices.txt'
	aliveFile = devicesFile .. '.alive'
	lastOutput = nil
	ticks = 0
	maxRows = tonumber(SKIN:GetVariable('MaxRows')) or 4
	rowsTop = tonumber(SKIN:GetVariable('RowsTop')) or 10
	rowHeight = tonumber(SKIN:GetVariable('RowHeight')) or 46
	rowsBottom = tonumber(SKIN:GetVariable('RowsBottom')) or 6
	barWidth = tonumber(SKIN:GetVariable('BarWidth')) or 291
end

local function resize(rows)
	local w = tonumber(SKIN:GetVariable('BackgroundWidth'))
	local h = rowsTop + math.max(rows, 1) * rowHeight + rowsBottom
	SKIN:Bang('!SetOption', 'BackgroundBox', 'Shape',
		string.format('Rectangle 0.5,0.5,%d,%d,%s | Fill Color %s | StrokeWidth 1 | Stroke Color %s',
			w - 1, h - 1, SKIN:GetVariable('CornerRadius'), SKIN:GetVariable('BackgroundColor'), SKIN:GetVariable('BorderColor')))
	SKIN:Bang('!UpdateMeter', 'BackgroundBox')
end

-- Lays out the devices listed in the helper's output (one line per device).
function Parse(output)
	local devices = {}
	for line in output:gmatch('[^\r\n]+') do
		local kind, name, percent, state = line:match('^[^|]*|([^|]*)|([^|]*)|(%-?%d+)|(%a+)')
		if kind then
			table.insert(devices, { kind = kind, name = name, percent = tonumber(percent), state = state })
		end
	end

	for i = 1, maxRows do
		local d = devices[i]
		if d then
			-- 'estimated': charging over cable, level estimated from the level before plugging in.
			local estimated = d.state == 'estimated'
			local charging = d.state == 'charging' or d.state == 'full' or estimated
			local color = charging and SKIN:GetVariable('SuccessColor')
				or (d.percent <= 20 and SKIN:GetVariable('CriticalColor')
				or SKIN:GetMeasure('MeasureWindowsColorLight'):GetStringValue())
			if color == '' then color = '255,255,255' end
			if d.percent < 0 then color = '0,0,0,0' end -- unknown level: empty bar
			local off = d.state == 'off'
			local fill = math.max(barWidth * math.max(d.percent, 0) / 100, 1)
			-- Unknown level: "Off" (receiver plugged in, headset switched off) or "Charging" (charging over cable).
			local percentText = d.percent >= 0 and (d.percent .. '%') or (off and 'Off' or 'Charging')
			local textColor = SKIN:GetVariable(off and 'SolidGreyText' or 'SolidWhite')
			SKIN:Bang('!SetOption', 'DevIcon' .. i, 'Text', ICONS[d.kind] or ICONS.device)
			SKIN:Bang('!SetOption', 'DevName' .. i, 'Text', d.name)
			SKIN:Bang('!SetOption', 'DevPercent' .. i, 'Text', percentText)
			for _, meter in ipairs({ 'DevIcon', 'DevName', 'DevPercent' }) do
				SKIN:Bang('!SetOption', meter .. i, 'FontColor', textColor)
			end
			SKIN:Bang('!SetOption', 'DevBar' .. i, 'Shape2',
				string.format('Rectangle 0,0,%.1f,4,2 | Fill Color %s | StrokeWidth 0', fill, color))
			SKIN:Bang('!ShowMeterGroup', 'Device' .. i)
			if not charging then SKIN:Bang('!HideMeter', 'DevCharging' .. i) end
		else
			SKIN:Bang('!HideMeterGroup', 'Device' .. i)
		end
	end

	local rows = math.min(#devices, maxRows)
	if rows == 0 then
		SKIN:Bang('!SetOption', 'NoDevices', 'Text', 'No wireless devices connected')
		SKIN:Bang('!ShowMeter', 'NoDevices')
	else
		SKIN:Bang('!HideMeter', 'NoDevices')
	end
	resize(rows)
	SKIN:Bang('!UpdateMeter', '*')
	SKIN:Bang('!Redraw')
end

-- Checks the helper's output file every second; redraws only when it changed.
-- Every 10 s it also touches the heartbeat file that keeps the helper running while the widget is loaded.
function Update()
	ticks = ticks + 1
	if ticks % 10 == 1 then
		local alive = io.open(aliveFile, 'w')
		if alive then alive:write(os.time()); alive:close() end
	end
	local file = io.open(devicesFile, 'r')
	if not file then return 'waiting' end
	local output = file:read('*a') or ''
	file:close()
	if output ~= lastOutput then
		lastOutput = output
		Parse(output)
	end
	return 'ok'
end