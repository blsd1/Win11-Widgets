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
