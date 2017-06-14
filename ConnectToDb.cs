using System;
using MySql.Data.MySqlClient;

namespace ParserTenders
{
    public class ConnectToDb
    {
        public static MySqlConnection GetDBConnection()
        {
            // Connection String.
            String connString = "Server=" + Program.Server + ";port=" + Program.Port + ";Database=" + Program.Database
                                + ";User Id=" + Program.User + ";password=" + Program.Pass + ";CharSet=utf8;Convert Zero Datetime=True";

            MySqlConnection conn = new MySqlConnection(connString);

            return conn;
        }
    }
}