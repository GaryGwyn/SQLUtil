using System;
using System.Data.SqlClient;

namespace SQLUtil
{
    /// <summary>
    /// SQL Server functions
    /// Backup / Restore / Copy / Delete
    /// </summary>
    public static class Server
    {
        /// <summary>
        /// Backup a SQL database on the server referenced in the connectionString parameter.
        /// Returns the path and generated file name as a string.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="serverBackupPath">Use null if you want to use SQL default backup directory</param>
        /// <param name="databaseName"></param>
        public static string Backup(string connectionString, string serverBackupPath, string databaseName)
        {
            // set backupfilename.  Example:  "C:\ProgramData\YourDB-2016-12-13.bak")
            var backupFileName = String.Format("{0}{1}-{2}.bak",
                serverBackupPath, databaseName,
                DateTime.Now.ToString("yyyy-MM-dd"));

            using (var connection = new SqlConnection(connectionString))
            {
                string query = "BACKUP DATABASE @DatabaseName TO DISK= @BackupFileAndPath";

                //Log
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    command.Parameters.AddWithValue("@DatabaseName", databaseName);
                    command.Parameters.AddWithValue("@BackupFileAndPath", backupFileName);
                    command.ExecuteNonQuery();
                    command.Parameters.Clear();
                    //Log
                }
            }
            return backupFileName;
        }

        /// <summary>
        /// Restore a SQL database.  Use originalDBName and newDBName paramers to restore
        ///     a copy of the "original" as the "new"
        ///     If restoring a database with the same name, the name will be duplicated in the parameters.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="backupFileAndPath"></param>///The location of the backup on disk
        /// <param name="originalDBName"></param> ///The old/original database name
        /// <param name="newDBName"></param>///The new name
        public static void Restore(string connectionString, string backupFileAndPath, string originalDBName, string newDBName)
        {
            string logicalname = "";
            string datapath = "";
            string logpath = "";

            //Log
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                //Get logical name of database -- required for the RESTORE DATABASE command
                string query = "SELECT TOP 1 name FROM master.sys.master_files WHERE DB_NAME(database_id) = @DatabaseName AND [type] = 0";

                SqlDataReader reader;
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DatabaseName", originalDBName);
                    reader = command.ExecuteReader();
                    command.Parameters.Clear();

                    while (reader.Read())
                        logicalname = reader.GetString(0);

                    reader.Close();
                }

                //Get server setting for data / log paths
                query = "SELECT SERVERPROPERTY('instancedefaultdatapath'), SERVERPROPERTY('instancedefaultlogpath')";
                using (var command = new SqlCommand(query, connection))
                {
                    reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        datapath = reader.GetString(0);
                        logpath = reader.GetString(1);
                    }

                    reader.Close();

                }


                //Execute the Restore command
                query = "RESTORE DATABASE @NewDBName " +
                            "FROM DISK= @BackupFile " +
                            "WITH " +
                                "MOVE @LogicalNameDB TO @Datapath, " +
                                "MOVE @LogicalNameLog TO @Logpath";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NewDBName", newDBName);
                    command.Parameters.AddWithValue("@BackupFile", backupFileAndPath);
                    command.Parameters.AddWithValue("@LogicalNameDB", logicalname);
                    command.Parameters.AddWithValue("@Datapath", datapath + newDBName + ".mdf");
                    command.Parameters.AddWithValue("@LogicalNameLog", logicalname + "_log");
                    command.Parameters.AddWithValue("@Logpath", logpath + newDBName + "_log.ldf");
                    command.ExecuteNonQuery();
                    command.Parameters.Clear();
                }
            }
        }

        /// <summary>
        /// Copy a SQL database by performing a backup and restore as
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="serverBackupPath"></param>//This should be stored in the application settings
        /// <param name="originalDBName"></param>
        /// <param name="newDBName"></param> 
        public static void Copy(string connectionString, string serverBackupPath, string originalDBName, string newDBName)
        {
            string backup;
            //Calls local Backup()
            backup = Backup(connectionString, serverBackupPath, originalDBName);
            //Calls local Restore()
            Restore(connectionString, backup, originalDBName, newDBName);

        }

        /// <summary>
        /// Delete an SQL database from the server
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="databaseName">Database name to be deleted.</param>
        public static void Delete(string connectionString, string databaseName)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                //Boot all users that could be in the Database
                using (SqlCommand command = new SqlCommand(String.Format("ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", databaseName), connection))
                {
                    //  command.CommandText = String.Format("ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", databaseName);
                    command.ExecuteNonQuery();

                    //Delete the database
                    command.CommandText = String.Format("DROP DATABASE [{0}]", databaseName);
                    command.ExecuteNonQuery();
                    //Log
                }
            }
        }
        
    }
}
