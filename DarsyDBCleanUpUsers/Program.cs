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
                CheckingForNullsColumns.CheckingForNullColumns();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in the main process: {ex.Message}");
            }

        }
    }
}


