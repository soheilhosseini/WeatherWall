using System;
using System.Linq;

namespace WeatherWall
{
    // The default location: the city that names the Windows time zone (W. Europe Standard Time ->
    // Europe/Berlin -> Berlin), found with the same geocoder the city search uses.
    public static class AutoCity
    {
        // Retired Windows zones that CLDR no longer lists but older Windows installs still offer.
        static readonly System.Collections.Generic.Dictionary<string, string> RetiredZones =
            new System.Collections.Generic.Dictionary<string, string>
            {
                { "Mid-Atlantic Standard Time", "Etc/GMT+2" },
                { "Kamchatka Standard Time", "Asia/Kamchatka" },
            };

        public static string IanaFor(string windowsZone)
        {
            string iana;
            if (windowsZone == null) return null;
            if (TimeZones.WindowsToIana.TryGetValue(windowsZone, out iana)) return iana;
            return RetiredZones.TryGetValue(windowsZone, out iana) ? iana : null;
        }

        public static string CurrentWindowsZone()
        {
            TimeZoneInfo.ClearCachedData();
            return TimeZoneInfo.Local.Id;
        }

        public static string CurrentZoneDisplayName()
        {
            TimeZoneInfo.ClearCachedData();
            return TimeZoneInfo.Local.DisplayName;
        }

        // "America/New_York" -> "New York"; zones that name no city (Etc/GMT+12, UTC) -> null.
        public static string CityNameFor(string windowsZone)
        {
            var iana = IanaFor(windowsZone);
            if (iana == null) return null;
            if (iana.StartsWith("Etc/") || !iana.Contains("/")) return null;
            return iana.Substring(iana.LastIndexOf('/') + 1).Replace('_', ' ');
        }

        // Network call. Prefers a match in the same IANA zone; null when nothing fits.
        public static City Resolve(string windowsZone)
        {
            var name = CityNameFor(windowsZone);
            if (name == null) return null;
            var iana = IanaFor(windowsZone);
            var found = WeatherClient.SearchCity(name);
            return found.FirstOrDefault(c => c.Timezone == iana) ?? found.FirstOrDefault();
        }
    }
}
