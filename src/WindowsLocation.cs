using System;
using System.Device.Location;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace WeatherWall
{
    // The PC's own position from the Windows location service (Wi-Fi, cell towers, IP, or the
    // "Default location" in Settings). Preferred over the time-zone city when Windows allows it.
    public static class WindowsLocation
    {
        public enum Outcome { Found, Off, NoFix }

        // Blocks for up to `timeout`, so call it off the UI thread. Asks once and stops again, so
        // the "location in use" icon shows only for a moment. Never shows a permission prompt.
        public static Outcome TryGet(TimeSpan timeout, out double latitude, out double longitude)
        {
            latitude = longitude = 0;
            var clock = Stopwatch.StartNew();
            using (var watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default))
            {
                watcher.Start(true);
                while (clock.Elapsed < timeout && watcher.Position.Location.IsUnknown
                       && watcher.Permission != GeoPositionPermission.Denied
                       && watcher.Status != GeoPositionStatus.Disabled)
                    Thread.Sleep(250);
                var fix = watcher.Position.Location;
                var off = watcher.Permission == GeoPositionPermission.Denied || watcher.Status == GeoPositionStatus.Disabled;
                watcher.Stop();
                if (!fix.IsUnknown)
                {
                    latitude = fix.Latitude;
                    longitude = fix.Longitude;
                    return Outcome.Found;
                }
                return off ? Outcome.Off : Outcome.NoFix;
            }
        }

        // Open-Meteo has no reverse geocoding, so the position borrows the time-zone city's name
        // when it is close by, and otherwise shows its coordinates.
        public static string LabelFor(double latitude, double longitude, City nearby)
        {
            if (nearby != null && DistanceKm(latitude, longitude, nearby.Latitude, nearby.Longitude) <= 100)
                return "Near " + nearby.Label;
            return string.Format(CultureInfo.InvariantCulture, "{0:0.00}, {1:0.00}", latitude, longitude);
        }

        public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            Func<double, double> rad = d => d * Math.PI / 180;
            var dLat = rad(lat2 - lat1);
            var dLon = rad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                    + Math.Cos(rad(lat1)) * Math.Cos(rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        }
    }
}
