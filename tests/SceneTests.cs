using System;
using System.IO;

namespace WeatherWall.Tests
{
    // Plain console checks for the scene and slot rules; run tests\run.cmd.
    static class SceneTests
    {
        static int failures;

        static void Eq(string expected, string actual, string what)
        {
            if (expected == actual) return;
            failures++;
            Console.WriteLine("FAIL " + what + ": expected " + expected + ", got " + actual);
        }

        static Weather W(int code, double temp, double wind)
        {
            return new Weather
            {
                Code = code, TemperatureC = temp, WindKmh = wind,
                Sunrise = new DateTime(2026, 6, 1, 6, 0, 0),
                Sunset = new DateTime(2026, 6, 1, 18, 0, 0),
            };
        }

        static int Main()
        {
            // Scene priority: storm > snow > rain > fog > wind > hot > cold > cloudy > clear.
            Eq("storm", Scenes.SceneFor(W(95, 20, 60)), "thunderstorm beats wind");
            Eq("snow", Scenes.SceneFor(W(73, -2, 0)), "snow");
            Eq("rain", Scenes.SceneFor(W(61, 2, 0)), "cold rain stays rain");
            Eq("rain", Scenes.SceneFor(W(81, 15, 0)), "showers");
            Eq("fog", Scenes.SceneFor(W(45, 8, 0)), "fog");
            Eq("wind", Scenes.SceneFor(W(1, 15, 45)), "wind");
            Eq("hot", Scenes.SceneFor(W(0, 33, 5)), "hot");
            Eq("cold", Scenes.SceneFor(W(3, 1, 5)), "cold beats cloudy");
            Eq("cloudy", Scenes.SceneFor(W(3, 12, 5)), "cloudy");
            Eq("clear", Scenes.SceneFor(W(1, 12, 5)), "clear");

            // Sunrise 06:00, sunset 18:00.
            var w = W(0, 15, 0);
            Func<int, int, string> at = (h, m) => Scenes.SlotFor(w, new DateTime(2026, 11, 2, h, m, 0));
            Eq("latenight", at(3, 0), "03:00");
            Eq("dawn", at(5, 20), "05:20");
            Eq("dawn", at(6, 40), "06:40");
            Eq("morning", at(8, 0), "08:00");
            Eq("noon", at(12, 0), "12:00");
            Eq("afternoon", at(15, 0), "15:00");
            Eq("sunset", at(17, 30), "17:30");
            Eq("sunset", at(18, 20), "18:20");
            Eq("night", at(21, 0), "21:00");
            Eq("latenight", at(23, 30), "23:30");

            // Fallbacks against a tiny pack.
            var dir = Path.Combine(Path.GetTempPath(), "ww-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            foreach (var f in new[] { "clear_noon.jpg", "clear_night.jpg", "rain_night.jpg", "cold_sunset.jpg" })
                File.WriteAllText(Path.Combine(dir, f), "");
            Func<string, string, string> pick = (s, t) =>
            {
                var p = Scenes.Resolve(dir, s, t);
                return p == null ? null : Path.GetFileName(p);
            };
            Eq("clear_noon.jpg", pick("clear", "noon"), "exact");
            Eq("clear_night.jpg", pick("clear", "latenight"), "latenight falls back to night");
            Eq("rain_night.jpg", pick("storm", "night"), "storm falls back to rain");
            Eq("rain_night.jpg", pick("rain", "noon"), "missing slot uses nearest in same scene");
            Eq("cold_sunset.jpg", pick("snow", "dawn"), "snow falls back to cold");
            Eq("clear_noon.jpg", pick("hot", "afternoon"), "hot falls back to clear, nearest slot");
            File.WriteAllText(Path.Combine(dir, "occasion_yalda.jpg"), "");
            File.WriteAllText(Path.Combine(dir, "occasion_nowruz.jpg"), "");
            File.WriteAllText(Path.Combine(dir, "occasion_nowruz_night.jpg"), "");
            Func<string, string, string> occ = (o, t) =>
            {
                var p = Scenes.ResolveOccasion(dir, o, t);
                return p == null ? null : Path.GetFileName(p);
            };
            Eq("occasion_yalda.jpg", occ("yalda", "night"), "occasion without night image");
            Eq("occasion_nowruz_night.jpg", occ("nowruz", "latenight"), "occasion night image");
            Eq("occasion_nowruz.jpg", occ("nowruz", "noon"), "occasion day image");
            Eq(null, occ("halloween", "noon"), "missing occasion image");
            // Variants: clear_noon.jpg and clear_noon~kiss.jpg take turns by day of year.
            File.WriteAllText(Path.Combine(dir, "clear_noon~kiss.jpg"), "");
            File.WriteAllText(Path.Combine(dir, "clear_noonday.jpg"), ""); // a different name, never a variant
            Func<DateTime, string> variant = d => Path.GetFileName(Scenes.Resolve(dir, "clear", "noon", d));
            var day1 = new DateTime(2027, 1, 1);   // day of year 1 -> second in order
            var day2 = new DateTime(2027, 1, 2);   // day of year 2 -> first in order
            Eq("clear_noon~kiss.jpg", variant(day1), "variant on an odd day");
            Eq("clear_noon.jpg", variant(day2), "base image on an even day");
            Directory.Delete(dir, true);

            // Dates checked against time.ir (1405): 1 Farvardin = 2026-03-21, 30 Azar = 2026-12-21.
            Func<int, int, int, string> on = (y, m, d) => Occasions.For(new DateTime(y, m, d));
            Eq("nowruz", on(2026, 3, 21), "Nowruz 1405");
            Eq(null, on(2026, 3, 20), "day before Nowruz 1405");
            Eq("nowruz", on(2026, 4, 1), "12 Farvardin 1405");
            Eq("sizdah_bedar", on(2026, 4, 2), "13 Farvardin 1405");
            Eq("yalda", on(2026, 12, 21), "Yalda 1405");
            Eq("chaharshanbe_suri", on(2027, 3, 16), "Tuesday before the last Wednesday of Esfand 1405");
            Eq(null, on(2027, 3, 17), "the Wednesday itself");
            Eq("new_year", on(2026, 12, 31), "New Year's Eve");
            Eq("new_year", on(2027, 1, 1), "New Year's Day");
            Eq("christmas", on(2026, 12, 25), "Christmas");
            Eq("halloween", on(2027, 10, 31), "Halloween");
            Eq("valentine", on(2027, 2, 14), "Valentine's Day");
            Eq("easter", on(2028, 4, 16), "Easter 2028");
            Eq("nowruz", on(2027, 3, 28), "Easter 2027 falls in Nowruz; Nowruz wins");
            Eq("lunar_new_year", on(2027, 2, 6), "Lunar New Year 2027");
            Eq("lunar_new_year", on(2028, 1, 26), "Lunar New Year 2028");
            Eq(null, on(2027, 6, 15), "ordinary day");

            failures += UpdaterTests.Run();
            failures += LocationTests.Run();

            Console.WriteLine(failures == 0 ? "All tests passed." : failures + " test(s) failed.");
            return failures == 0 ? 0 : 1;
        }
    }
}
