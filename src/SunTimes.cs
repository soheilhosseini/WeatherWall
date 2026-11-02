using System;

namespace WeatherWall
{
    // Sunrise and sunset worked out on this PC, for when the weather service cannot be reached.
    // NOAA's general solar position equations: gml.noaa.gov/grad/solcalc/solareqns.PDF
    public static class SunTimes
    {
        // Local times at the place on `day`, for a place `utcOffsetSeconds` ahead of UTC.
        public static void For(double latitude, double longitude, DateTime day, int utcOffsetSeconds,
            out DateTime sunrise, out DateTime sunset)
        {
            var daysInYear = DateTime.IsLeapYear(day.Year) ? 366 : 365;
            var gamma = 2 * Math.PI / daysInYear * (day.DayOfYear - 1);   // at noon, (hour - 12) / 24 = 0
            var eqtime = 229.18 * (0.000075 + 0.001868 * Math.Cos(gamma) - 0.032077 * Math.Sin(gamma)
                                   - 0.014615 * Math.Cos(2 * gamma) - 0.040849 * Math.Sin(2 * gamma));
            var decl = 0.006918 - 0.399912 * Math.Cos(gamma) + 0.070257 * Math.Sin(gamma)
                       - 0.006758 * Math.Cos(2 * gamma) + 0.000907 * Math.Sin(2 * gamma)
                       - 0.002697 * Math.Cos(3 * gamma) + 0.00148 * Math.Sin(3 * gamma);
            var lat = latitude * Math.PI / 180;
            var cosHa = Math.Cos(90.833 * Math.PI / 180) / (Math.Cos(lat) * Math.Cos(decl)) - Math.Tan(lat) * Math.Tan(decl);
            // Beyond the polar circles the sun can stay down (cos > 1) or up (cos < -1) all day.
            var ha = Math.Acos(Math.Max(-1, Math.Min(1, cosHa))) * 180 / Math.PI;
            var offsetMinutes = utcOffsetSeconds / 60.0;
            sunrise = day.Date.AddMinutes(720 - 4 * (longitude + ha) - eqtime + offsetMinutes);
            sunset = day.Date.AddMinutes(720 - 4 * (longitude - ha) - eqtime + offsetMinutes);
        }
    }
}
