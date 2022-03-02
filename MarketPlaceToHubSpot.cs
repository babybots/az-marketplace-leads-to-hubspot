using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using RestSharp;

namespace marketplaceleadstohubspot
{
    public static class MarketPlaceToHubSpot
    {
        [FunctionName("MarketPlaceToHubSpot")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            // Hubspot API information
            string hubspotAPIKey = System.Environment.GetEnvironmentVariable("hubspotAPIKEY");
            string baseURI = "https://api.hubapi.com/contacts/v1/contact/?hapikey=";
            string URI = baseURI + hubspotAPIKey;

            // Process leads from the marketplace
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Lead deserializedMarketplaceLead = JsonConvert.DeserializeObject<Lead>(requestBody);


            // You can use as much properties as you want, as long as they exist in HubSpot.
            // Do a HTTP get to https://api.hubapi.com/properties/v1/contacts/properties?hapikey=<APIKEY> to check
            var details = new HubSpotContactPoperties();
            details.properties = new List<Property>
        {
            new Property { property = "firstname", value = deserializedMarketplaceLead.userDetails.firstName },
            new Property { property = "lastName", value = deserializedMarketplaceLead.userDetails.lastName },
            new Property { property = "email", value = deserializedMarketplaceLead.userDetails.email },
            new Property { property = "website", value = "NotProvidedFromAzureMarketPlace" },
            new Property { property = "company", value = deserializedMarketplaceLead.userDetails.company },
            new Property { property = "phone", value = deserializedMarketplaceLead.userDetails.phone },
            new Property { property = "address", value = "NotProvidedFromAzureMarketPlace"},
            new Property { property = "city", value = "NotProvidedFromAzureMarketPlace" },
            new Property { property = "state", value = "NotProvidedFromAzureMarketPlace" },
            new Property { property = "zip", value = "NotProvidedFromAzureMarketPlace" },
            // You could use deserializedMarketplaceLead.leadSource here but it needs to exist in Hubspot!
            new Property { property = "message", value = "Offer Title: " + deserializedMarketplaceLead.offerTitle }
        };
            // Serialize details to jsonBody
            string jsonBody = JsonConvert.SerializeObject(details);

            // Write Leads to HubSpot
            string result = await WriteLeadHubSpot(URI, jsonBody);

            // Write contact to hubspot
            static async Task<string> WriteLeadHubSpot(string URI, string jsonBody)
            {
                var client = new RestClient(URI);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/json");

                request.AddParameter("application/json", jsonBody, ParameterType.RequestBody);
                IRestResponse response = await client.ExecuteAsync(request);
                return response.Content;
            }


            return new OkObjectResult(JsonConvert.DeserializeObject(result));
        }
    }
}
