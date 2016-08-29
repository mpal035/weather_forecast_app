'use strict';

//===============================================================================
//  App configuration and global variables
//===============================================================================

var SERVICE_BASE_ADDR = 'http://localhost:55791/';      //API service address

angular.module('myApp.weather', ['ngRoute'])
.config(['$routeProvider', function ($routeProvider) {
    $routeProvider.when('/weather', {
        templateUrl: 'weather/weather_view.html',
        controller: 'WeatherCtrl'
    });
}])

//===============================================================================
//  Using a factory to keep all of our promise calls in one place for extensibility
//===============================================================================
.factory('weatherService', ['$http', function ($http) {

    function getCitiesForCountry(countryName) {
        var request = {
            method: 'POST',
            url: SERVICE_BASE_ADDR + '/api/v1/weather/GetCitiesByCountryName?countryName=' + countryName,
            responseType: 'ArrayBuffer',
            cache: 'false'
        };

        return $http(request);
    }

    function getWeatherForecast(cityName, countryName) {
        var request = {
            method: 'POST',
            url: SERVICE_BASE_ADDR + '/api/v1/weather/GetWeatherByCityAndCountry?cityName=' + cityName + '&countryName=' + countryName,
            responseType: 'ArrayBuffer',
            cache: 'false'
        };

        return $http(request);
    }

    return {
        getCitiesForCountry: getCitiesForCountry,
        getWeatherForecast: getWeatherForecast
    };
}])


//===============================================================================
//  Client side input validator in the factory for extensibility
//===============================================================================
.factory('validator', [function () {

    function validateInput(strInput) {
        if (strInput == null || strInput == undefined)	//check for nulls
            return false;
        if (strInput != null && (strInput.length == 0 || strInput.length > 100))
            return false;

        return true;
    }

    return {
        validateInput: validateInput
    };
}])


//===============================================================================
//  Controller will keep all local level variables and control events
//===============================================================================
.controller('WeatherCtrl', ['$scope', '$http', 'weatherService', 'validator', function ($scope, $http, weatherService, validator) {

    //scope level local variables
    $scope.params = {
        Country: '',
        CountryCities: [],
        SelectedCity: null
    };
    $scope.weather = null;
    $scope.error_msg = null;

    //helper method to clear drop down control for city
    $scope.clearCountryCities = function () {
        $scope.params.CountryCities = [];
        $scope.params.SelectedCity = null;
    }

    //button events
    $scope.getCitiesForCountry = function () {
        $scope.weather = null;  //clear weather variable as we are doing a new search

        if (validator.validateInput($scope.params.Country)) {
            weatherService.getCitiesForCountry($scope.params.Country)
                .then(
                    function (result) {
                        if (result.data != null && result.data != undefined && result.data.length > 0) {
                            $scope.params.CountryCities = result.data;
                            $scope.error_msg = null;

                            if (result.data.length > 0)
                                $scope.params.SelectedCity = $scope.params.CountryCities[0];
                        }
                        else
                            $scope.error_msg = 'No cities found for country ' + $scope.params.Country;
                    },
                    function (error) {
                        if (error.data != null)
                            $scope.error_msg = error.data;
                        else
                            $scope.error_msg = 'An error occured during the processing of your request';
                    });
        }
        else {
            $scope.error_msg = 'Input for country name is invalid';
        }
    };

    $scope.getWeatherForecast = function () {
        if (validator.validateInput($scope.params.Country) && validator.validateInput($scope.params.SelectedCity)) {
            weatherService.getWeatherForecast($scope.params.SelectedCity, $scope.params.Country)
                .then(
                    function (result) {
                        if (result.data != null && result.data != undefined) {
                            $scope.weather = result.data;
                            $scope.error_msg = null;
                        }
                        else
                            $scope.error_msg = result.error;
                    },
                    function (error) {
                        if (error.data != null)
                            $scope.error_msg = error.data;
                        else
                            $scope.error_msg = 'An error occured during the processing of your request';
                    });
        }
        else {
            $scope.error_msg = 'Input for country name and/or city is invalid';
        }
    };


}]);


