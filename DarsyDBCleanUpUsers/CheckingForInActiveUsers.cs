using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static DarsyDBCleanUpUsers.ViewModels;

namespace DarsyDBCleanUpUsers
{
    public static class DarsyUpdateInactiveUsers
    {
        public static string connectionString = ConfigurationManager.AppSettings["DbConnection"];
        public static HttpResponseMessage CallGraphAPI(RequestViewModel requestViewModel)
        {
            string responseContent = String.Empty;
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            try
            {
                string requesturl = ConfigurationManager.AppSettings["RequestUrl"];
                string token = ConfigurationManager.AppSettings["BearerToken"];

                HttpClient httpClient = new HttpClient();

                string jsonRequest = Newtonsoft.Json.JsonConvert.SerializeObject(requestViewModel);

                HttpContent content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                httpResponseMessage = httpClient.PostAsync(requesturl, content).Result;

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    Console.WriteLine("API call successful. Response:");
                }
                else
                {
                    Console.WriteLine($"Error calling API. Status code: {httpResponseMessage.StatusCode}");
                    throw new Exception("Error Calling the API, Please check the Bearer Token");
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

            return httpResponseMessage;
        }

        public static void RetriveAndProcessUsers()
        {

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Connection Successful");
                    string query = "SELECT UserEmail, UserAlias, UserDomain,IsActive FROM Users";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        string userAlias = reader["UserAlias"].ToString();
                        string userDomain = reader["UserDomain"].ToString();
                        string userEmail = reader["UserEmail"].ToString();
                        bool isActive = (bool)reader["IsActive"];

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
                        var httpResponseMessage = CallGraphAPI(searchRequest);
                        string responseContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                        Console.WriteLine($"Response from Graph API: {responseContent}");

                        // Check if the user exists based on the API response,Not enters if StatusCode is Not 200;  
                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            bool userExists = CheckIfUserExistsV1(responseContent);

                            if (userExists)
                            {
                                Console.WriteLine($"User '{userEmail}' exists in the Graph API response.");
                            }
                            else
                            {
                                Console.WriteLine($"User '{userEmail}' does not exist in the Graph API response.");
                                UpdateUserIsActiveStatus(userEmail);
                            }

                            Console.WriteLine($"Completed Checking User by Graph API: {userEmail}");
                            Console.WriteLine();
                        }
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

        public static bool CheckIfUserExistsV1(string responseContent)
        {
            try
            {
                dynamic responseObject = JsonConvert.DeserializeObject(responseContent);
                // Check if there are any hits
                if (responseObject?.value.Count > 0 && responseObject?.value[0].hitsContainers.Count > 0)
                {
                    var hitsContainer = responseObject?.value[0].hitsContainers[0];
                    if (hitsContainer?.total > 0)
                    {
                        // Check if the user is an OrganizationUser
                        var resource = hitsContainer.hits[0].resource;
                        if (resource != null && resource?.companyName == "Microsoft" &&
                            resource?.personType != null && resource?.personType.@class == "Person" &&
                            resource?.personType.subclass == "OrganizationUser")
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Graph API response: {ex.Message}");
                return false; // Return false in case of any error or unexpected response structure
            }
        }



        public static void UpdateUserIsActiveStatus(string userEmail)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        string updateQuery = "UPDATE Users SET IsActive = @IsActive WHERE UserEmail = @UserEmail";
                        SqlCommand updateCommand = new SqlCommand(updateQuery, connection, transaction);
                        updateCommand.Parameters.AddWithValue("@IsActive", 0);
                        updateCommand.Parameters.AddWithValue("@UserEmail", userEmail);

                        int rowsAffected = updateCommand.ExecuteNonQuery();
                        transaction.Commit();
                        if (rowsAffected > 0)
                        {
                            Console.WriteLine($"Successfully updated IsActive status for user '{userEmail}' to 0.");
                        }
                        else
                        {
                            Console.WriteLine($"No rows were updated for user '{userEmail}'.");
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"SQL Error while updating IsActive status: {sqlEx.Message}");
                // Optionally log the exception to a file or other logging mechanism
                throw new Exception("Error updating IsActive status in database.", sqlEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error while updating IsActive status: {ex.Message}");
                // Optionally log the exception to a file or other logging mechanism            
            }
        }
    }
}
