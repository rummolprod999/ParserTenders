using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ParserTenders
{
    internal class Program
    {
        private static string _database;
        private static string _tempPath44;
        private static string _logPath44;
        private static string _prefix;
        private static string _user;
        private static string _pass;
        private static string _server;
        private static int _port;
        private static List<string> _years = new List<string>();
        public static string Database => _database;
        public static string Prefix => _prefix;
        public static string User => _user;
        public static string Pass => _pass;
        public static string Server => _server;
        public static int Port => _port;
        public static List<string> Years => _years;
        public static readonly DateTime LocalDate = DateTime.Now;
        public static string FileLog;
        public static string StrArg;
        public static TypeArguments Periodparsing;
        public static string PathProgram;
        public static string TempPath
        {
            get
            {
                if (Periodparsing == TypeArguments.Curr44 || Periodparsing == TypeArguments.Prev44 ||
                    Periodparsing == TypeArguments.Last44)
                    return _tempPath44;

                return "";
            }
        }

        public static string LogPath
        {
            get
            {
                if (Periodparsing == TypeArguments.Curr44 || Periodparsing == TypeArguments.Prev44 ||
                    Periodparsing == TypeArguments.Last44)
                    return _logPath44;

                return "";
            }
        }
        public static int Addtender44 = 0;

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(
                    "Недостаточно аргументов для запуска, используйте last44 или prev44 или curr44 в качестве аргумента");
                return;
            }

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName()
                .CodeBase);
            if (path != null) PathProgram = path.Substring(5);
            StrArg = args[0];
            switch (args[0])
            {
                case "last44":
                    Periodparsing = TypeArguments.Last44;
                    Init(Periodparsing);
                    ParserTender44(Periodparsing);
                    break;
                case "prev44":
                    Periodparsing = TypeArguments.Prev44;
                    Init(Periodparsing);
                    ParserTender44(Periodparsing);
                    break;
                case "curr44":
                    Periodparsing = TypeArguments.Curr44;
                    Init(Periodparsing);
                    ParserTender44(Periodparsing);
                    break;
                default:
                    Console.WriteLine("Неправильно указан аргумент, используйте last44, curr44, prev44");
                    break;
            }
        }

        private static void Init(TypeArguments arg)
        {
            GetSettings set = new GetSettings();
            _database = set.Database;
            _logPath44 = set.LogPathTenders44;
            _prefix = set.Prefix;
            _user = set.UserDB;
            _pass = set.PassDB;
            _tempPath44 = set.TempPathTenders44;
            _server = set.Server;
            _port = set.Port;
            string tmp = set.Years;
            string[] temp_years = tmp.Split(new char[] {','});

            foreach (var s in temp_years.Select(v => $"_{v.Trim()}"))
            {
                _years.Add(s);
            }
            if (String.IsNullOrEmpty(TempPath) || String.IsNullOrEmpty(LogPath))
            {
                Console.WriteLine("Не получится создать папки для парсинга");
                Environment.Exit(0);
            }

            if (Directory.Exists(TempPath))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(TempPath);
                dirInfo.Delete(true);
                Directory.CreateDirectory(TempPath);
            }
            else
            {
                Directory.CreateDirectory(TempPath);
            }
            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }
            FileLog = $"{LogPath}{Path.DirectorySeparatorChar}Tenders44_{LocalDate:dd_MM_yyyy}.log";
        }

        private static void ParserTender44(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Tenders44");
            /*ParserTend44 t44 = new ParserTend44(Periodparsing);
            t44.Parsing();*/
            Log.Logger("Время окончания парсинга Tenders44");
            ParserTend44 t44 = new ParserTend44(Periodparsing);
            FileInfo f = new FileInfo("/home/alex/Рабочий стол/parser/fcsNotificationEA44_0126300029115000948_6597318.xml");
            t44.ParsingXML(f, "br", 32, TypeFile44.TypeTen44);
        }
    }
}