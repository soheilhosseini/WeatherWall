using System;
using System.Globalization;

namespace WeatherWall
{
    // Special days. On these days the pack's occasion_<id>.jpg replaces every weather image
    // (occasion_<id>_night.jpg, if present, is used after sunset).
    public static class Occasions
    {
        static readonly PersianCalendar Persian = new PersianCalendar();
        static readonly ChineseLunisolarCalendar Chinese = new ChineseLunisolarCalendar();

        public static readonly string[] All =
        {
            "nowruz", "sizdah_bedar", "chaharshanbe_suri", "yalda",
            "new_year", "christmas", "halloween", "valentine", "easter", "lunar_new_year",
        };

        // date is the city's local date.
        public static string For(DateTime date)
        {
            date = date.Date;
            int pm = Persian.GetMonth(date), pd = Persian.GetDayOfMonth(date);
            if (pm == 1 && pd <= 12) return "nowruz";                  // 1–12 Farvardin
            if (pm == 1 && pd == 13) return "sizdah_bedar";            // 13 Farvardin
            if (pm == 9 && pd == 30) return "yalda";                   // the night of 30 Azar
            if (date == ChaharshanbeSuri(Persian.GetYear(date))) return "chaharshanbe_suri";

            if ((date.Month == 12 && date.Day == 31) || (date.Month == 1 && date.Day == 1)) return "new_year";
            if (date.Month == 12 && date.Day >= 24 && date.Day <= 26) return "christmas";
            if (date.Month == 10 && date.Day == 31) return "halloween";
            if (date.Month == 2 && date.Day == 14) return "valentine";
            if (date == EasterSunday(date.Year)) return "easter";
            if (IsLunarNewYear(date)) return "lunar_new_year";
            return null;
        }

        // The evening before the last Wednesday of Esfand, the last month of the Persian year.
        public static DateTime ChaharshanbeSuri(int persianYear)
        {
            int days = Persian.GetDaysInMonth(persianYear, 12);
            var d = Persian.ToDateTime(persianYear, 12, days, 0, 0, 0, 0);
            while (d.DayOfWeek != DayOfWeek.Wednesday) d = d.AddDays(-1);
            return d.AddDays(-1);
        }

        // Western (Gregorian) Easter, anonymous Gregorian algorithm.
        public static DateTime EasterSunday(int y)
        {
            int a = y % 19, b = y / 100, c = y % 100, d = b / 4, e = b % 4;
            int f = (b + 8) / 25, g = (b - f + 1) / 3, h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31, day = (h + l - 7 * m + 114) % 31 + 1;
            return new DateTime(y, month, day);
        }

        // First three days of the first lunar month.
        static bool IsLunarNewYear(DateTime date)
        {
            if (date < Chinese.MinSupportedDateTime || date > Chinese.MaxSupportedDateTime) return false;
            return Chinese.GetMonth(date) == 1 && Chinese.GetDayOfMonth(date) <= 3;
        }
    }
}
