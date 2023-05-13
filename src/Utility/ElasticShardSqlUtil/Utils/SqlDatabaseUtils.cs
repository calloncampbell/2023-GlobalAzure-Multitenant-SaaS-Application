using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ElasticShardSqlUtil.Utils
{
    /// <summary>
    /// Helper methods for interacting with SQL Databases.
    /// </summary>
    internal static class SqlDatabaseUtils
    {
        public const string MasterDatabaseName = "master";

        /// <summary>
        /// Test connection to the database
        /// </summary>
        /// <returns></returns>
        public static bool TryConnectToSqlDatabase()
        {
            string connectionString = ConfigurationUtils.GetConnectionString(ConfigurationUtils.ShardMapManagerServerName, MasterDatabaseName);

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                }

                ConsoleUtils.WriteSuccess($"Successfully connected to database server '{ConfigurationUtils.ShardMapManagerServerName}'.");

                return true;
            }
            catch (SqlException e)
            {
                ConsoleUtils.WriteWarning("Failed to connect to database server. If this connection string is incorrect, please update the SQL database settings in appsettings.json.");
                ConsoleUtils.WriteError("Exception details: {0}", e.Message);
                return false;
            }
        }

        /// <summary>
        /// Check if database exists
        /// </summary>
        /// <param name="server"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static bool DatabaseExists(string server, string db)
        {
            using (var connection = new SqlConnection(ConfigurationUtils.GetConnectionString(server, MasterDatabaseName)))
            {
                connection.Open();

                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = "select count(*) from sys.databases where name = @dbname";
                cmd.Parameters.AddWithValue("@dbname", db);
                cmd.CommandTimeout = 60;
                
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                
                bool exists = count > 0;
                return exists;
            }
        }

        /// <summary>
        /// Check if database is online
        /// </summary>
        /// <param name="server"></param>
        /// <param name="db"></param>
        /// <returns></returns>
        public static bool DatabaseIsOnline(string server, string db)
        {
            using (var connection = new SqlConnection(ConfigurationUtils.GetConnectionString(server, MasterDatabaseName)))
            {
                connection.Open();

                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = "select count(*) from sys.databases where name = @dbname and state = 0"; // online
                cmd.Parameters.AddWithValue("@dbname", db);
                cmd.CommandTimeout = 60;

                int count = Convert.ToInt32(cmd.ExecuteScalar());

                bool exists = count > 0;
                return exists;
            }
        }
        
        /// <summary>
        /// Escapes a database name with brackets if it contains special characters.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="db"></param>
        /// <param name="sqlScript"></param>
        public static void ExecuteSqlScript(string server, string db, string sqlScript)
        {
            ConsoleUtils.WriteMessage("Executing script {0}", sqlScript);

            //// create sqlConnection and run the script
            //using (var connection = new SqlConnection(ConfigurationUtils.GetConnectionString(server, db)))
            //{
            //    //using (ReliableSqlConnection conn = new ReliableSqlConnection(
            //    //    Configuration.GetConnectionString(server, db),
            //    //    SqlRetryPolicy,
            //    //    SqlRetryPolicy))
            //    //{
            //    connection.Open();

            //    // Read the script file
            //    string script = File.ReadAllText(schemaFile);

            //    // Split the script on "GO" statements
            //    IEnumerable<string> commandStrings = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            //    // Execute each command in the script
            //    foreach (string commandString in commandStrings)
            //    {
            //        if (!string.IsNullOrWhiteSpace(commandString))
            //        {
            //            SqlCommand cmd = connection.CreateCommand();
            //            cmd.CommandText = commandString;
            //            cmd.CommandTimeout = 60;
            //            connection.ExecuteCommand(cmd);
            //        }
            //    }
            //}

            // create sqlConnection and run the script
            using (var connection = new SqlConnection(ConfigurationUtils.GetConnectionString(server, db)))
            {
                connection.Open();
                SqlCommand cmd = connection.CreateCommand();

                // Read the commands from the sql script file
                IEnumerable<string> commands = ReadSqlScript(sqlScript);

                foreach (string command in commands)
                {
                    if (!string.IsNullOrWhiteSpace(command))
                    {                        
                        cmd.CommandText = command;
                        cmd.CommandTimeout = 60;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Reads the SQL script file and returns the commands.
        /// </summary>
        /// <param name="scriptFile"></param>
        /// <returns></returns>
        private static IEnumerable<string> ReadSqlScript(string scriptFile)
        {
            List<string> commands = new List<string>();
            using (TextReader tr = new StreamReader(scriptFile))
            {
                StringBuilder sb = new StringBuilder();
                string line;
                while ((line = tr.ReadLine()) != null)
                {
                    if (line == "GO")
                    {
                        commands.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
            }

            return commands;
        }

        /// <summary>
        /// Escapes a SQL object name with brackets to prevent SQL injection.
        /// </summary>
        private static string BracketEscapeName(string sqlName)
        {
            return '[' + sqlName.Replace("]", "]]") + ']';
        }        
    }
}
