using Newtonsoft.Json;
using System.Collections.Generic;

namespace WeatherMonitoring
{
    class WeatherResponse
    {

        public List<Weather> weather { get; set; }
        public Main main { get; set; }
        public Wind wind { get; set; }
        public Clouds clouds { get; set; }
        public Sys sys { get; set; }

        [JsonObject("coord")]
        public class Coord
        {
            //longitude 
            public double lon { get; set; }
            //latitude
            public double lat { get; set; }
        }

        [JsonObject("weather")]
        public class Weather
        {
            public int id { get; set; }
            public string main { get; set; }
            public string description { get; set; }
            public string icon { get; set; }
        }

        [JsonProperty("base")]
        public string Base { get; set; }

        [JsonObject("main")]
        public class Main
        {
            public double temp { get; set; }
            public double feels_like { get; set; }
            public double temp_min { get; set; }
            public double temp_max { get; set; }
            public double pressure { get; set; }
            public double humidity { get; set; }
        }
        public int visibility { get; set; }

        [JsonObject("wind")]
        public class Wind
        {
            public double speed { get; set; }
            public int deg { get; set; }
        }

        [JsonObject("clouds")]
        public class Clouds
        {
            public int all { get; set; }
        }

        public long dt { get; set; }

        [JsonObject("Sys")]
        public class Sys
        {
            public int type { get; set; }
            public int id { get; set; }
            public string country { get; set; }

            public long sunrise { get; set; }
            public long sunset { get; set; }
        }

        public int timezone { get; set; }
        public int id { get; set; }
        public string name { get; set; }

        public int cod { get; set; }
    }
}
