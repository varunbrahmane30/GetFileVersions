using System;
using System.IO;
using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

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
        static void Main(string[] args)
        {
            using (var conn = new SqlConnection())
            {
                conn.ConnectionString = GetConnectionStringByName("FLCADDB");
                try
                {
                    conn.Open();
                    //var readQuery = $"select f.computername, f.swname, t.path from FLCSystemMember as f ,FileVersions as t where t.FileName = f.SWName and t.Host = f.ComputerName";

                    var cmd = new SqlCommand("sp_GetFileVersion", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    var path = string.Empty;
                    var hostName = string.Empty;
                    var computerName = string.Empty;
                    var softwareName = string.Empty;
                    var reader = cmd.ExecuteReader();

                    Console.WriteLine("----------------------- \t  FILES AND VERSIONS FOR HOST  \t -----------------------");

                    while (reader.Read())
                    {
                        path = reader["Path"].ToString();
                        computerName = reader["ComputerName"].ToString();
                        softwareName = reader["SWName"].ToString();

                        if (softwareName == "MailSave")
                            softwareName = "MailSave_Server";
                        

                        var fileInfo = FileVersionInfo.GetVersionInfo(Path.Combine(path, softwareName + ".exe"));
                       Console.WriteLine($"\n\t\t\t {fileInfo.ProductName} \t  Version number: {fileInfo.FileVersion}");

                        if (softwareName == "MailSave_Server")
                             softwareName = "MailSave";
                        //var insertQuery = $"update FileVersions set FileVersion=@FileVersion where FileName=@FileName and Host=@Host";
                        //var cmd2 = new SqlCommand(insertQuery, conn);
                        var cmd2 = new SqlCommand("sp_UpdateGetFileVersion", conn);
                        cmd2.CommandType = CommandType.StoredProcedure;

                        cmd2.Parameters.AddWithValue("@FileVersion", fileInfo.FileVersion);
                        cmd2.Parameters.AddWithValue("@FileName", softwareName);
                        cmd2.Parameters.AddWithValue("@Host", computerName);
                        
                        var result = cmd2.ExecuteNonQuery();
                     
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
            }
            Console.ReadKey(true);
        }
    }
    
}
