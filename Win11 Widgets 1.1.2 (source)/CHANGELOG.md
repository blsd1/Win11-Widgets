# Win11 Widgets for Rainmeter

**Win11 Widgets** is a remake of the classic **Win10 Widgets** for Rainmeter. It keeps the
simple widgets that blend into your desktop and gives them a Windows 11 look. It also fixes
the parts that stopped working over the years, like the weather, and adds a few new
features.

- **Remake by:** ketamine
- **Original author:** TJ Markham, creator of [Win10 Widgets](http://win10widgets.com)
- **License:** [Creative Commons BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/),
  the same license as the original
- **Weather icons:** [Fluent Emoji](https://github.com/microsoft/fluentui-emoji) © Microsoft, MIT license

All credit for the original design and widgets goes to TJ Markham. This remake would not
exist without Win10 Widgets.

---

## Changelog

### 1.1.2

#### New
- **Devices widget:** battery of your wireless devices on one card, with an icon, name,
  percentage and a rounded bar (green while charging, red when low).
  - **Logitech mice and keyboards** (LIGHTSPEED, Unifying and Bolt receivers) are read directly
    from the receiver. G HUB is not needed.
  - **Wireless headsets** (SteelSeries Arctis, Logitech G, Corsair, HyperX, ...) are read with the
    bundled [HeadsetControl](https://github.com/Sapd/HeadsetControl) (GPL-3.0, license included).
  - A headset whose receiver is plugged in but that is switched off shows as **Off**.
  - **Headsets charging over their cable** (e.g. Logitech G733) show an estimated percentage.
    These headsets can't report their real level while charging, so it is estimated from the
    level before plugging in plus the time spent charging.
  - Plugging devices in or out is picked up within about 2 seconds. The card grows and shrinks to
    fit the connected devices.

#### Fixed
- **Spotify:** the widget works again. The old Spotify plugin relied on a Spotify API that no
  longer exists. The widget now reads Spotify from Windows' media session (the same info the
  Windows 11 media flyout shows): song, artist, cover art, time and progress. Play/pause,
  next and previous control Spotify itself, even while other media (e.g. YouTube) is playing.
- **Layout switcher:**
  - "Set Layout" no longer hangs. It used to wait forever on a hidden "file or directory?"
    question, so no layout was ever saved.
  - Its confirmation messages show again (they used `mshta`, which current Windows blocks).
  - Clicking the switcher before a layout is saved now explains how to save one.
- **Performance Combo Thin and Wide:** extra disks are laid out correctly in these layouts (they
  used to reuse the normal Combo's positions), and the Network value in Wide no longer overlaps
  the Memory label.
- **Logitech mice:** no more false "15%, charging" readings while the mouse is asleep.

#### Changed
- **Task Manager colors:** the performance graphs use the colors of the Windows 11 Task Manager
  (cyan CPU, blue Memory, lime green Disk, purple GPU, pink Network), with grey frames, a darker
  inset background and a softer fill.
- **Much lighter on CPU:** Rainmeter's CPU use with the widgets went from about 22% to about 3% of
  one core.
  - The clock, disk and volume widgets used to update 1000 times per second. Each widget now
    updates only as often as it needs to.
  - The accent-color check used to start a program up to 10 times per second in every widget.
    It now runs about every 30 seconds.
  - The volume slider redraws instantly when you drag, click or scroll it, so it stays smooth
    while updating once per second otherwise.
- **Small helpers:** the Devices and Spotify helpers are small programs compiled on your PC from
  the included source on first use (about 25-30 MB of RAM each). They run from their own folder
  in `%LOCALAPPDATA%\Win11Widgets` (so they never block Skin Installer updates) and close by
  themselves when their widget is unloaded or Rainmeter closes.
### 1.1.0: first Win11 Widgets release (changes from Win10 Widgets 1.0.0)

#### Fixed
- **Weather "Connection Error":** the original widget got its data from Yahoo's weather
  service, which shut down in 2019, so it could never load. Weather now comes from
  [Open-Meteo](https://open-meteo.com), which is free and needs no API key.
- **Weather location detection:** replaced the old ZIP-code/WOEID lookup. Your location is
  now detected automatically from your IP address.
- **Weather "Set location":** the search failed for every place name and postal code. It
  now uses [OpenStreetMap](https://www.openstreetmap.org) and accepts cities, addresses and
  postal codes worldwide (e.g. `Bratislava`, `840 02`, `Vienna, Austria`). Accented names
  like `Banská Bystrica` also work. It searches your own country first.
- **Weather forecast:** the forecast row no longer repeats today. It now starts with tomorrow
  (e.g. Tue–Sat on a Monday).
- **Update check:** the Welcome widget no longer tells you to "update" back to the original
  Win10 Widgets.

#### New
- **GPU usage** in Performance Combo (all three layouts), shown the same way as CPU and read
  from the same counter as Task Manager.
- **Automatic extra disks** in Performance Combo: every connected drive (D:, E:, USB drives,
  ...) gets its own Disk graph, and it disappears when unplugged. The widget re-arranges and
  resizes itself automatically (up to 5 extra drives).
- **Performance Combo layout:** disks are grouped together (Disk C: next to Disk D:), with
  GPU and Network below.

#### Windows 11 design
- **Cards:** rounded corners, a subtle 1px border and the Windows 11 dark card color. The
  widgets float with small gaps instead of touching.
- **Font:** Segoe UI Variable, the Windows 11 system font.
- **Icons:** Segoe Fluent Icons, the Windows 11 icon set, for volume (level and mute),
  battery (level and charging), WiFi (signal strength and open network), lock/unlock,
  Spotify controls and the layout switcher.
- **Weather icons:** colorful 3D Fluent Emoji, like the Windows 11 weather widget, with
  night icons for clear and cloudy nights.
- **Volume slider:** the Windows 11 slider, with a thin rounded track, accent-colored fill
  and a round thumb.
- **Disk space bar:** a rounded progress bar in the accent color.
- **Spotify:** the cover art and progress bar are rounded to the card's corners.
- **Accent colors:** these follow your Windows accent color, using the lighter shades
  Windows 11 uses in dark mode, so text stays readable.

#### Requirements
- Rainmeter 4.5 or newer
- Windows 10 or 11. Windows 11 is recommended, since the Segoe UI Variable and Segoe Fluent
  Icons fonts come with it.

---

### 1.0.0: Win10 Widgets (original)
The original release by TJ Markham. See [win10widgets.com](http://win10widgets.com).
