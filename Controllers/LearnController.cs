using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TodoApi.Controllers
{
    public class PlanetApiData
    {
        public String? Name { get; set; }
        public int? Population { get; set; }
        public String[] Residents  { get; set; }
        public String[] Films { get; set; }

        public PlanetApiData()
        {
            Residents = new String[] {};
            Films = new String[] {};
        }
    }

    public class Planet
    {
        public String? Name { get; set; }
        public int? Population { get; set; }
        public List<Resident>? Residents  { get; set; }
        public List<Film>? Films { get; set; }

        public Planet()
        {
            Residents = new List<Resident>();
            Films = new List<Film>();
        }
    }

    public class Resident {
        public String? Birth_Year { get; set; }
        public String? Name { get; set; }
        public String? Gender { get; set; }
    }

    public class Film {
        public String? Title { get; set; }
        public String? Director { get; set; }
    }

    public class CustomError {
        public int code;
        public String errormsg;

        public CustomError() {
            errormsg = "";
        }
    }

    [Route("api/learn")]
    [ApiController]
    public class LearnController : ControllerBase
    {
        public String populateErrorMessage = "";

        public LearnController()
        {
        }

        // GET: api/learn/test
        [HttpGet("test")]
        public ActionResult getTest()
        {
             return Content("Hello world learn 2 test");
        }

        [HttpGet("planets")]
        public ActionResult getPlanets()
        {
             return Content("Hi and welcome to the universe.\n\t GET /planets/{id} to learn about planets.\n\n");
        }

        [HttpGet("planets/{id}")]
        public async Task<IActionResult> getPlanet(long id)
        {
            //return this.StatusCode(418, "hello teapots are cool");

            // Validation for the sake of validation
            if (id < 1) {
                 return this.StatusCode(400, "error: id must be a postive integer\n");
            }

            // Disable swapi.dev SSL validation because it fails..
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback =
                (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return true;
            };

            // Setup HTTP Client
            HttpClient client = new HttpClient(handler);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // Make a request to the API
            HttpResponseMessage apiResponse = await client.GetAsync("http://swapi.dev/api/planets/" + id);
            if (apiResponse.IsSuccessStatusCode)
            {

                // var body = await apiResponse.Content.ReadAsStringAsync();
                // await Response.WriteAsync("apiResponse from api:\n\n");
                // await Response.WriteAsync(body);

                PlanetApiData? p = await apiResponse.Content.ReadFromJsonAsync<PlanetApiData>();

                if (p == null) {
                    return this.StatusCode(500, "error: planet out of orbit. Couldn't decode data from API.\n");
                }

                // await Response.WriteAsync("Downloaded api data... \n\t" + p.Name + "\n\t" + p.Population + "\n\n");

                Planet returnPlanet = new Planet();
                returnPlanet.Name = p.Name;
                returnPlanet.Population = p.Population;

                int maxApiCalls = 3;

                //
                // Get residents
                //
                if (p.Residents.Length > 0) {
                    foreach (string residentUrl in p.Residents) {

                        // Save some time by ignoring residents
                        if (maxApiCalls-- == 0) {
                            break;
                        }

                        var nonHttpsUrl = residentUrl.Replace("https://", "http://");
                        client = new HttpClient(handler);

                        HttpResponseMessage residentApiResponse = await client.GetAsync(nonHttpsUrl);
                        if (!residentApiResponse.IsSuccessStatusCode) {
                            return this.StatusCode(500, "error: Couldnt fetch from api backend /api/people/" + id + "\n");
                        }
                        var tmpObject = await residentApiResponse.Content.ReadFromJsonAsync<Resident>();
                        if (tmpObject == null) {
                            return this.StatusCode(500, "error: Couldnt decode /api/people/" + id + "\n");
                        }
                        returnPlanet.Residents.Add((Resident)tmpObject);
                    }
                }

                //
                // Get films
                //
                populateSomething<Film>(handler, p.Films, returnPlanet.Films);


                await Response.WriteAsync(JsonSerializer.Serialize(returnPlanet));
                await Response.CompleteAsync();

                return Ok();
            }

            var errorMessage = "error: planet out of orbit. Couldn't decode data from API /planets/" + id + ".\n";
            errorMessage += "apiresponse code: " + apiResponse.StatusCode + "\n\n";
            errorMessage += "path: " + apiResponse.Headers.ToString + "\n";
            errorMessage += "path: " + apiResponse.RequestMessage.RequestUri + "\n";
            return this.StatusCode(500, errorMessage);
        }

        private async void populateSomething<T>(HttpClientHandler handler, string[] urls, List<T> listToPopulate)
        {
            int maxApiCalls = 3;

            foreach (string url in urls) {
                // Save some time
                if (maxApiCalls-- == 0) {
                    break;
                }

                // We dont have time to debug certificate errors
                var nonHttpsUrl = url.Replace("https://", "http://");
                HttpClient client = new HttpClient(handler);

                // Fetch api response
                HttpResponseMessage swApiResponse = await client.GetAsync(nonHttpsUrl);
                if (!swApiResponse.IsSuccessStatusCode) {
                    // return this.StatusCode(500, "error: fetch api url " + nonHttpsUrl + "\n");
                    populateErrorMessage = "error: fetch api url " + nonHttpsUrl + "\n";
                }
                var tmpObject = await swApiResponse.Content.ReadFromJsonAsync<T>();
                if (tmpObject == null) {
                    // return this.StatusCode(500, "error: Couldnt decode " + nonHttpsUrl + "\n");
                    populateErrorMessage = "error: Couldnt decode " + nonHttpsUrl + "\n";
                }
                listToPopulate.Add((T)tmpObject);
            }
        }
    }
}