using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace WeatherWall
{
    public class Weather
    {
        public int Code;            // WMO weather code
        public double TemperatureC;
        public double WindKmh;
        public DateTime Sunrise;    // in the city's local time
        public DateTime Sunset;
        public int UtcOffsetSeconds;
        public DateTime FetchedUtc;

        public DateTime CityNow
        {
            get { return DateTime.UtcNow.AddSeconds(UtcOffsetSeconds); }
        }
    }

    public class City
    {
        public string Label;
        public double Latitude;
        public double Longitude;

        public override string ToString() { return Label; }
    }

    // Open-Meteo: free for non-commercial use, no API key (open-meteo.com/en/pricing).
    public static class WeatherClient
    {
        static WeatherClient()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        static Dictionary<string, object> GetJson(string url)
        {
            using (var wc = new WebClient())
            {
                wc.Encoding = Encoding.UTF8;
                wc.Headers[HttpRequestHeader.UserAgent] = "WeatherWall";
                var text = wc.DownloadString(url);
                return new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(text);
            }
        }

        static double Num(object o)
        {
            return Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }

        static DateTime Iso(object o)
        {
            return DateTime.ParseExact((string)o, "yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture);
        }

        public static Weather Fetch(double lat, double lon)
        {
            var url = string.Format(CultureInfo.InvariantCulture,
                "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}" +
                "&current=weather_code,temperature_2m,wind_speed_10m" +
                "&daily=sunrise,sunset&timezone=auto&forecast_days=1",
                lat, lon);
            var root = GetJson(url);
            var current = (Dictionary<string, object>)root["current"];
            var daily = (Dictionary<string, object>)root["daily"];
            return new Weather
            {
                Code = (int)Num(current["weather_code"]),
                TemperatureC = Num(current["temperature_2m"]),
                WindKmh = Num(current["wind_speed_10m"]),
                Sunrise = Iso(((IList)daily["sunrise"])[0]),
                Sunset = Iso(((IList)daily["sunset"])[0]),
                UtcOffsetSeconds = (int)Num(root["utc_offset_seconds"]),
                FetchedUtc = DateTime.UtcNow,
            };
        }

        public static List<City> SearchCity(string name)
        {
            var url = "https://geocoding-api.open-meteo.com/v1/search?count=10&language=en&name=" +
                Uri.EscapeDataString(name);
            var root = GetJson(url);
            var list = new List<City>();
            object results;
            if (!root.TryGetValue("results", out results) || results == null) return list;
            // JavaScriptSerializer returns JSON arrays as ArrayList.
            foreach (Dictionary<string, object> r in (IList)results)
            {
                var parts = new List<string> { (string)r["name"] };
                object admin, country;
                if (r.TryGetValue("admin1", out admin)) parts.Add((string)admin);
                if (r.TryGetValue("country", out country)) parts.Add((string)country);
                list.Add(new City
                {
                    Label = string.Join(", ", parts),
                    Latitude = Num(r["latitude"]),
                    Longitude = Num(r["longitude"]),
                });
            }
            return list;
        }
    }
}
