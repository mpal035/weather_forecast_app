using iasset.services.weather.Models;
using Newtonsoft.Json;
using OpenWeatherMap;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using System.Xml;
using System.Xml.Linq;

namespace iasset.services.weather.Controllers
{
    /// <summary>
    /// The controller for all weather app related functionality. In future we may want to seperate the city searching functionality into its own controller
    /// </summary>
    [RoutePrefix("api/v1/weather")]
    public class WeatherController : ApiController
    {
        /// <summary>
        /// Returns a list of cities for a given country name
        /// </summary>
        /// <param name="countryName">Name of the country to query cities for</param>
        [HttpPost]
        [Route("GetCitiesByCountryName")]
        public IHttpActionResult GetCitiesByCountryName(string countryName)
        {
            //server side filtering
            FilterCountryOrCityName(countryName, "Country");

            SortedList<string, string> cities = new SortedList<string, string>();
            try
            {
                net.webservicex.www.GlobalWeather proxy = new net.webservicex.www.GlobalWeather();

                string result = proxy.GetCitiesByCountry(countryName);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(result);

                XmlElement root = doc.DocumentElement;
                XmlNodeList nodes = root.SelectNodes("Table");
                
                foreach (XmlNode node in nodes)
                {
                    string city = node.LastChild.LastChild.Value;
                    cities.Add(city, city);
                }
            }
            catch(Exception ex)
            {
                //didn't find any cities for the entered country
                var message = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    //TODO: log the exception in logfile/DB or appropriate logging mechanism. Do not expose exception info back to outside world
                    Content = new StringContent("An error occured while processing your request"),
                    ReasonPhrase = "An error occured while processing your request"
                };
                throw new HttpResponseException(message);
            }

            return Ok(cities.Keys.ToArray());
        }

        /// <summary>
        /// Server side input validation for getting country by name.
        /// 
        /// Assumptions: 
        /// 
        ///     1. We don't allow empty string/all search (even though the API does support this)
        ///     2. Country or city name cannot be longer than 100 characters
        /// </summary>
        /// <param name="name">Value of item to validate</param>
        /// <param name="entity">Type of item we are validating</param>
        private void FilterCountryOrCityName(string name, string entity)
        {
            if (string.IsNullOrEmpty(name))
            {
                var message = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(string.Format("Cannot search for {0} using empty string input", entity)),
                    ReasonPhrase = "Empty string value is not permitted"
                };
                throw new HttpResponseException(message);
            }
            
            if (name.Length > 100)
            {
                var message = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(string.Format("{0}'s name exceeds more than 100 characters", entity)),
                    ReasonPhrase = "Input string was out of range"
                };
                throw new HttpResponseException(message);
            }
        }

        /// <summary>
        /// Returns the weather forecast for the city and country provided
        /// </summary>
        /// <param name="cityName">Name of the city to query weather for</param>
        /// <param name="countryName">Name of the country to query weather for</param>
        [HttpPost]
        [Route("GetWeatherByCityAndCountry")]
        public async Task<IHttpActionResult> GetWeatherByCityAndCountry(string cityName, string countryName)
        {
            //server side filtering
            FilterCountryOrCityName(countryName, "Country");
            FilterCountryOrCityName(cityName, "City");

            //city needs to belong to country
            ValidateCityCountry(cityName, countryName);

            WeatherInfo result;
            try
            {
                net.webservicex.www.GlobalWeather proxy = new net.webservicex.www.GlobalWeather();
                string apiResult = proxy.GetWeather(cityName, countryName);

                //query OpenWeatherMap if we get no results from main web service
                if (apiResult == null || apiResult.Equals("Data Not Found"))
                {
                    //api key comes from config or some other persistent storage form
                    string apiKey = ConfigurationManager.AppSettings["OpenWeatherMapKey"];

                    //call OWM api using nuget package library
                    var client = new OpenWeatherMapClient(apiKey);
                    var owmWeatherForecast = await client.CurrentWeather.GetByName(cityName, MetricSystem.Metric, OpenWeatherMapLanguage.EN);

                    if (owmWeatherForecast != null)
                        result = WeatherInfoFromOWM(owmWeatherForecast);
                    else
                        result = FakeWeatherInfo(cityName);
                }
                else
                    result = WeatherInfoFromIasset(apiResult);
            }
            catch (Exception ex)
            {
                var message = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    //TODO: log the exception in logfile/DB or appropriate logging mechanism. Do not expose exception info back to outside world
                    Content = new StringContent("An error occured while processing your request"),
                    ReasonPhrase = "An error occured while processing your request"
                };
                throw new HttpResponseException(message);
            }

            return Ok(result);
        }

        /// <summary>
        /// Validates that a city name belongs to a country
        /// </summary>
        /// <param name="cityName"></param>
        /// <param name="countryName"></param>
        private void ValidateCityCountry(string cityName, string countryName)
        {
            var cities = GetCitiesByCountryName(countryName) as OkNegotiatedContentResult<string[]>;
            if(cities == null || cities.Content == null || !cities.Content.Contains<string>(cityName))
            {
                var message = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(string.Format("The city {0} does not belong to country {1}", cityName, countryName)),
                    ReasonPhrase = "Invalid input query"
                };
                throw new HttpResponseException(message);
            }
        }

        /// <summary>
        /// Converts a web api response object from Open Weather Map into a WeatherInfo to pass to the client
        /// </summary>
        /// <param name="owmWeatherForecast">Object returned from OWM web api</param>
        /// <returns></returns>
        private WeatherInfo WeatherInfoFromOWM(CurrentWeatherResponse owmWeatherForecast)
        {
            WeatherInfo result = new WeatherInfo();

            result.DataSource = "Open Weather Map";
            result.Location = owmWeatherForecast.City.Name;
            result.Pressure = owmWeatherForecast.Pressure.Value.ToString() + " " + owmWeatherForecast.Pressure.Unit;
            result.Temperature = owmWeatherForecast.Temperature.Value + " C";
            result.Time = owmWeatherForecast.LastUpdate.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");
            result.SkyConditions = owmWeatherForecast.Clouds.Name;
            result.Precipitation = owmWeatherForecast.Precipitation.Value.ToString() + " " + owmWeatherForecast.Precipitation.Unit;
            result.Weather = owmWeatherForecast.Weather.Value;
            result.IconUri = @"http://openweathermap.org/img/w/" + owmWeatherForecast.Weather.Icon + ".png";
            result.DewPoint = "No Data";
            result.RelativeHumidity = owmWeatherForecast.Humidity.Value.ToString() + owmWeatherForecast.Humidity.Unit;
            result.Visibility = "No Data";
            result.Wind = owmWeatherForecast.Wind.Speed.Name + " " + owmWeatherForecast.Wind.Speed.Value.ToString() + "m/s, "
                            + owmWeatherForecast.Wind.Direction.Name;

            return result;
        }


        /// <summary>
        /// Neither OWM nor iasset web service gave us a forecast, creating a fake one
        /// </summary>
        /// <returns></returns>
        private WeatherInfo FakeWeatherInfo(string cityName)
        {
            WeatherInfo result = new WeatherInfo();

            result.DataSource = "Fake Weather Service";
            result.Location = cityName;
            result.Pressure = "1014 hpa";
            result.Temperature = "23 C";
            result.Time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            result.SkyConditions = "Sunny";
            result.Precipitation = "0";
            result.Weather = "Sunny";
            result.IconUri = @"http://openweathermap.org/img/w/01d.png";
            result.DewPoint = "No Data";
            result.RelativeHumidity = "70%";
            result.Visibility = "No Data";
            result.Wind = "1.5 m/s";

            return result;
        }

        /// <summary>
        /// Assumption: since we haven't been able to get a successfull response from the GetWeather API call I will assume the 
        ///              response will be in XML with the same structure as the previous call and match all the params I'm using in WeatherInfo
        /// </summary>
        /// <param name="apiResult"></param>
        /// <returns></returns>
        private WeatherInfo WeatherInfoFromIasset(string apiResult)
        {
            WeatherInfo result = new WeatherInfo();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(apiResult);
            XmlElement root = doc.DocumentElement;
            XmlNode weatherResult = root.SelectSingleNode("Table");

            result.DataSource = "iasset Weather API";
            result.Location = weatherResult["Location"].Value;
            result.Pressure = weatherResult["Pressure"].Value;
            result.Temperature = weatherResult["Temperature"].Value;
            result.Time = weatherResult["Time"].Value;
            result.SkyConditions = weatherResult["SkyConditions"].Value;
            result.Precipitation = weatherResult["Precipitation"].Value;
            result.Weather = weatherResult["Weather"].Value;
            result.IconUri = weatherResult["IconUri"].Value;
            result.DewPoint = weatherResult["DewPoint"].Value;
            result.RelativeHumidity = weatherResult["RelativeHumidity"].Value;
            result.Visibility = weatherResult["Visibility"].Value;
            result.Wind = weatherResult["Wind"].Value;

            return result;
        }

    }
}
