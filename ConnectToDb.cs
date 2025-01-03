#region

using MySql.Data.MySqlClient;

#endregion

namespace ParserTenders
{
    public class ConnectToDb
    {
        public static string ConnectString { get; }

        static ConnectToDb()
        {
            ConnectString =
                $"Server={Program.Server};port={Program.Port};Database={Program.Database};User Id={Program.User};password={Program.Pass};CharSet=utf8;Convert Zero Datetime=True;default command timeout=3600;Connection Timeout=3600";
        }

        public static MySqlConnection GetDbConnection()
        {
            var conn = new MySqlConnection(ConnectString);

            return conn;
        }
    }
}