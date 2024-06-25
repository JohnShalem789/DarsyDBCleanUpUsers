using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text.Encodings.Web;
using System.Text.Json;


namespace DarsyDBCleanUpUsers
{
    public class Program
    {
        public static void Main(string[] args)
        {
            GraphApiService graphApiService = new GraphApiService();
            graphApiService.CallGraphAPI();
        }
    }
}

public class GraphApiService
{   
    public void CallGraphAPI()
    {
        try
        {
            string token = ConfigurationManager.AppSettings["RequestUrl"];
            string requesturl = ConfigurationManager.AppSettings["BearerToken"];


            
            

            var searchRequestByEmail = new
            {
                requests = new[]
                {
                    new
                    {
                        entityTypes = new[]{"person"},
                        query = new
                        {
                            queryString = "v-tshalem"
                        }
                    }
                }
            };

            HttpClient httpClient = new HttpClient();

            string jsonRequest = Newtonsoft.Json.JsonConvert.SerializeObject(searchRequestByEmail);

            HttpContent content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = httpClient.PostAsync(requesturl, content).Result;

            string responseContent = response.Content.ReadAsStringAsync().Result;

            Console.WriteLine(responseContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message.ToString());
        }
    }
}