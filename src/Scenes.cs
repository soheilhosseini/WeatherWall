using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WeatherWall
{
    // Picks the image for the current weather and time of day.
    // A pack is a folder of images named <scene>_<slot>.jpg (or .png), e.g. rain_noon.jpg.
    // <scene>_<slot>~<anything>.jpg adds a variant; one of them is shown each day, in turn.
    public static class Scenes
    {
        public static readonly string[] All =
            { "clear", "cloudy", "fog", "rain", "storm", "snow", "cold", "hot", "wind" };

        // Day order, used to find the nearest slot when an image is missing.
        public static readonly string[] Slots =
            { "dawn", "morning", "noon", "afternoon", "sunset", "night", "latenight" };

        // Where to look when a scene has no image for any slot.
        static readonly Dictionary<string, string> SceneFallback = new Dictionary<string, string>
        {
            { "storm", "rain" }, { "snow", "cold" }, { "rain", "cloudy" }, { "fog", "cloudy" },
            { "wind", "cloudy" }, { "hot", "clear" }, { "cold", "clear" }, { "cloudy", "clear" },
        };

        // WMO codes: open-meteo.com/en/docs (weather_code).
        public static string SceneFor(Weather w)
        {
            int c = w.Code;
            if (c >= 95) return "storm";
            if ((c >= 71 && c <= 77) || c == 85 || c == 86) return "snow";
            if ((c >= 51 && c <= 67) || (c >= 80 && c <= 82)) return "rain";
            if (c == 45 || c == 48) return "fog";
            if (w.WindKmh >= 40) return "wind";
            if (w.TemperatureC >= 30) return "hot";
            if (w.TemperatureC <= 5) return "cold";
            if (c == 2 || c == 3) return "cloudy";
            return "clear";
        }

        public static string SlotFor(Weather w, DateTime cityNow)
        {
            var rise = cityNow.Date + w.Sunrise.TimeOfDay;
            var set = cityNow.Date + w.Sunset.TimeOfDay;
            var day = set - rise;
            if (cityNow >= rise.AddMinutes(-45) && cityNow < rise.AddMinutes(45)) return "dawn";
            if (cityNow >= rise.AddMinutes(45) && cityNow < rise + TimeSpan.FromTicks((long)(day.Ticks * 0.35))) return "morning";
            if (cityNow >= rise && cityNow < rise + TimeSpan.FromTicks((long)(day.Ticks * 0.65))) return "noon";
            if (cityNow >= rise && cityNow < set.AddMinutes(-60)) return "afternoon";
            if (cityNow >= set.AddMinutes(-60) && cityNow < set.AddMinutes(30)) return "sunset";
            if (cityNow.Hour >= 23 || cityNow < rise.AddMinutes(-45)) return "latenight";
            return "night";
        }

        public static string Resolve(string folder, string scene, string slot, DateTime? day = null)
        {
            var date = day ?? DateTime.Today;
            var seen = new HashSet<string>();
            while (scene != null && seen.Add(scene))
            {
                var path = NearestSlot(folder, scene, slot, date);
                if (path != null) return path;
                string next;
                scene = SceneFallback.TryGetValue(scene, out next) ? next : null;
            }
            return null;
        }

        public static string ResolveOccasion(string folder, string occasion, string slot, DateTime? day = null)
        {
            var date = day ?? DateTime.Today;
            if (slot == "night" || slot == "latenight")
            {
                var night = FindFile(folder, "occasion_" + occasion + "_night", date);
                if (night != null) return night;
            }
            return FindFile(folder, "occasion_" + occasion, date);
        }

        static string NearestSlot(string folder, string scene, string slot, DateTime date)
        {
            int start = Array.IndexOf(Slots, slot);
            if (start < 0) start = 0;
            // "latenight" falls back to "night" before anything else.
            if (slot == "latenight")
            {
                var p = Find(folder, scene, "latenight", date) ?? Find(folder, scene, "night", date);
                if (p != null) return p;
            }
            for (int d = 0; d < Slots.Length; d++)
            {
                foreach (int i in new[] { start - d, start + d })
                {
                    if (i < 0 || i >= Slots.Length) continue;
                    var p = Find(folder, scene, Slots[i], date);
                    if (p != null) return p;
                }
            }
            return null;
        }

        static string Find(string folder, string scene, string slot, DateTime date)
        {
            return FindFile(folder, scene + "_" + slot, date);
        }

        static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };

        static string FindFile(string folder, string name, DateTime date)
        {
            if (!Directory.Exists(folder)) return null;
            var matches = Directory.GetFiles(folder, name + ".*")
                .Concat(Directory.GetFiles(folder, name + "~*.*"))
                .Where(p => ImageExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return matches.Length == 0 ? null : matches[date.DayOfYear % matches.Length];
        }
    }
}
