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
        static string ConnectionString { get; set; }
        static void Main(string[] args)
        {
            try
            {
                ConnectionString = GetConnectionStringByName("FLCADDB");
                Run();
            }
            finally
            {
                Console.WriteLine("stopped ...");
                Console.ReadKey(true);
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
        public static string GetConnectionStringByName(string dbName)
        {
            string returnValue = null;
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[dbName];

            if (settings != null)
                returnValue = settings.ConnectionString;

            return returnValue;
        }

        private static void InsertFileVersion(string host, SqlConnection conn, string softwareName)
        {
            var currentserviceExePath = string.Empty;
            var services = ServiceController.GetServices();


            foreach (var service in services)
            {
                if (service.ServiceName.ToLower().Contains(softwareName.ToLower()))
                {

                    currentserviceExePath = ConfigurationManager.AppSettings[softwareName.ToLower()];
                    //using (var wmiService = new ManagementObject("Win32_Service.Name='" + service.ServiceName + "'"))
                    //{
                    //    wmiService.Get();

                    //    currentserviceExePath = wmiService["PathName"].ToString();

                    //    if (currentserviceExePath.Contains(" "))
                    //        if(currentserviceExePath.Contains("AlwaysUp"))
                    //        {
                    //            var startfrom =@"D:\";
                    //            int index= currentserviceExePath.IndexOf(startfrom);
                    //            currentserviceExePath = currentserviceExePath.Substring(index, currentserviceExePath.IndexOf(".exe") + ".exe".Length);
                    //            if (currentserviceExePath.Contains(" "))
                    //            {

                    //            }
                    //        }
                    //        else
                    //        {
                    //            currentserviceExePath = currentserviceExePath.Substring(0, currentserviceExePath.IndexOf(".exe") + ".exe".Length);
                    //        }

                    //}
                    break;
                }
            }

            //"C:\Program Files (x86)\AlwaysUp\AlwaysUpService.exe"  "FLCAD MailSave (managed by AlwaysUpService)" "D:\Staging\MailSave\MailSave_Server.exe conf=FLCAD_vb6_local.ini" - k - m - ms - o "Sathish.M@kone.com" - h "SVC_FLCAD_INDIA@kone.com" - 3 "AlwaysUp" - g "mail" - 7 2 - r - w "D:\Staging\MailSave" - z 512 - rn - nt - f 3 0 - fd 5 1
            var existsOnMachine = !string.IsNullOrWhiteSpace(currentserviceExePath);
            if (existsOnMachine)
            {
                var fileInfo = FileVersionInfo.GetVersionInfo(Path.Combine(currentserviceExePath));
                // insert new record in table
                var insertCmd = new SqlCommand("[sp_FLCAD_FILE_VERSION_InsertFileVersion]", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                //Get the Database name from Connection String
                var builder = new SqlConnectionStringBuilder(ConnectionString);
                var databaseName = builder.InitialCatalog;

                insertCmd.Parameters.AddWithValue("@Host", host);
                insertCmd.Parameters.AddWithValue("@SoftwareName", softwareName);
                insertCmd.Parameters.AddWithValue("@FileVersion", fileInfo.FileVersion);
                insertCmd.Parameters.AddWithValue("@Path", currentserviceExePath);
                insertCmd.Parameters.AddWithValue("@Last_Update", DateTime.Now);
                insertCmd.Parameters.AddWithValue("@Database_Name", databaseName);

                insertCmd.ExecuteNonQuery();
            }
            
        }

        private static void UpdateFileVersion(string host, SqlConnection conn, string softwareName, string path)
        {

            // update existing record with new file version
            var fileInfo = FileVersionInfo.GetVersionInfo(Path.Combine(path));
            var updateFileVersionCmd = new SqlCommand("[sp_FLCAD_FILE_VERSION_UpdateFileVersion]", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            updateFileVersionCmd.Parameters.AddWithValue("@FileVersion", fileInfo.FileVersion);
            updateFileVersionCmd.Parameters.AddWithValue("@Last_Update", DateTime.Now);
            updateFileVersionCmd.Parameters.AddWithValue("@Host", host);
            updateFileVersionCmd.Parameters.AddWithValue("@Path", path);
            updateFileVersionCmd.Parameters.AddWithValue("@SoftwareName", softwareName);
            
            updateFileVersionCmd.ExecuteNonQuery();
        }

        private static void LogException(Exception ex)
        {
            string filePath = ConfigurationManager.AppSettings["ErrorLogPath"];

            if(File.Exists(filePath)==false)
            {
                FileStream read = System.IO.File.Create(filePath);

                read.Close();
            }

            using (StreamWriter writer = new StreamWriter(File.Open(filePath, FileMode.Append)))
            {
                writer.WriteLine("-----------------------------------------------------------------------------");
                writer.WriteLine("Date : " + DateTime.Now.ToString() + "\n");

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