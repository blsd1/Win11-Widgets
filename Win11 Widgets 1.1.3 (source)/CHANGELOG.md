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

### 1.1.3

#### New
- **Background styles:** give every widget a TranslucentTB-style background. Right-click any
  widget → **Custom skin actions** → **Background: Normal / Opaque / Clear / Blur / Acrylic**.
  All widgets switch together, and the current style is marked *(current)* in the menu.
  - **Normal:** the usual dark card.
  - **Opaque:** a solid black card.
  - **Clear:** a see-through tinted card.
  - **Blur** and **Acrylic:** the desktop behind the card is blurred, using the same Windows
    effect TranslucentTB uses for the taskbar. It is clipped to each card's rounded corners.
    A small helper (`@Resources\Scripts\Backdrop.cs`) is compiled on first use and cached in
    `%LOCALAPPDATA%\Win11Widgets`, like the Devices and Spotify helpers.
  - The colors and tints of each style can be changed in `@Resources\backgroundStyles.ini`.
- **Performance graphs follow the style:** the graph area is a darker inset on Normal, black on
  Opaque, and see-through on Clear, Blur and Acrylic, so it blends into the card.
- **Devices:** the battery bars use your Windows accent color and follow accent changes.
- **Only Win11 Widgets refresh:** changing the style refreshes just these widgets
  (`Group=Win11Widgets`), not other skins you have loaded.

#### Fixed

**Spotify**
- **Froze after sleep:** after the PC slept or hibernated for more than about a minute, the
  helper saw an old heartbeat and exited, and nothing started it again. The song, time and
  buttons froze until a refresh. The helper now notices the sleep and keeps running, and the
  widget restarts it if it ever stops.
- **Wrong play/pause icon:** after Spotify was closed and reopened (or after a one-second
  glitch), the widget could show ▶ while music was playing. The icon now always updates, and
  a failed read keeps the last known state instead of reporting "nothing playing".
- **Garbled titles:** song titles and artists with accents or non-Latin letters (e.g. "Beyoncé",
  Japanese titles) were garbled. The script is now saved in the encoding Rainmeter needs for
  Unicode.
- **Stale cover art:** the cover could stay on the previous song, because Windows often reports
  the new title before the new cover is ready. The cover is now re-checked for a few seconds
  after each song change.
- **Possible helper crash:** an error right after a play/pause/next command could crash the
  helper and freeze the widget. It is now caught.
- **Small memory leak:** the cover image stream was never released, leaking a little memory per
  song.
- **Old helper builds piled up:** old builds were only removed when the helper was rebuilt, so a
  build that was running at that moment stayed forever. Old builds are now removed on every start.
- **Build race:** two launches at the same time could both try to build the helper and fail.
  Only one builds now, into a temporary file that is renamed when complete.
- **Heartbeat setting:** with a slow update rate the heartbeat was never written, so the helper
  exited after a minute.

**Devices**
- **Froze after a refresh:** a refreshed widget starts a new helper, which waited only
  5 seconds for the old one to exit. Reading a sleepy mouse can take longer, so both could exit
  and the battery levels froze. The new helper now waits up to a minute, and the old one exits
  quickly when asked.
- **Froze after sleep:** the same sleep problem as Spotify. Fixed the same way.
- **Stuck HeadsetControl:** its 10-second timeout never worked, so a misbehaving headset could
  block the helper forever. It is now stopped after 10 seconds.
- **One device hid the others:** a Logitech device that failed mid-read (e.g. unplugged) could
  make all Logitech devices disappear for that read. Each device is now read on its own.
- **Slow updates after a busy file:** when the battery file was in use while being written, the
  next try waited a full interval (30 s). It now retries after 1 second.
- **Handle leak:** a receiver handle was left open when opening its second channel failed.
- **Missing HeadsetControl:** if `headsetcontrol.exe` was missing (e.g. removed by antivirus),
  the helper didn't start at all, so mice weren't shown either. It now starts without it.
- **Build race:** like Spotify, two launches at once could fail to build the helper.
- **Window bigger than the card:** hidden rows still made the window as tall as four devices.
  The empty part covered widgets below it (and, with Blur/Acrylic, blurred them). The window
  now matches the card.
- **White battery bars:** the bars were meant to use the accent color but were always white,
  because the color came with stray line breaks and wasn't ready on first draw.

**Volume**
- **"-1" when muted:** clicking the speaker to mute showed "-1" and made the slider jump to 0.

**Weather**
- **Log errors:** "Invalid TimeStampFormat" errors filled the Rainmeter log at every load
  (4–9 per load), because the day names were read before the weather data arrived.
- **Stuck on "Searching for location...":** when the location search failed (offline or server
  down), the message stayed forever and covered the widget. It now shows "Could not connect.
  Please try again." and keeps the current weather.
- **Not refreshed after wake:** after the PC woke up the weather could stay up to ~27 minutes
  old. It now refreshes a few seconds after waking.
- **Pressure decimals:** metric pressure showed as "1013.00 mb". It now shows "1013 mb"
  (inches keep 2 decimals).
- **Blank lines in the skin file:** every time you saved a location, blank lines were added to
  the skin's settings.
- **"Updated" time jumped:** switching between 12- and 24-hour time changed the "updated" time
  to the current time.
- **Weather Small:** the update time ("- 20:42") was drawn in the top-left corner instead of
  next to the city.
- **Weather ExtraLarge:** the error text could overlap the "click outside the text box" hint.
- **Error text not updated:** if the internet dropped after loading, the error overlay kept
  saying "Try again in a couple seconds" instead of asking you to connect (also in Weather Tiny).
- **Special letters in place names:** names with Ł, ø, ß, đ, æ and similar letters were saved
  wrong (e.g. "Łódź" became "?odz").

**Performance**
- **Gbps never shown:** at 1 Gbps or more the network showed e.g. "2048 Mbps". It now switches
  to Gbps, and back to Mbps when the speed drops.
- **Network units:** network speeds used 1024 instead of 1000, so they read 2–7% lower than
  Task Manager.
- **Memory %:** it was calculated from rounded numbers (up to 1% off) and only updated when
  the rounded value changed. It is now exact and updates every second.
- **Combo window bigger than its card:** extra-disk slots that were no longer used kept their old
  positions and stretched the window (e.g. 211 px tall for a 162 px card).
- **Combo Thin drive picker:** the last button (U:) stuck out of the card.
- **Offline network drives:** a disconnected mapped drive was still checked every 5 seconds,
  which could make Rainmeter stutter. Network drives are now skipped.
- **GPU description:** the GPU value adds up all GPU engines. The note claiming it matches
  Task Manager was wrong and has been corrected (see Known issues).

**Battery**
- **Overlapping text:** "Charging" could overlap the time remaining, and "Calculating..." didn't
  come back when the time became unknown again.
- **Typo:** removed a stray `]` in an action.

**Lock and Layout switcher**
- **Help:** it only worked once per refresh and opened the old win10widgets.com help pages,
  which are offline. It now always opens the included help file. The dead links and "watch the
  video" notes were removed from the help files.
- **Wrong auto-layout:** going from 2 to 3 monitors (or 3 to 2) loaded Layout 1 (Laptop).
- **Reloads when monitors sleep:** monitors that turn off and on (e.g. DisplayPort sleep)
  reloaded your layout every time. The monitor count must now stay the same for about
  4 seconds first.
- **"Auto-layout" menu option:** it didn't update the icon right away.
- **Lock with a space in your user name:** the lock toggle failed if your user folder name
  contained a space.

**WiFi**
- **WiFi Tiny:** after switching networks the percentage could overlap the network name.
- **Names:** WiFi Tiny was named "WiFi (medium)", and the WiFi skins described themselves as
  "Displays the current date and time".

**General**
- **Accent colors:** the Windows accent colors carried stray line breaks from the registry.
- **User names with spaces:** settings writes failed when your user folder name contained a
  space.
- **Helper builds:** a build that was interrupted could leave a broken helper behind, and an
  interrupted build lock could stop every widget from applying its backdrop. Both are now
  handled.

#### Known issues
- **Blur/Acrylic lag while dragging:** the backdrop can lag a little behind a widget while you
  drag it. This is a limitation of the Windows effect (TranslucentTB has the same thing).
- **Blur/Acrylic GPU use:** Windows (Desktop Window Manager, not Rainmeter) re-blurs the
  background each time a widget redraws, which adds roughly 0.5–2% GPU.
- **GPU usage reads high:** the GPU value adds up all GPU engines (3D, video, copy...), so it
  can read higher than Task Manager. The plugin Rainmeter includes can't count only the 3D engine.

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
