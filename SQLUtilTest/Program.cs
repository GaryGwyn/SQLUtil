using SQLUtil;
using System;

namespace SQLUtilTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = @"Server=.\SQLExpress;Trusted_Connection=True;"; //Use the proper server name
            string backupPath = @"C:\LocalSQLBackup\"; //Use a valid local path
            string databaseName = "TESTDB"; //Use a valid existing database

            //Test backup
            Console.WriteLine($"Backing up: {databaseName}");
            string backup = Server.Backup(connectionString, backupPath, databaseName);
            Console.WriteLine($"Created:  {backup}");
            Console.WriteLine();


            //Test copy (does a backup and restore)
            Console.WriteLine($"Copying: {databaseName} to {databaseName}_copy");
            Server.Copy(connectionString, backupPath, databaseName, $"{databaseName}_copy");
            Console.WriteLine($"Created:  {databaseName}_copy");
            Console.WriteLine();


            //Test delete
            Console.WriteLine($"Deleting:  {databaseName}_copy");            
            Server.Delete(connectionString, $"{ databaseName}_copy");
            Console.WriteLine($"Deleted:  {databaseName}_copy");
            Console.WriteLine();
            Console.WriteLine("Press any key to quit.");
            Console.ReadKey();
        }
    }
}
