using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using iasset.services.weather.Controllers;
using System.Web.Http.Results;
using System.Web.Http;
using System.Threading.Tasks;
using iasset.services.weather.Models;
//using System.Web.Http;

namespace iasset.services.weather.Tests
{
    [TestClass]
    public class TestWeatherController
    {
        /// <summary>
        /// Should not be able to search for cities with an empty string for country name
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(HttpResponseException))]
        public void TestEmptyCountryInput()
        {
            var controller = new WeatherController();
            var result = controller.GetCitiesByCountryName(string.Empty);
        }

        /// <summary>
        /// Should not be able to search for cities with a country name longer than 100 chars
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(HttpResponseException))]
        public void TestEmptyLargeInput()
        {
            var controller = new WeatherController();

            string largeCountryName = GenerateLongString(101);

            var result = controller.GetCitiesByCountryName(largeCountryName);
        }

        /// <summary>
        /// A non existent country query should return an empty result
        /// </summary>
        [TestMethod]
        public void TestInvalidCountry()
        {
            var controller = new WeatherController();
            var result = controller.GetCitiesByCountryName("123");

            Assert.AreSame(result.GetType(), typeof(OkNegotiatedContentResult<string[]>));

            var content = (OkNegotiatedContentResult<string[]>)result;
            Assert.AreEqual(content.Content.Length, 0);
        }

        /// <summary>
        /// Test a valid country query
        /// </summary>
        [TestMethod]
        public void TestValidCountry()
        {
            var controller = new WeatherController();

            var result = controller.GetCitiesByCountryName("Australia");

            Assert.AreSame(result.GetType(), typeof(OkNegotiatedContentResult<string[]>));
            Assert.AreEqual(((OkNegotiatedContentResult<string[]>)result).Content.Length, 66);  //should have 66 cities returned for country Australia    
        }


        /// <summary>
        /// Should not be able to get any weather forecast for empty city/country name either
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(HttpResponseException))]
        public async Task TestEmptyWeatherForeCastQueryInput()
        {
            var controller = new WeatherController();
            var result = await controller.GetWeatherByCityAndCountry(string.Empty, string.Empty);
        }


        /// <summary>
        /// Should not be able to get any weather forecast for city/country names with names longer than 100 characters
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(HttpResponseException))]
        public async Task TestLongWeatherForeCastQueryInput()
        {
            var controller = new WeatherController();

            string largeCountryName = GenerateLongString(101);
            string largeCityName = GenerateLongString(101);
            var result = await controller.GetWeatherByCityAndCountry(largeCityName, largeCountryName);
        }


        /// <summary>
        /// The weather forecast query should only perform searches on valid city and country combination
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        [ExpectedException(typeof(HttpResponseException))]
        public async Task TestInvalidCityCountryWeatherForeCastQueryInput()
        {
            var controller = new WeatherController();
            var result = await controller.GetWeatherByCityAndCountry("Auckland", "Australia") as OkNegotiatedContentResult<WeatherInfo>;

            Assert.IsNotNull(result.Content);
            Assert.AreSame(result.Content.GetType(), typeof(WeatherInfo));
        }


        /// <summary>
        /// Test the base case with valid country and city name. Should get a non-null WeatherInfo object back
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestValidWeatherForeCastQueryInput()
        {
            var controller = new WeatherController();            
            var result = await controller.GetWeatherByCityAndCountry("Canberra", "Australia") as OkNegotiatedContentResult<WeatherInfo>;

            Assert.IsNotNull(result.Content);
            Assert.AreSame(result.Content.GetType(), typeof(WeatherInfo));            
        }


        /// <summary>
        /// As the iasset web api was not returning results I cannot test their data set, however we can test Open Weather MAP.
        /// There will be instances where the city name returned in the forecast is not the same as the query city name.
        /// Therefore we can't expect the OWM service to return the same city name as our query
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestNonMatchingWeatherForeCastQueryInput()
        {
            var controller = new WeatherController();
            var result = await controller.GetWeatherByCityAndCountry("Adelaide Airport", "Australia") as OkNegotiatedContentResult<WeatherInfo>;

            Assert.IsNotNull(result.Content);
            Assert.AreSame(result.Content.GetType(), typeof(WeatherInfo));

            //test names
            Assert.AreNotEqual(result.Content.Location, "Adelaide Airport");
        }


        /// <summary>
        /// Certain city names may throw exceptions in OWM, this test uses a valid city/country combination but still throws an exception
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        [ExpectedException(typeof(HttpResponseException))]
        public async Task TestExceptionCityCountryWeatherForeCastQueryInput()
        {
            var controller = new WeatherController();
            var result = await controller.GetWeatherByCityAndCountry("Bullsbrook Pearce Amo", "Australia") as OkNegotiatedContentResult<WeatherInfo>;

            Assert.IsNotNull(result.Content);
            Assert.AreSame(result.Content.GetType(), typeof(WeatherInfo));
        }


        #region Helper methods
        private string GenerateLongString(int length)
        {
            string largeStr = string.Empty;
            while (largeStr.Length <= length)
                largeStr += "a";
            return largeStr;
        }
        #endregion

    }
}
