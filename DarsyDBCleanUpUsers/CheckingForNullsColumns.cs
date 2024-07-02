using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarsyDBCleanUpUsers
{
    public static class CheckingForNullsColumns
    {
        public static void CheckingForNullColumns()
        {
            string connectionString = "Server=(LocalDb)\\MSSQLLocalDB;Database=DarsyTestDB;Trusted_Connection=True;";
            //string connectionString = "data source=tcp:darsydevserver.database.windows.net,1433;initial catalog=DevToolsDB;persist security info=True;User ID=DevDBUser;Password=D@rsyT00ls20@!;MultipleActiveResultSets=True;Connection Timeout=250;App=EntityFramework";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    Console.WriteLine("Connection successful!");
                    DataTable tablesSchema = connection.GetSchema("Tables");

                    foreach (DataRow table in tablesSchema.Rows)
                    {
                        string tableName = table["TABLE_NAME"].ToString();
                        Console.WriteLine($"Checking table: {tableName}");

                        CheckColumnsForNulls(connection, tableName);
                        Console.WriteLine(new string('-', 50));
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"SQL exception: {ex.Message}");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"Invalid operation: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                        Console.WriteLine("Connection closed.");
                    }
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void CheckColumnsForNulls(SqlConnection connection, string tableName)
        {
            try
            {
                DataTable columnsSchema = connection.GetSchema("Columns", new[] { null, null, tableName });

                foreach (DataRow column in columnsSchema.Rows)
                {
                    string columnName = column["COLUMN_NAME"].ToString();
                    CheckColumnForNulls(connection, tableName, columnName);
                    CheckForLastUpdateColumn(connection, tableName, columnName);
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL exception while processing table {tableName}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while processing table {tableName}: {ex.Message}");
            }
        }

        private static void CheckColumnForNulls(SqlConnection connection, string tableName, string columnName)
        {
            try
            {
                string query1 = $"SELECT COUNT(*) FROM [{tableName}] WHERE [{columnName}] IS NULL";
                string query2 = $"SELECT COUNT(*) FROM [{tableName}] WHERE [{columnName}] IS NOT NULL";
                using (SqlCommand command1 = new SqlCommand(query1, connection))
                using (SqlCommand command2 = new SqlCommand(query2, connection))
                {
                    int countNulls = Convert.ToInt32(command1.ExecuteScalar());
                    int countNotNulls = Convert.ToInt32(command2.ExecuteScalar());
                    int Totalrows = countNulls + countNotNulls;
                    if (countNulls > 0)
                    {
                        Console.WriteLine($"Table :'{tableName}', Column :'{columnName}', Total Column:'{countNulls + countNotNulls}',  NULL value(s): '{countNulls}'  NOT NULL value(s):'{countNotNulls}' .");
                    }

                    else if (countNulls == Totalrows)
                    {

                        Console.WriteLine($"Table '{tableName}', '{columnName}' entire column NULL");
                    }

                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL exception while checking nulls in {tableName}.{columnName}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while checking nulls in {tableName}.{columnName}: {ex.Message}");
            }
        }

        private static void CheckForLastUpdateColumn(SqlConnection connection, string tableName, string columnName)
        {
            if (columnName.ToLower().Contains("last") && columnName.ToLower().Contains("update") && !columnName.ToLower().Contains("by"))
            {
                try
                {
                    string lastUpdateQuery = $"SELECT MAX([{columnName}]) FROM [{tableName}]";
                    string firstUpdateQuery = $"SELECT MIN([{columnName}]) FROM [{tableName}]";
                    using (SqlCommand lastUpdateCommand = new SqlCommand(lastUpdateQuery, connection))
                    using (SqlCommand firstUpdateCommand = new SqlCommand(firstUpdateQuery, connection))
                    {
                        object lastUpdateDate = lastUpdateCommand.ExecuteScalar();
                        object firstUpdateDate = firstUpdateCommand.ExecuteScalar();
                        if (lastUpdateDate != DBNull.Value)
                        {
                            Console.WriteLine($"Table '{tableName}' was first updated on: {firstUpdateDate}");
                            Console.WriteLine($"Table '{tableName}' was last updated on: {lastUpdateDate}");

                        }
                        else
                        {
                            Console.WriteLine($"Table '{tableName}' has no updates recorded in column '{columnName}'.");
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"SQL exception while checking last update in {tableName}.{columnName}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while checking last update in {tableName}.{columnName}: {ex.Message}");
                }
            }
        }

    }
}
