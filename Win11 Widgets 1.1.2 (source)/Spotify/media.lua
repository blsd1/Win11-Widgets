-- Win11 Widgets for Rainmeter - Spotify
--	Author - ketamine
--	Reads what Spotify is playing from %TEMP%\Win11Widgets\media.txt (written by the MediaHelper
--	background program) and passes it to the widget's measures. Button presses are sent to the
--	helper through media.txt.cmd.

function Initialize()
	mediaFile = (os.getenv('TEMP') or '') .. '\\Win11Widgets\\media.txt'
	cmdFile = mediaFile .. '.cmd'
	aliveFile = mediaFile .. '.alive'
	lastText = nil
	ticks = 0
	-- Heartbeat every ~10 s (keeps the helper running while the widget is loaded).
	heartbeatEvery = math.max(1, math.floor(10000 / (tonumber(SKIN:GetVariable('UpdateMs')) or 500)))
end

local function timeText(seconds)
	seconds = tonumber(seconds) or 0
	return string.format('%d:%02d', math.floor(seconds / 60), seconds % 60)
end

local function set(measure, option, value)
	SKIN:Bang('!SetOption', measure, option, value)
	SKIN:Bang('!UpdateMeasure', measure)
end

function Update()
	ticks = ticks + 1
	if ticks % heartbeatEvery == 1 then
		local alive = io.open(aliveFile, 'w')
		if alive then alive:write(os.time()); alive:close() end
	end

	local file = io.open(mediaFile, 'r')
	if not file then return 'waiting' end
	local text = file:read('*a') or ''
	file:close()
	if text == lastText then return 'ok' end
	lastText = text

	local media = {}
	for key, value in text:gmatch('(%w+)=([^\n]*)') do media[key] = value end
	local running = media.status == 'playing' or media.status == 'paused'

	set('MeasurePlayer', 'Formula', running and '1' or '0')
	if running then
		local position, duration = tonumber(media.position) or 0, tonumber(media.duration) or 0
		set('MeasurePlaying', 'Formula', media.status == 'playing' and '1' or '0')
		set('MeasureTrack', 'String', media.title or '')
		set('MeasureArtist', 'String', media.artist or '')
		set('MeasurePosition', 'String', timeText(position))
		set('MeasureLength', 'String', timeText(duration))
		set('MeasureProgress', 'Formula', tostring(duration > 0 and position * 100 / duration or 0))
		set('MeasureCover', 'String', media.cover or '')
	end
	return media.status or 'none'
end

-- Called by the buttons: 'playpause', 'next' or 'previous'.
function Send(command)
	local file = io.open(cmdFile, 'w')
	if file then file:write(command); file:close() end
end
