# Win11 Widgets

**A Windows 11 remake of the classic Win10 Widgets for [Rainmeter](https://www.rainmeter.net).**

Win11 Widgets brings the clean, minimal desktop widgets of Win10 Widgets into the Windows 11
era: rounded cards, the Segoe UI Variable font, Fluent icons and your Windows accent color. It
also fixes the parts that stopped working over the years, like the weather and Spotify, and
adds GPU monitoring, automatic detection of extra drives and a battery widget for your wireless
Logitech and SteelSeries devices.

> Remake by **ketamine**. Based on [Win10 Widgets](http://win10widgets.com) by **TJ Markham**,
> the original author. All credit for the original design goes to him.

---

## Features

- **Windows 11 design:** rounded cards, subtle borders, the Segoe UI Variable font and Segoe
  Fluent Icons, all following your Windows accent color
- **Weather:** live weather from Open-Meteo (no API key needed), automatic location, and
  search by city or postal code. It shows current conditions and a 5-day forecast with
  colorful Fluent weather icons.
- **Performance:** CPU, **GPU**, memory, disk and network graphs in the colors of the Windows 11
  Task Manager. Extra drives (D:, E:, USB, ...) appear automatically when connected and
  disappear when removed.
- **Devices:** battery of your wireless mouse, keyboard and headsets (Logitech, SteelSeries and
  more), with charging state. G HUB and SteelSeries GG are not needed.
- **Spotify:** song, artist, cover art, progress and controls, read from Windows' media session
- **Volume:** a Windows 11 style slider with the Fluent volume icon
- **Hard drive:** free space with a rounded usage bar
- **Date & time**, **Battery**, **WiFi**, **Lock** and **Layout switcher**

Lightweight: all widgets together use about 3% of one CPU core.

## Installation

1. Install [Rainmeter](https://www.rainmeter.net) 4.5 or newer.
2. Download `Win11 Widgets 1.1.2.rmskin` from [Releases](../../releases) and double-click it.
3. Click **Install**, then right-click the Rainmeter tray icon → **Skins** → **Win11 Widgets**
   and load the widgets you want.

**Tips:**
- Right-click any widget → **Variants** to switch its size.
- Right-click the weather widget to set your location or switch between °C and °F.
- Layout switcher: arrange your widgets, then right-click it → **Set Layout 1 / 2** to save them.

## Requirements

- Windows 10 or 11 (Windows 11 recommended; the Segoe UI Variable and Segoe Fluent Icons fonts
  come with it)
- Rainmeter 4.5+

## What's new compared to Win10 Widgets

- Fixed the weather widget, which had shown "Connection Error" since Yahoo's weather service
  shut down
- Location search that works worldwide, with postal codes
- The forecast starts from tomorrow instead of repeating today
- Working Spotify widget and layout switcher
- GPU usage and automatic extra-disk detection in Performance Combo
- New Devices widget for wireless device batteries
- Much lower CPU use (about 22% → 3% of one core)
- A complete Windows 11 visual refresh

See [CHANGELOG.md](CHANGELOG.md) for the full list.

## Credits & License

- **Original:** [Win10 Widgets](http://win10widgets.com) by TJ Markham
- **Remake:** ketamine
- **Weather data:** [Open-Meteo](https://open-meteo.com)
- **Location search:** [OpenStreetMap Nominatim](https://nominatim.openstreetmap.org)
- **Weather icons:** [Fluent Emoji](https://github.com/microsoft/fluentui-emoji) © Microsoft, MIT license
- **Headset battery:** [HeadsetControl](https://github.com/Sapd/HeadsetControl) by Sapd and
  contributors, GPL-3.0 (unmodified `headsetcontrol.exe` in `@Resources\Tools`, license included)

Licensed under [Creative Commons BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/),
the same license as the original. You may share and adapt it for non-commercial use, as long as
you give credit and keep the same license.
