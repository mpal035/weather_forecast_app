using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iasset.services.weather.Models
{
    /// <summary>
    /// This object will be passed to the client as the weather forecast result.
    /// </summary>
    public class WeatherInfo
    {
        public string Weather { get; set; }

        public string DataSource { get; set; }

        public string Location { get; set; }

        public string Time { get; set; }

        public string Wind { get; set; }

        public string Visibility { get; set; }

        public string SkyConditions { get; set; }

        public string Temperature { get; set; }

        public string DewPoint { get; set; }

        public string RelativeHumidity { get; set; }

        public string Pressure { get; set; }

        public string IconUri { get; set; }

        public string Precipitation { get; set; }
    }
}