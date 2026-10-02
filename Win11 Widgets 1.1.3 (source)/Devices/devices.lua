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

local ROW_METERS = { 'DevIcon', 'DevName', 'DevPercent', 'DevCharging', 'DevBar' }

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
	-- Card height the backdrop was last applied for (nil: not applied yet).
	appliedHeight = nil
	-- Helper liveness: it rewrites devices.txt with a fresh "updated=" time at least every 10 s.
	lastSeen = os.time()
	lastUpdate = os.time()
	lastRestart = os.time()
	-- Each row's meter positions, so hidden rows can be parked at the top: Rainmeter still sizes the window
	-- by hidden meters' Y, and the window has to match the card for a blur/acrylic backdrop.
	rowY = {}
	for i = 1, maxRows do
		for _, meter in ipairs(ROW_METERS) do
			rowY[meter .. i] = SKIN:GetMeter(meter .. i):GetY()
		end
	end
end

local function placeRow(i, shown)
	for _, meter in ipairs(ROW_METERS) do
		SKIN:Bang('!SetOption', meter .. i, 'Y', shown and rowY[meter .. i] or 0)
	end
end

local function resize(rows)
	local w = tonumber(SKIN:GetVariable('BackgroundWidth'))
	local h = rowsTop + math.max(rows, 1) * rowHeight + rowsBottom
	SKIN:Bang('!SetOption', 'BackgroundBox', 'Shape',
		string.format('Rectangle 0.5,0.5,%d,%d,%s | Fill Color %s | StrokeWidth 1 | Stroke Color %s',
			w - 1, h - 1, SKIN:GetVariable('CornerRadius'), SKIN:GetVariable('BackgroundColor'), SKIN:GetVariable('BorderColor')))
	SKIN:Bang('!UpdateMeter', 'BackgroundBox')
	-- Re-clip the blur/acrylic backdrop (background.ini) to the new card height (applied in Update()).
	-- Only when the height changed: each apply starts powershell + Backdrop.exe.
	backdropHeight = h ~= appliedHeight and h or nil
end

-- Re-applies the backdrop once background.ini isn't still busy applying the previous one.
local function updateBackdrop()
	if backdropHeight and SKIN:GetVariable('BackdropBusy', '0') ~= '1' then
		SKIN:Bang('!SetVariable', 'BackgroundHeight', backdropHeight)
		-- Devices.ini sets BackdropReady=0, so background.ini skips the backdrop at load until the card is sized here.
		SKIN:Bang('!SetVariable', 'BackdropReady', '1')
		SKIN:Bang('!UpdateMeasure', 'MeasureBackdrop')
		SKIN:Bang('!UpdateMeasure', 'MeasureBackdropStyle')
		appliedHeight = backdropHeight
		backdropHeight = nil
	end
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
				-- Windows accent colour (keep just the RRGGBB, in case of stray characters around the value).
				or SKIN:GetMeasure('MeasureWindowsColorLight'):GetStringValue():match('%x%x%x%x%x%x'))
			if not color then color = '255,255,255' end
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
			placeRow(i, true)
			SKIN:Bang('!ShowMeterGroup', 'Device' .. i)
			if not charging then SKIN:Bang('!HideMeter', 'DevCharging' .. i) end
		else
			placeRow(i, false)
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
-- Redraws the rows on the next update (called when the Windows accent colour is read or changes).
function Redraw()
	lastOutput = nil
end

function Update()
	ticks = ticks + 1
	if ticks % 10 == 1 then
		local alive = io.open(aliveFile, 'w')
		if alive then alive:write(os.time()); alive:close() end
	end

	-- Restarts the helper if it stopped writing (e.g. it exited while the PC was asleep). A long gap since the
	-- last update means the PC was asleep: give the helper time to catch up first.
	local now = os.time()
	if now - lastUpdate > 5 then lastSeen = now end
	lastUpdate = now
	if now - lastSeen > 60 and now - lastRestart > 60 then
		lastRestart = now
		SKIN:Bang('!CommandMeasure', 'MeasureDevicesHelper', 'Run')
	end

	local file = io.open(devicesFile, 'r')
	if file then
		local output = file:read('*a') or ''
		file:close()
		local updated = tonumber(output:match('^updated=(%d+)'))
		if updated then lastSeen = math.max(lastSeen, updated) end
		-- Compared without the time, so the rows are only redrawn when a device changed.
		output = (output:gsub('^updated=%d*\n', ''))
		if output ~= lastOutput then
			lastOutput = output
			Parse(output)
		end
	elseif not appliedHeight then
		-- No helper output yet (e.g. first start, while it is being compiled): apply the backdrop to the empty card.
		resize(0)
	end
	-- After Parse, so the first backdrop is applied right away with the real card height.
	updateBackdrop()
	return file and 'ok' or 'waiting'
end