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
        private static string _tempPath223;
        private static string _logPath223;
        private static string _tempAttach;
        private static string _logAttach;
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

        //public static string FileLogAttach;
        public static string StrArg;

        public static TypeArguments Periodparsing;
        public static string PathProgram;
        //public static string LogAttach => _logAttach;
        //public static string TempAttach => _tempAttach;

        public static string TempPath
        {
            get
            {
                if (Periodparsing == TypeArguments.Curr44 || Periodparsing == TypeArguments.Prev44 ||
                    Periodparsing == TypeArguments.Last44)
                    return _tempPath44;
                else if (Periodparsing == TypeArguments.Daily223 || Periodparsing == TypeArguments.Last223)
                    return _tempPath223;
                else if (Periodparsing == TypeArguments.Attach)
                    return _tempAttach;

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
                else if (Periodparsing == TypeArguments.Daily223 || Periodparsing == TypeArguments.Last223)
                    return _logPath223;
                else if (Periodparsing == TypeArguments.Attach)
                    return _logAttach;

                return "";
            }
        }

        public static int AddTender44 = 0;
        public static int AddTenderSign = 0;
        public static int AddDateChange = 0;
        public static int AddProlongation = 0;
        public static int AddOrgChange = 0;
        public static int AddLotCancel = 0;
        public static int AddCancel = 0;
        public static int AddCancelFailure = 0;
        public static int AddTender223 = 0;

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(
                    "Недостаточно аргументов для запуска, используйте last44, prev44, curr44, last223, daily223, attach в качестве аргумента");
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
                case "last223":
                    Periodparsing = TypeArguments.Last223;
                    Init(Periodparsing);
                    ParserTender223(Periodparsing);
                    break;
                case "daily223":
                    Periodparsing = TypeArguments.Daily223;
                    Init(Periodparsing);
                    ParserTender223(Periodparsing);
                    break;
                case "attach":
                    Periodparsing = TypeArguments.Attach;
                    Init(Periodparsing);
                    ParserAtt(Periodparsing);
                    break;
                default:
                    Console.WriteLine(
                        "Неправильно указан аргумент, используйте last44, prev44, curr44, last223, daily223, attach ");
                    break;
            }
        }

        private static void Init(TypeArguments arg)
        {
            GetSettings set = new GetSettings();
            _database = set.Database;
            _logPath44 = set.LogPathTenders44;
            _logPath223 = set.LogPathTenders223;
            _prefix = set.Prefix;
            _user = set.UserDB;
            _pass = set.PassDB;
            _tempPath44 = set.TempPathTenders44;
            _tempPath223 = set.TempPathTenders223;
            _server = set.Server;
            _port = set.Port;
            _logAttach = set.LogPathAttach;
            _tempAttach = set.TempPathAttach;
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
            if (arg == TypeArguments.Curr44 || arg == TypeArguments.Last44 || arg == TypeArguments.Prev44)
                FileLog = $"{LogPath}{Path.DirectorySeparatorChar}Tenders44_{LocalDate:dd_MM_yyyy}.log";
            else if (arg == TypeArguments.Daily223 || arg == TypeArguments.Last223)
                FileLog = $"{LogPath}{Path.DirectorySeparatorChar}Tenders223_{LocalDate:dd_MM_yyyy}.log";
            else if (arg == TypeArguments.Attach)
                FileLog = $"{LogPath}{Path.DirectorySeparatorChar}Attach_{LocalDate:dd_MM_yyyy}.log";
        }

        private static void ParserTender44(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Tenders44");
            ParserTend44 t44 = new ParserTend44(Periodparsing);
            t44.Parsing();
            /*Log.Logger("Время окончания парсинга Tenders44");
            ParserTend44 t44 = new ParserTend44(Periodparsing);
            FileInfo f = new FileInfo("/home/alex/Рабочий стол/parser/fcsNotificationCancelFailure_0328300028016000028_7111.xml");
            t44.ParsingXML(f, "br", 32, TypeFile44.TypeCancelFailure);*/
            Log.Logger("Добавили tender44", AddTender44);
            Log.Logger("Добавили tenderSign", AddTenderSign);
            Log.Logger("Добавили DateChange", AddDateChange);
            Log.Logger("Добавили Prolongation", AddProlongation);
            Log.Logger("Добавили OrgChange", AddOrgChange);
            Log.Logger("Добавили LotCancel", AddLotCancel);
            Log.Logger("Добавили Cancel", AddCancel);
            Log.Logger("Добавили CancelFailure", AddCancelFailure);
            Log.Logger("Время окончания парсинга Tenders44");
        }

        private static void ParserTender223(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Tenders223");
            ParserTend223 t223 = new ParserTend223(Periodparsing);
            t223.Parsing();
            /*ParserTend223 t223 = new ParserTend223(Periodparsing);
            FileInfo f = new FileInfo("/home/alex/Рабочий стол/parser/purchaseNotice_Belgorodskaya_obl_20170201_000000_20170228_235959_015.xml");
            t223.ParsingXML(f, "br", 32, TypeFile223.purchaseNotice);*/
            Log.Logger("Время окончания парсинга Tenders223");
            Log.Logger("Добавили tender223", AddTender223);
        }

        private static void ParserAtt(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Attach");
            ParserAttach att = new ParserAttach(TypeArguments.Attach);
            att.Parsing();
            Log.Logger("Время окончания парсинга Attach");
        }
    }
}