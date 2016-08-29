'use strict';

/* https://github.com/angular/protractor/blob/master/docs/toc.md */

describe('iasset weather forecast app e2e tests', function() {


    it('should automatically redirect to /weather when location hash/fragment is empty', function() {
        browser.get('index.html');
        expect(browser.getLocationAbsUrl()).toMatch("/weather");
    });


    describe('weather', function() {

        beforeEach(function() {
            browser.get('index.html#!/weather');
        });

    });

});