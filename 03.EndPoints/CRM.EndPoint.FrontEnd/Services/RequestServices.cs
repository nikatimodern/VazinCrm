using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Services
{
    public class RequestServices
    {

        public async Task<string> CreateRequestAsync(string urlApi, string obj, string method)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Set up the request content with the POST data
                    var content = new StringContent(obj, Encoding.UTF8, "application/json");

                    // Send the request (depending on the method: POST, PUT, etc.)
                    HttpResponseMessage response = null;

                    if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                    {
                        response = await client.PostAsync(urlApi, content);
                    }
                    else if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                    {
                        response = await client.GetAsync(urlApi);
                    }
                    // You can extend this with other methods like PUT, DELETE, etc.

                    // Ensure successful response status
                    response.EnsureSuccessStatusCode();

                    // Read the response content
                    string responseFromServer = await response.Content.ReadAsStringAsync();

                    // Return the server response
                    return responseFromServer;

                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., network issues, invalid responses)
                    return $"Error: {ex.Message}";
                }
            }
        }


    }
}
