# WeatherWall

A tiny Windows tray app that changes your desktop wallpaper to match the **live weather**, the
**time of day** in your city, and **special days** of the year.

- Lives next to the clock, starts with Windows, ~45 KB, no installer, no runtime to download
  (it uses the .NET Framework 4.x that ships with Windows 10 and 11).
- Weather from [Open-Meteo](https://open-meteo.com) — free for non-commercial use, no API key.
- Checks the weather once an hour and the time of day every five minutes; the wallpaper only
  changes when the picture should.

## Use

1. Build it (below) or take `WeatherWall.exe` from a release.
2. Put a `pack` folder of images next to the exe (or pick any folder from the tray menu).
3. Run `WeatherWall.exe`. Right-click the tray icon:
   - **City…** — search any city (Open-Meteo geocoding).
   - **Scene** — Auto, or pin a scene / occasion to preview it.
   - **Refresh now**, **Choose image folder…**, **Open image folder**
   - **Start with Windows** — on by default after the first run.

Settings and a log live in `%APPDATA%\WeatherWall\`.

## Image packs

A pack is a folder of images named `<scene>_<slot>.jpg` (`.png` and `.bmp` work too).

| Scenes | When |
|---|---|
| `storm` | thunderstorm (WMO 95–99) |
| `snow` | snow (71–77, 85–86) |
| `rain` | drizzle, rain, showers (51–67, 80–82) |
| `fog` | fog (45, 48) |
| `wind` | wind ≥ 40 km/h |
| `hot` | ≥ 30 °C |
| `cold` | ≤ 5 °C |
| `cloudy` | partly cloudy / overcast (2, 3) |
| `clear` | everything else |

The first match in that order wins.

| Slots | When (from the city's real sunrise and sunset) |
|---|---|
| `dawn` | sunrise ± 45 min |
| `morning` | until 35 % of the daylight |
| `noon` | until 65 % of the daylight |
| `afternoon` | until 1 h before sunset |
| `sunset` | 1 h before to 30 min after sunset |
| `night` | evening until 23:00 |
| `latenight` | 23:00 until dawn (falls back to `night`) |

A pack does not need every combination. A missing slot uses the nearest slot of the same scene;
a missing scene falls back (`storm→rain→cloudy→clear`, `snow→cold→clear`, `fog→cloudy`,
`wind→cloudy`, `hot→clear`). A pack with only `clear_noon.jpg` works.

### Special days

On these days `occasion_<id>.jpg` replaces every weather picture
(`occasion_<id>_night.jpg`, if present, is used after sunset):

| id | Day |
|---|---|
| `nowruz` | 1–12 Farvardin (Persian New Year) |
| `sizdah_bedar` | 13 Farvardin |
| `chaharshanbe_suri` | the evening before the last Wednesday of Esfand |
| `yalda` | 30 Azar |
| `new_year` | 31 December and 1 January |
| `christmas` | 24–26 December |
| `halloween` | 31 October |
| `valentine` | 14 February |
| `easter` | Western Easter Sunday |
| `lunar_new_year` | first three days of the Chinese lunar year |

Persian and lunar dates come from .NET's `PersianCalendar` and `ChineseLunisolarCalendar`, so
they are right every year without a table. If two fall on one day, the Persian one wins.

## Updates

WeatherWall checks GitHub once a day (and from **Check for updates** in the tray menu):

- the latest release of this repository for a newer `WeatherWall.exe`;
- the latest release of [`WeatherWall-Pack`](https://github.com/soheilhosseini/WeatherWall-Pack)
  for a new `pack.zip`, but only while the pack folder is the default `pack` next to the exe.

Every file is signed (RSA-3072, SHA-256). The app installs nothing whose `.sig` does not verify
against the public key compiled into it, so a swapped download — or a compromised GitHub
account — cannot push code to installed copies.

Publishing (needs the private key in `%USERPROFILE%\.weatherwall\`, which never goes into git):

```
tools\release.cmd 1.0.1 "What changed"          :: app
tools\release-pack.cmd <image folder> 2          :: images
```

## Build

```
build.cmd        :: -> bin\WeatherWall.exe
tests\run.cmd    :: scene, slot, fallback and calendar checks
```

No SDK or Visual Studio needed: both scripts use the C# compiler in
`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\`.

## Licence

MIT for the code. Image packs are published separately and are not covered by this licence.
