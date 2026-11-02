using System;
using System.Linq;

namespace WeatherWall.Tests
{
    // The automatic location: Windows time zone -> IANA zone -> city name to look up.
    static class LocationTests
    {
        static int failures;

        static void Eq(string expected, string actual, string what)
        {
            if (expected == actual) return;
            failures++;
            Console.WriteLine("FAIL " + what + ": expected " + (expected ?? "null") + ", got " + (actual ?? "null"));
        }

        public static int Run()
        {
            Eq("Tehran", AutoCity.CityNameFor("Iran Standard Time"), "Iran");
            Eq("Berlin", AutoCity.CityNameFor("W. Europe Standard Time"), "Western Europe");
            Eq("New York", AutoCity.CityNameFor("Eastern Standard Time"), "underscore becomes a space");
            Eq(null, AutoCity.CityNameFor("UTC"), "UTC names no city");
            Eq(null, AutoCity.CityNameFor("Dateline Standard Time"), "Etc/GMT+12 names no city");
            Eq(null, AutoCity.CityNameFor("No Such Zone"), "unknown zone");
            Eq(null, AutoCity.CityNameFor(null), "no zone");
            Eq("Kamchatka", AutoCity.CityNameFor("Kamchatka Standard Time"), "retired zone still resolves");

            // Derived, not restated: every time zone this Windows knows must be in the CLDR table,
            // or a user in that zone silently gets no automatic city.
            var missing = TimeZoneInfo.GetSystemTimeZones()
                .Select(z => z.Id)
                .Where(id => AutoCity.IanaFor(id) == null)
                .ToList();
            if (missing.Count > 0)
            {
                failures++;
                Console.WriteLine("FAIL Windows zones missing from TimeZones.cs (regenerate from CLDR): " + string.Join(", ", missing));
            }
            return failures;
        }
    }
}
