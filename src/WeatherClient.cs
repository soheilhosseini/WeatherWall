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
        // The larger of the low (< 3 km) and mid (3-8 km) cloud cover, in %. High, thin cloud is left
        // out: it can make the WMO code "overcast" while the sun still shines. Null = not known.
        public int? LowMidCloudPercent;
        public DateTime Sunrise;    // in the city's local time
        public DateTime Sunset;
        public int UtcOffsetSeconds;
        public DateTime FetchedUtc;

        public DateTime CityNow
        {
            get { return DateTime.UtcNow.AddSeconds(UtcOffsetSeconds); }
        }

        // One line for settings.json; sunrise and sunset keep only their time of day, which is all
        // SlotFor uses, so an old copy still gives the right part of the day.
        public string Save()
        {
            return string.Join(";", new[]
            {
                Code.ToString(CultureInfo.InvariantCulture), TemperatureC.ToString(CultureInfo.InvariantCulture),
                WindKmh.ToString(CultureInfo.InvariantCulture),
                LowMidCloudPercent.HasValue ? LowMidCloudPercent.Value.ToString(CultureInfo.InvariantCulture) : "",
                Sunrise.TimeOfDay.Ticks.ToString(CultureInfo.InvariantCulture), Sunset.TimeOfDay.Ticks.ToString(CultureInfo.InvariantCulture),
                UtcOffsetSeconds.ToString(CultureInfo.InvariantCulture), FetchedUtc.Ticks.ToString(CultureInfo.InvariantCulture),
            });
        }

        public static Weather Load(string saved)
        {
            try
            {
                var p = saved.Split(';');
                Func<int, long> num = i => long.Parse(p[i], CultureInfo.InvariantCulture);
                return new Weather
                {
                    Code = (int)num(0),
                    TemperatureC = double.Parse(p[1], CultureInfo.InvariantCulture),
                    WindKmh = double.Parse(p[2], CultureInfo.InvariantCulture),
                    LowMidCloudPercent = p[3] == "" ? (int?)null : (int)num(3),
                    Sunrise = DateTime.Today + new TimeSpan(num(4)),
                    Sunset = DateTime.Today + new TimeSpan(num(5)),
                    UtcOffsetSeconds = (int)num(6),
                    FetchedUtc = new DateTime(num(7), DateTimeKind.Utc),
                };
            }
            catch (Exception)
            {
                return null;   // missing or from an older version
            }
        }
    }

    public class City
    {
        public string Label;
        public double Latitude;
        public double Longitude;
        public string Timezone;   // IANA, e.g. "Asia/Tehran"

        public override string ToString() { return Label; }
    }

    // WebClient with a 20 s timeout for small requests: the default 100 s keeps the app waiting
    // long after a network drop (asynchronous downloads ignore it).
    public class TimedWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            request.Timeout = 20000;
            return request;
        }
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
            using (var wc = new TimedWebClient())
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
                "&current=weather_code,temperature_2m,wind_speed_10m,cloud_cover_low,cloud_cover_mid" +
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
                LowMidCloudPercent = (int)Math.Round(Math.Max(Num(current["cloud_cover_low"]), Num(current["cloud_cover_mid"]))),
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
                object admin, country, zone;
                if (r.TryGetValue("admin1", out admin)) parts.Add((string)admin);
                if (r.TryGetValue("country", out country)) parts.Add((string)country);
                list.Add(new City
                {
                    Label = string.Join(", ", parts),
                    Latitude = Num(r["latitude"]),
                    Longitude = Num(r["longitude"]),
                    Timezone = r.TryGetValue("timezone", out zone) ? (string)zone : null,
                });
            }
            return list;
        }
    }
}
