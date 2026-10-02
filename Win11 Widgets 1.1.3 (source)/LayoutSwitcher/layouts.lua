-- Win11 Widgets for Rainmeter - LayoutSwitcher
--	Author - ketamine
--	Loads a saved layout. If it hasn't been saved yet ("Set Layout 1/2" in the context menu),
--	shows how to save it instead of silently doing nothing.

function Initialize()
	layoutsDir = SKIN:GetVariable('SETTINGSPATH') .. 'Layouts\\'
	coreFile = SKIN:GetVariable('CURRENTPATH') .. 'LayoutSwitcher-Small.ini'
end

local function saved(name)
	local file = io.open(layoutsDir .. name .. '\\Rainmeter.ini', 'r')
	if file then file:close() return true end
	return false
end

-- name: layout to load; setting: CurrentSetting to remember (0 auto, 1, 2; nil = keep);
-- quiet: don't explain when the layout is missing (automatic switching).
function Load(name, setting, quiet)
	if not name or name == '' then return end
	if saved(name) then
		if setting then SKIN:Bang('!WriteKeyValue', 'Variables', 'CurrentSetting', tostring(setting), coreFile) end
		SKIN:Bang('!LoadLayout', name)
	elseif not quiet then
		local number = name == SKIN:GetVariable('Layout2Name') and '2' or '1'
		SKIN:Bang('!CommandMeasure', 'MeasureLayout' .. number .. 'NotSet', 'Run')
	end
end
