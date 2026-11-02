<div align="center">

# WeatherWall

**Your Windows wallpaper follows the real weather, the hour of the day, and the holidays.**

<img src="https://raw.githubusercontent.com/soheilhosseini/WeatherWall-Pack/main/showcase/day-cycle.gif" alt="The wallpaper through one day" width="760">

[**⬇ Download WeatherWall.exe**](https://github.com/soheilhosseini/WeatherWall/releases/latest/download/WeatherWall.exe)
&nbsp;·&nbsp; Windows 10 / 11 &nbsp;·&nbsp; ~100 KB &nbsp;·&nbsp; free and open source

</div>

## The story

Someone I love is moving to another country to study, and for a while we will live far apart.

I made WeatherWall so that every time she looks at her computer, she sees these two cats on her
desktop — always holding each other, always side by side. Sun or rain, morning or late at night,
they are together.

We may be far apart in distance, but our hearts are always side by side. That is what these
pictures say.

<details>
<summary>به فارسی</summary>

<div dir="rtl">

کسی که دوستش دارم برای درس خواندن به کشور دیگری می‌رود و مدتی از هم دور خواهیم بود.

WeatherWall را ساختم تا هر بار که به کامپیوترش نگاه می‌کند، این دو گربه را روی دسکتاپش ببیند؛ همیشه
در آغوش هم، همیشه کنار هم. آفتابی یا بارانی، صبح یا نیمه‌شب، با هم‌اند.

شاید از هم دور باشیم، ولی قلب‌هایمان همیشه کنار هم است. این عکس‌ها همین را می‌گویند.

</div>
</details>

If you are far from someone you love, send it to them.

## What it does

Rain outside? The two cats share an umbrella. Snowing? Knitted hats and a campfire. Late at
night they fall asleep, and on Nowruz, Yalda, Halloween or Christmas the whole beach dresses up.

<img src="https://raw.githubusercontent.com/soheilhosseini/WeatherWall-Pack/main/showcase/scenes.jpg" alt="Weather and holiday scenes" width="100%">

- **Live weather** for your city — clear, cloudy, rain, storm, snow, fog, wind, heat and cold.
- **Real sunrise and sunset**: dawn, morning, noon, afternoon, sunset, night and late night.
- **Special days**: Nowruz, Sizdah Bedar, Chaharshanbe Suri, Yalda, New Year, Christmas,
  Halloween, Valentine's Day, Easter and Lunar New Year — calculated, never a fixed table.
- **Zero setup**: it uses where your PC is (Windows location), or your Windows time zone when
  location is off; pick an exact city if you like.
- **Tiny and quiet**: lives next to the clock, starts with Windows, no admin rights, no runtime
  to install, no account, no tracking.
- **Updates itself**, safely: every update is cryptographically signed.

## Use

1. [Download `WeatherWall.exe`](https://github.com/soheilhosseini/WeatherWall/releases/latest/download/WeatherWall.exe)
   and run it. It installs itself into `%LOCALAPPDATA%\Programs\WeatherWall`, starts with
   Windows, and downloads the images (about 110 MB, once) — a notification and the tray
   tooltip show the progress. Run with `--portable` to skip the install.
   Windows SmartScreen may warn about a new, unsigned app: **More info → Run anyway**.
2. Double-click the tray icon to open the WeatherWall window — a preview of your wallpaper and
   every setting in one place. The right-click menu offers the same:
   - **Location…** — automatic or an exact city you search for. Automatic asks the Windows
     location service once an hour (it never shows a prompt; the position is used only for the
     weather request), and falls back to the city of your Windows time zone when Location is
     turned off in *Settings → Privacy & security → Location*.
   - **Scene** — Auto, or pin a scene / special day to preview it.
   - **Refresh now**, **Choose image folder…**, **Open image folder**
   - **Start with Windows** — on by default.
   - **Check for updates**
   - **Share WeatherWall…** — copy the link, send it on Telegram or by email, or save the
     current wallpaper as a picture.
   - **About…** — the story behind it, and links to this page.

Weather comes from [Open-Meteo](https://open-meteo.com) (free for non-commercial use, no API
key). WeatherWall checks the weather hourly, the time of day every five minutes, and changes the
wallpaper only when the picture should.

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
| `cloudy` | partly cloudy / overcast (2, 3) with at least 40 % low or mid cloud — high, thin cloud alone leaves the sun out, so it stays `clear` |
| `clear` | everything else |

The first match in that order wins.

| Slots | When (from the city's real sunrise and sunset) |
|---|---|
| `dawn` | sunrise ± 45 min |
| `morning` | until 35 % of the daylight |
| `noon` | until 75 % of the daylight |
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

WeatherWall checks GitHub when it starts, after the PC wakes from sleep, once a day, and from
**Check for updates** in the tray menu:

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
