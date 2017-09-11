using System;
using MySql.Data.MySqlClient;

namespace ParserTenders
{
    public class ConnectToDb
    {
        public static string ConnectString { get; private set; }

        static ConnectToDb()
        {
            ConnectString =
                $"Server={Program.Server};port={Program.Port};Database={Program.Database};User Id={Program.User};password={Program.Pass};CharSet=utf8;Convert Zero Datetime=True;default command timeout=3600;Connection Timeout=3600";
        }
        
        public static MySqlConnection GetDbConnection()
        {
            MySqlConnection conn = new MySqlConnection(ConnectString);

            return conn;
        }
    }
}