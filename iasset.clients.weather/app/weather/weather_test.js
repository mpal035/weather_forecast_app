'use strict';

//===============================================================================
//  Test cases for input validation only. The web api has its own set of unit tests
//===============================================================================
describe('Test suite for input validation', function () {

    var validationService;

    beforeEach(function () {
        module('myApp.weather');

        inject(function ($injector) {
            validationService = $injector.get('validator');
        });
    });

    it('should reject null input', function () {
        var result = validationService.validateInput(null);
        expect(result).toBe(false);
    });

    it('should reject empty input', function () {
        var result = validationService.validateInput('');
        expect(result).toBe(false);
    });

    it('should reject input longer than 100 chars', function () {
        var longInput = '';
        while (longInput.length <= 100)
            longInput += 'a';

        var result = validationService.validateInput(longInput);
        expect(result).toBe(false);
    });


    it('should accept non null, less than 100 char input', function () {
        var result = validationService.validateInput('test');
        expect(result).toBe(true);
    });


});