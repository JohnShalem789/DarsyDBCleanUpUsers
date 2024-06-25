using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text.Encodings.Web;
using System.Text.Json;
using static DarsyDBCleanUpUsers.ViewModels;


namespace DarsyDBCleanUpUsers
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                DarsyUpdateInactiveUsers.RetriveAndProcessUsers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in the main process: {ex.Message}");
            }

        }
    }
}

public class DarsyUpdateInactiveUsers
{   
    public static string CallGraphAPI(RequestViewModel requestViewModel)
    {
        string responseContent = String.Empty;

        try
        {
            string requesturl = ConfigurationManager.AppSettings["RequestUrl"];
            string token = ConfigurationManager.AppSettings["BearerToken"];
           
            HttpClient httpClient = new HttpClient();

            string jsonRequest = Newtonsoft.Json.JsonConvert.SerializeObject(requestViewModel);

            HttpContent content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage response = httpClient.PostAsync(requesturl, content).Result;

            responseContent = response.Content.ReadAsStringAsync().Result;            

            if (response.IsSuccessStatusCode)
            {
                string result = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine("API call successful. Response:");
            }
            else
            {
                Console.WriteLine($"Error calling API. Status code: {response.StatusCode}");
            }         
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine($"HTTP Request Error: {httpEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General Error: {ex.Message}");
        }

        return responseContent;
    }

    public static void RetriveAndProcessUsers()
    {
        string connectionString = ConfigurationManager.AppSettings["DbConnection"];
        
        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                Console.WriteLine("Connection Successful");
                string query = "SELECT UserEmail, UserAlias, UserDomain FROM Users";
                SqlCommand command = new SqlCommand(query, connection);
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string userAlias = reader["UserAlias"].ToString();
                    string userDomain = reader["UserDomain"].ToString();
                    string userEmail = reader["UserEmail"].ToString();

                    var searchRequest = new RequestViewModel
                    {
                        requests = new UserRequestViewModel[] {
                            new UserRequestViewModel()
                            {
                                entityTypes = new[] { "person" },
                                query = new QueryViewModel
                                {
                                    queryString = $"{userEmail}"
                                }
                            }
                        }
                    };

                    Console.WriteLine($"Started Checking User by Graph API: {userEmail}");
                    string responseContent = CallGraphAPI(searchRequest);
                    Console.WriteLine($"Response from Graph API: {responseContent}");

                    // Check if the user exists based on the API response
                    bool userExists = CheckIfUserExists(responseContent);

                    if (userExists)
                    {
                        Console.WriteLine($"User '{userEmail}' exists in the Graph API response.");
                        // Perform further actions if the user exists
                    }
                    else
                    {
                        Console.WriteLine($"User '{userEmail}' does not exist in the Graph API response.");
                        // Perform actions for non-existent users
                    }

                    Console.WriteLine($"Completed Checking User by Graph API: {userEmail}");
                    Console.WriteLine(); 

                }              
                reader.Close();
            }
        }
        catch (SqlException sqlEx)
        {
            Console.WriteLine($"SQL Error: {sqlEx.Message}");
            // Optionally log the exception to a file or other logging mechanism
            throw new Exception("Error retrieving data from database.", sqlEx);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error constructing request: {ex.Message}");
            // Optionally log the exception to a file or other logging mechanism
            throw; // Rethrow the exception to be caught in the Main method
        }
    }

    public static bool CheckIfUserExists(string responseContent)
    {
        try
        {
            dynamic responseObject = JsonConvert.DeserializeObject(responseContent);
            int totalHits = responseObject.value[0].hitsContainers[0].total;

            return totalHits > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing Graph API response: {ex.Message}");
            return false; // Return false in case of any error or unexpected response structure
        }
    }
}
