using System;
using System.IO;
using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.ServiceProcess;

namespace GetVersions
{
    internal class Program
    {  
        public static string GetConnectionStringByName(string dbName)
        {
            string returnValue = null;
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[dbName];

            if (settings != null)
                returnValue = settings.ConnectionString;

            return returnValue;
        }

        static string ConnectionString { get; set; }

        static void Main(string[] args)
        {
            int loop = 0;
            ConnectionString = GetConnectionStringByName("FLCADDB");
            while ( loop <= 1)
            {
                Run();
                loop++;
            }
            Console.ReadKey(true);
        }

        private static void Run()
        {
            var computerName = Environment.MachineName;
            var softwareNames = GetSoftwareNames(computerName);
            UpdateFileVersions(computerName, softwareNames);
        }

        private static List<string> GetSoftwareNames(string computerName)
        {
            var computerNames = new List<string>();
            using (var conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();

                    // Get the software name based on FLCSystemMember table.
                    var cmd = new SqlCommand("sp_FLCAD_FILE_VERSION_GetFileVersion", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@ComputerName", computerName);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        computerNames.Add(reader["SWName"].ToString());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    LogException(ex);
                }
            }

            return new HashSet<string>(computerNames).ToList();
        }


        private static void UpdateFileVersions(string host, List<string> softwareNames)
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    
                    foreach (var softwareName in softwareNames)
                    {
                        // Get existing record from table
                        var cmd = new SqlCommand("sp_FLCAD_FILE_VERSION_CompareFileVersion", conn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@Host", host);
                        cmd.Parameters.AddWithValue("@SoftwareName", softwareName);

                        var path = string.Empty;
                        var fileVersion = string.Empty;
                        var swname = string.Empty;
                        var reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            path = reader["Path"].ToString();
                            fileVersion = reader["FileVersion"].ToString();
                             swname= reader["SoftwareName"].ToString();

                        }

                        var recordExists = !string.IsNullOrWhiteSpace(swname);
                        if (recordExists)
                        {
                            UpdateFileVersion(host, conn, softwareName, path);
                        }   
                        else
                        {
                            InsertFileVersion(host, conn, softwareName);
                        }
                    }
                   
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    LogException(ex);
                }
            }
        }

        private static void InsertFileVersion(string host, SqlConnection conn, string softwareName)
        {

            //var result = softwareName.Contains("");

            string ServiceName = softwareName;
            string currentserviceExePath = string.Empty;

            //Get the Database name from Connection String
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConnectionString);
            String databaseName = builder.InitialCatalog;

            ServiceController[] services = ServiceController.GetServices();

            foreach(var service in services)
            {
                if (service.ServiceName.Contains(ServiceName))
                {
                    using (ManagementObject wmiService = new ManagementObject("Win32_Service.Name='" + service.ServiceName + "'"))
                    {
                        wmiService.Get();

                        currentserviceExePath = wmiService["PathName"].ToString();
                        if (currentserviceExePath.Contains(" "))
                            currentserviceExePath = currentserviceExePath.Substring(0, currentserviceExePath.IndexOf(" "));
                    }
                    break;
                }
            }
            // Get the Path of Windows service.
           

            // insert new record in table
            var insertCmd = new SqlCommand("[sp_FLCAD_FILE_VERSION_UpdateFileVersion]", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            insertCmd.Parameters.AddWithValue("@Host", host);
            insertCmd.Parameters.AddWithValue("@SoftwareName", softwareName);
            insertCmd.Parameters.AddWithValue("@Path", currentserviceExePath);
            insertCmd.Parameters.AddWithValue("@Last_Update", DateTime.Now);
            insertCmd.Parameters.AddWithValue("@Database_Name", databaseName);


            insertCmd.ExecuteNonQuery();
            
        }

        private static void UpdateFileVersion(string host, SqlConnection conn, string softwareName, string path)
        {

            // update existing record with new file version
            var fileInfo = FileVersionInfo.GetVersionInfo(Path.Combine(path));
            var updateFileVersionCmd = new SqlCommand("update FileVersions set FileVersion=@FileVersion, Last_Update=@Last_Update,Path=@Path where Host=@ComputerName and SoftwareName=@SoftwareName", conn)
            {
                CommandType = CommandType.Text
            };

            updateFileVersionCmd.Parameters.AddWithValue("@FileVersion", fileInfo.FileVersion);
            updateFileVersionCmd.Parameters.AddWithValue("@Last_Update", DateTime.Now);
            updateFileVersionCmd.Parameters.AddWithValue("@ComputerName", host);
            updateFileVersionCmd.Parameters.AddWithValue("@Path", path);
            updateFileVersionCmd.Parameters.AddWithValue("@SoftwareName", softwareName);

            updateFileVersionCmd.ExecuteNonQuery();
        }

        private static void LogException(Exception ex)
        {
            string filePath = ConfigurationManager.AppSettings["ErrorLogPath"];
            using (StreamWriter writer = new StreamWriter(File.Open(filePath, FileMode.Append)))
            {
                writer.WriteLine("-----------------------------------------------------------------------------");
                writer.WriteLine("Date : " + DateTime.Now.ToString());
                writer.WriteLine();

                while (ex != null)
                {
                    writer.WriteLine(ex.GetType().FullName);
                    writer.WriteLine("Message : " + ex.Message);
                    ex = ex.InnerException;
                }
            }
        }

    } 
}






/*
           using (var conn = new SqlConnection())
           {
               conn.ConnectionString = GetConnectionStringByName("FLCADDB");
               SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(conn.ConnectionString);
               String databaseName = builder.InitialCatalog;

               try
               {
                   conn.Open();

                   var cmd = new SqlCommand("sp_GetFileVersion", conn);
                   cmd.CommandType = CommandType.StoredProcedure;

                   var path = string.Empty;
                   var computerName = string.Empty;
                   var softwareName = string.Empty;
                   var reader = cmd.ExecuteReader();

                   Console.WriteLine("----------------------- \t  FILES AND VERSIONS FOR HOST  \t -----------------------");

                   while (reader.Read())
                   {

                       computerName = reader["ComputerName"].ToString();
                       softwareName = reader["SWName"].ToString();


                       path = reader["Path"].ToString();

                       if (path != String.Empty)
                       {
                           GetFileInformation(path, softwareName, computerName);
                       }
                       else
                       {
                           var cmd3 = new SqlCommand("", conn);
                           cmd.CommandType = CommandType.StoredProcedure;
                       }
                   }      
               }
               catch (Exception ex)
               {
                   Console.WriteLine(ex.Message);

                   string filePath =ConfigurationManager.AppSettings["ErrorLogPath"];

                   using (StreamWriter writer = new StreamWriter(File.Open(filePath,FileMode.Append)))
                   {
                       writer.WriteLine("-----------------------------------------------------------------------------");
                       writer.WriteLine("Date : " + DateTime.Now.ToString());
                       writer.WriteLine();

                       while (ex != null)
                       {
                           writer.WriteLine(ex.GetType().FullName);
                           writer.WriteLine("Message : " + ex.Message);

                           ex = ex.InnerException;
                       }
                   }
               }

               FileVersionInfo GetFileInformation(String path,string softwareName, string computerName)
               {
                   if (softwareName == "MailSave")
                       softwareName = "MailSave_Server";

                   var fileInfo = FileVersionInfo.GetVersionInfo(Path.Combine(path, softwareName + ".exe"));
                   Console.WriteLine($"\n\t\t\t {fileInfo.ProductName} \t  Version number: {fileInfo.FileVersion}");

                   if (softwareName == "MailSave_Server")
                       softwareName = "MailSave";

                   var cmd2 = new SqlCommand("sp_UpdateGetFileVersion", conn);
                   cmd2.CommandType = CommandType.StoredProcedure;

                   cmd2.Parameters.AddWithValue("@FileVersion", fileInfo.FileVersion);
                   cmd2.Parameters.AddWithValue("@FileName", softwareName);
                   cmd2.Parameters.AddWithValue("@Host", computerName);
                   cmd2.Parameters.AddWithValue("@Last_Update", DateTime.Now);
                   cmd2.Parameters.AddWithValue("@Database_Name", databaseName);


                   var result = cmd2.ExecuteNonQuery();
                   return fileInfo;
               }
           }

           */