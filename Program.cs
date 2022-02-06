using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ParserTenders.ParserDir;

namespace ParserTenders
{
    internal static class Program
    {
        private const string Arguments =
            "last44, prev44, curr44, last223, daily223, attach, lastsign223, dailysign223, gpb, lastexp223, dailyexp223, gntweb, obtorgweb, spectorgweb, web, mrsk, rosneft, sakhalin, tekgpm, interrao, rzd, last615, prev615, curr615, web44, currsignproj44, lastsignproj44, prevsignproj44, lastcurr44, currreq44, prevreq44, lastreq44";

        private static string _database;
        private static string _tempPath44;
        private static string _logPath44;
        private static string _tempPath615;
        private static string _logPath615;
        private static string _tempPath223;
        private static string _logPath223;
        private static string _tempAttach;
        private static string _logAttach;
        private static string _tempSign223;
        private static string _logSign223;
        private static string _logGazProm;
        private static string _tempGazProm;
        private static string _logExp223;
        private static string _tempExp223;
        private static string _logGntWeb;
        private static string _tempGntWeb;
        private static string _logObTorgWeb;
        private static string _tempObTorgWeb;
        private static string _logSpecTorgWeb;
        private static string _tempSpecTorgWeb;
        private static string _tempPathWeb;
        private static string _logPathWeb;
        private static string _tempMrsk;
        private static string _logMrsk;
        private static string _tempRosneft;
        private static string _logRosneft;
        private static string _tempSakhalin;
        private static string _logSakhalin;
        private static string _tempTektorgGazprom;
        private static string _logTektorgGazprom;
        private static string _tempTektorgInterRao;
        private static string _logTektorgInterRao;
        private static string _tempTektorgRzd;
        private static string _logTektorgRzd;
        private static string _tempPathWeb44;
        private static string _logPathWeb44;
        private static string _tempPathRequestQ44;
        private static string _logPathRequestQ44;
        private static string _prefix;
        private static string _user;
        private static string _pass;
        private static string _server;
        private static int _port;
        private static int _maxthread;
        private static int _maxtrydown;
        private static string _tempSignProj44;
        private static string _logSignProj44;
        private static List<string> _years = new List<string>();
        public static readonly DateTime LocalDate = DateTime.Now;

        public static string FileLog;

        //public static string FileLogAttach;
        public static string StrArg;

        public static TypeArguments Periodparsing;

        public static string PathProgram;

        public static string TableContractsSign;
        public static string TableSuppliers;
        public static string TableArchiveSign223;
        public static string TableArchiveExp223;
        public static int AddTender44 = 0;
        public static int AddTender504 = 0;
        public static int AddTender615 = 0;
        public static int UpdateTender44 = 0;
        public static int UpdateTender504 = 0;
        public static int UpdateTender615 = 0;
        public static int AddTenderSign = 0;
        public static int AddTenderSignProj44 = 0;
        public static int AddTenderSign615 = 0;
        public static int AddDateChange = 0;
        public static int AddDateChange615 = 0;
        public static int AddProlongation = 0;
        public static int AddOrgChange = 0;
        public static int AddLotCancel = 0;
        public static int AddLotCancel223 = 0;
        public static int AddLotCancel615 = 0;
        public static int AddCancel = 0;
        public static int AddCancel223 = 0;
        public static int AddCancel615 = 0;
        public static int AddCancelFailure = 0;
        public static int AddTender223 = 0;
        public static int UpdateTender223 = 0;
        public static int AddAttach = 0;
        public static int NotAddAttach = 0;
        public static int AddSign223 = 0;
        public static int UpdateSign223 = 0;
        public static int AddGazprom = 0;
        public static int UpGazprom = 0;
        public static int AddClarification = 0;
        public static int AddClarification223 = 0;
        public static int AddGntWeb = 0;
        public static int UpGntWeb = 0;
        public static int AddObTorgWeb = 0;
        public static int UpObTorgWeb = 0;
        public static int AddSpecTorgWeb = 0;
        public static int UpSpecTorgWeb = 0;
        public static int AddMrsk = 0;
        public static int UpMrsk = 0;
        public static int AddRosneft = 0;
        public static int UpRosneft = 0;
        public static int AddSakhalin = 0;
        public static int UpSakhalin = 0;
        public static int AddTektorgGazprom = 0;
        public static int UpTektorgGazprom = 0;
        public static int AddTektorgInterRao = 0;
        public static int UpTektorgInterRao = 0;
        public static int AddTektorgRzd = 0;
        public static int UpTektorgRzd = 0;
        public static int AddRequestQ44 = 0;
        public static int UpdateRequestQ44 = 0;
        public static string Database => _database;
        public static string Prefix => _prefix;
        public static string User => _user;
        public static string Pass => _pass;
        public static string Server => _server;
        public static int Port => _port;
        public static List<string> Years => _years;
        public static int DownCount => _maxtrydown;

        public static int MaxThread => _maxthread;
        //public static string LogAttach => _logAttach;
        //public static string TempAttach => _tempAttach;

        public static string TempPath
        {
            get
            {
                switch (Periodparsing)
                {
                    case TypeArguments.Curr44:
                    case TypeArguments.Prev44:
                    case TypeArguments.Last44:
                    case TypeArguments.LastCurr44:
                        return _tempPath44;
                    case TypeArguments.Curr615:
                    case TypeArguments.Prev615:
                    case TypeArguments.Last615:
                        return _tempPath615;
                    case TypeArguments.Daily223:
                    case TypeArguments.Last223:
                        return _tempPath223;
                    case TypeArguments.Attach:
                        return _tempAttach;
                    case TypeArguments.LastSign223:
                    case TypeArguments.DailySign223:
                        return _tempSign223;
                    case TypeArguments.GpB:
                        return _tempGazProm;
                    case TypeArguments.LastExp223:
                    case TypeArguments.DailyExp223:
                        return _tempExp223;
                    case TypeArguments.GntWeb:
                        return _tempGntWeb;
                    case TypeArguments.ObTorgWeb:
                        return _tempObTorgWeb;
                    case TypeArguments.SpecTorgWeb:
                        return _tempSpecTorgWeb;
                    case TypeArguments.Web:
                        return _tempPathWeb;
                    case TypeArguments.Mrsk:
                        return _tempMrsk;
                    case TypeArguments.Rosneft:
                        return _tempRosneft;
                    case TypeArguments.Sakhalin:
                        return _tempSakhalin;
                    case TypeArguments.TektorgGazprom:
                        return _tempTektorgGazprom;
                    case TypeArguments.TektorgInterRao:
                        return _tempTektorgInterRao;
                    case TypeArguments.TektorgRzd:
                        return _tempTektorgRzd;
                    case TypeArguments.Web44:
                        return _tempPathWeb44;
                    case TypeArguments.CurrSignProj44:
                    case TypeArguments.LastSignProj44:
                    case TypeArguments.PrevSignProj44:
                        return _tempSignProj44;
                    case TypeArguments.CurrReq44:
                    case TypeArguments.PrevReq44:
                    case TypeArguments.LastReq44:
                        return _tempPathRequestQ44;
                    default:
                        return "";
                }
            }
        }

        public static string LogPath
        {
            get
            {
                switch (Periodparsing)
                {
                    case TypeArguments.Curr44:
                    case TypeArguments.Prev44:
                    case TypeArguments.Last44:
                    case TypeArguments.LastCurr44:
                        return _logPath44;
                    case TypeArguments.Curr615:
                    case TypeArguments.Prev615:
                    case TypeArguments.Last615:
                        return _logPath615;
                    case TypeArguments.Daily223:
                    case TypeArguments.Last223:
                        return _logPath223;
                    case TypeArguments.Attach:
                        return _logAttach;
                    case TypeArguments.LastSign223:
                    case TypeArguments.DailySign223:
                        return _logSign223;
                    case TypeArguments.GpB:
                        return _logGazProm;
                    case TypeArguments.LastExp223:
                    case TypeArguments.DailyExp223:
                        return _logExp223;
                    case TypeArguments.GntWeb:
                        return _logGntWeb;
                    case TypeArguments.ObTorgWeb:
                        return _logObTorgWeb;
                    case TypeArguments.SpecTorgWeb:
                        return _logSpecTorgWeb;
                    case TypeArguments.Web:
                        return _logPathWeb;
                    case TypeArguments.Mrsk:
                        return _logMrsk;
                    case TypeArguments.Rosneft:
                        return _logRosneft;
                    case TypeArguments.Sakhalin:
                        return _logSakhalin;
                    case TypeArguments.TektorgGazprom:
                        return _logTektorgGazprom;
                    case TypeArguments.TektorgInterRao:
                        return _logTektorgInterRao;
                    case TypeArguments.TektorgRzd:
                        return _logTektorgRzd;
                    case TypeArguments.Web44:
                        return _logPathWeb44;
                    case TypeArguments.CurrSignProj44:
                    case TypeArguments.LastSignProj44:
                    case TypeArguments.PrevSignProj44:
                        return _logSignProj44;
                    case TypeArguments.CurrReq44:
                    case TypeArguments.PrevReq44:
                    case TypeArguments.LastReq44:
                        return _logPathRequestQ44;
                    default:
                        return "";
                }
            }
        }

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(
                    $"Недостаточно аргументов для запуска, используйте {Arguments} в качестве аргумента");
                return;
            }

            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName()
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
                case "lastcurr44":
                    Periodparsing = TypeArguments.LastCurr44;
                    Init(Periodparsing);
                    ParserTender44(Periodparsing);
                    break;
                case "last615":
                    Periodparsing = TypeArguments.Last615;
                    Init(Periodparsing);
                    ParserTender615(Periodparsing);
                    break;
                case "prev615":
                    Periodparsing = TypeArguments.Prev615;
                    Init(Periodparsing);
                    ParserTender615(Periodparsing);
                    break;
                case "curr615":
                    Periodparsing = TypeArguments.Curr615;
                    Init(Periodparsing);
                    ParserTender615(Periodparsing);
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
                case "lastsign223":
                    Periodparsing = TypeArguments.LastSign223;
                    Init(Periodparsing);
                    ParserSign223(Periodparsing);
                    break;
                case "dailysign223":
                    Periodparsing = TypeArguments.DailySign223;
                    Init(Periodparsing);
                    ParserSign223(Periodparsing);
                    break;
                case "gpb":
                    Periodparsing = TypeArguments.GpB;
                    Init(Periodparsing);
                    ParserGpb(Periodparsing);
                    break;
                case "lastexp223":
                    Periodparsing = TypeArguments.LastExp223;
                    Init(Periodparsing);
                    ParserExp(Periodparsing);
                    break;
                case "dailyexp223":
                    Periodparsing = TypeArguments.DailyExp223;
                    Init(Periodparsing);
                    ParserExp(Periodparsing);
                    break;
                case "gntweb":
                    Periodparsing = TypeArguments.GntWeb;
                    Init(Periodparsing);
                    ParserGntWeb(Periodparsing);
                    break;
                case "obtorgweb":
                    Periodparsing = TypeArguments.ObTorgWeb;
                    Init(Periodparsing);
                    ParserObTorgWeb(Periodparsing);
                    break;
                case "spectorgweb":
                    Periodparsing = TypeArguments.SpecTorgWeb;
                    Init(Periodparsing);
                    ParserSpecTorgTorgWeb(Periodparsing);
                    break;
                case "web":
                    Periodparsing = TypeArguments.Web;
                    Init(Periodparsing);
                    ParserWeb(Periodparsing);
                    break;
                case "web44":
                    Periodparsing = TypeArguments.Web44;
                    Init(Periodparsing);
                    ParserWeb44(Periodparsing);
                    break;
                case "mrsk":
                    Periodparsing = TypeArguments.Mrsk;
                    Init(Periodparsing);
                    ParserMrsk(Periodparsing);
                    break;
                case "rosneft":
                    Periodparsing = TypeArguments.Rosneft;
                    Init(Periodparsing);
                    ParserRosneft(Periodparsing);
                    break;
                case "sakhalin":
                    Periodparsing = TypeArguments.Sakhalin;
                    Init(Periodparsing);
                    ParserSakhalin(Periodparsing);
                    break;
                case "tekgpm":
                    Periodparsing = TypeArguments.TektorgGazprom;
                    Init(Periodparsing);
                    ParserTektorgGazprom(Periodparsing);
                    break;
                case "interrao":
                    Periodparsing = TypeArguments.TektorgInterRao;
                    Init(Periodparsing);
                    ParserTektorgInterRao(Periodparsing);
                    break;
                case "rzd":
                    Periodparsing = TypeArguments.TektorgRzd;
                    Init(Periodparsing);
                    ParserTektorgRzd(Periodparsing);
                    break;
                case "currsignproj44":
                    Periodparsing = TypeArguments.CurrSignProj44;
                    Init(Periodparsing);
                    ParserSignProj44(Periodparsing);
                    break;
                case "lastsignproj44":
                    Periodparsing = TypeArguments.LastSignProj44;
                    Init(Periodparsing);
                    ParserSignProj44(Periodparsing);
                    break;
                case "prevsignproj44":
                    Periodparsing = TypeArguments.PrevSignProj44;
                    Init(Periodparsing);
                    ParserSignProj44(Periodparsing);
                    break;
                case "lastreq44":
                    Periodparsing = TypeArguments.LastReq44;
                    Init(Periodparsing);
                    ParserRequestQ44(Periodparsing);
                    break;
                case "prevreq44":
                    Periodparsing = TypeArguments.PrevReq44;
                    Init(Periodparsing);
                    ParserRequestQ44(Periodparsing);
                    break;
                case "currreq44":
                    Periodparsing = TypeArguments.CurrReq44;
                    Init(Periodparsing);
                    ParserRequestQ44(Periodparsing);
                    break;
                default:
                    Console.WriteLine(
                        $"Неправильно указан аргумент, используйте {Arguments}");
                    break;
            }
        }

        private static void Init(TypeArguments arg)
        {
            var set = new GetSettings();
            _database = set.Database;
            _logPath44 = set.LogPathTenders44;
            _logPath615 = set.LogPathTenders615;
            _logPath223 = set.LogPathTenders223;
            _prefix = set.Prefix;
            _user = set.UserDb;
            _pass = set.PassDb;
            _tempPath44 = set.TempPathTenders44;
            _tempPath615 = set.TempPathTenders615;
            _tempPath223 = set.TempPathTenders223;
            _server = set.Server;
            _port = set.Port;
            _logAttach = set.LogPathAttach;
            _tempAttach = set.TempPathAttach;
            _maxthread = set.MaxThread;
            _maxtrydown = set.MaxTryDown;
            var tmp = set.Years;
            _tempSign223 = set.TempPathSign223;
            _logSign223 = set.LogPathSign223;
            _logGazProm = set.LogPathGazProm;
            _tempGazProm = set.TempPathGazProm;
            _logExp223 = set.LogPathExp223;
            _tempExp223 = set.TempPathExp223;
            _tempGntWeb = set.TempGntWeb;
            _logGntWeb = set.LogGntWeb;
            _tempObTorgWeb = set.TempObTorgWeb;
            _logObTorgWeb = set.LogObTorgWeb;
            _tempSpecTorgWeb = set.TempSpecTorgWeb;
            _logSpecTorgWeb = set.LogSpecTorgWeb;
            _tempPathWeb = set.TempPathTendersWeb;
            _logPathWeb = set.LogPathTendersWeb;
            _tempPathWeb44 = set.TempPathTendersWeb44;
            _logPathWeb44 = set.LogPathTendersWeb44;
            _tempMrsk = set.TempMrsk;
            _logMrsk = set.LogMrsk;
            _tempRosneft = set.TempRosneft;
            _logRosneft = set.LogRosneft;
            _tempSakhalin = set.TempSakhalin;
            _logSakhalin = set.LogSakhalin;
            _tempTektorgGazprom = set.TempPathTektorgGazprom;
            _logTektorgGazprom = set.LogPathTektorgGazprom;
            _tempTektorgInterRao = set.TempPathTektorgInterRao;
            _logTektorgInterRao = set.LogPathTektorgInterRao;
            _tempTektorgRzd = set.TempPathTektorgRzd;
            _logTektorgRzd = set.LogPathTektorgRzd;
            _tempSignProj44 = set.TempPathSignProj44;
            _logSignProj44 = set.LogPathSignProj44;
            _tempPathRequestQ44 = set.TempPathReq44;
            _logPathRequestQ44 = set.LogPathReq44;
            TableArchiveSign223 = $"{Prefix}arhiv_tender223_sign";
            TableArchiveExp223 = $"{Prefix}arhiv_explanation223";
            TableContractsSign = $"{Prefix}contract_sign";
            TableSuppliers = $"{Prefix}supplier";
            var tempYears = tmp.Split(new char[] {','});

            foreach (var s in tempYears.Select(v => $"_{v.Trim()}"))
            {
                _years.Add(s);
            }

            if (string.IsNullOrEmpty(TempPath) || string.IsNullOrEmpty(LogPath))
            {
                Console.WriteLine("Не получится создать папки для парсинга");
                Environment.Exit(0);
            }

            if (Directory.Exists(TempPath))
            {
                var dirInfo = new DirectoryInfo(TempPath);
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

            switch (arg)
            {
                case TypeArguments.Curr44:
                case TypeArguments.Last44:
                case TypeArguments.Prev44:
                case TypeArguments.LastCurr44:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}Tenders44_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.Curr615:
                case TypeArguments.Last615:
                case TypeArguments.Prev615:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}Tenders615_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.Daily223:
                case TypeArguments.Last223:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}Tenders223_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.Attach:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}Attach_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.DailySign223:
                case TypeArguments.LastSign223:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}Sign223_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.GpB:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}Gpb_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.DailyExp223:
                case TypeArguments.LastExp223:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}Explanation223_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.GntWeb:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}{arg}_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.ObTorgWeb:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}{arg}_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.SpecTorgWeb:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}{arg}_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.Web:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}{arg}_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.Web44:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}{arg}_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.Mrsk:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}{arg}_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.Rosneft:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}{arg}_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.Sakhalin:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}{arg}_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.TektorgGazprom:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}{arg}_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.TektorgInterRao:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}{arg}_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.TektorgRzd:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}{arg}_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.CurrSignProj44:
                case TypeArguments.PrevSignProj44:
                case TypeArguments.LastSignProj44:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}SignProject44_{LocalDate:dd_MM_yyyy}.log";
                    break;
                case TypeArguments.CurrReq44:
                case TypeArguments.LastReq44:
                case TypeArguments.PrevReq44:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}RequestQ44_{LocalDate:dd_MM_yyyy}.log";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(arg), arg, null);
            }
        }

        private static void ParserTender44(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Tenders44");
            try
            {
                var t44 = new ParserTend44(Periodparsing);
                t44.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            /*ParserTend44 t44 = new ParserTend44(Periodparsing);
            FileInfo f =
                new FileInfo(
                    "/home/alex/RiderProjects/ParserTenders/ParserTenders/bin/Release/222.xml");
            t44.ParsingXml(f, "br", 32, TypeFile44.TypeTen504);*/

            Log.Logger("Добавили tender44", AddTender44);
            Log.Logger("Обновили tender44", UpdateTender44);
            Log.Logger("Добавили tender504", AddTender504);
            Log.Logger("Обновили tender504", UpdateTender504);
            Log.Logger("Добавили tenderSign", AddTenderSign);
            Log.Logger("Добавили DateChange", AddDateChange);
            Log.Logger("Добавили Prolongation", AddProlongation);
            Log.Logger("Добавили OrgChange", AddOrgChange);
            Log.Logger("Добавили LotCancel", AddLotCancel);
            Log.Logger("Добавили Cancel", AddCancel);
            Log.Logger("Добавили CancelFailure", AddCancelFailure);
            Log.Logger("Добавили Clarification", AddClarification);
            Log.Logger("Время окончания парсинга Tenders44");
        }

        private static void ParserTender615(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Tenders615");
            try
            {
                var t615 = new ParserTend615(Periodparsing);
                t615.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            //t615.ParsingContractS();
            /*var t615 = new ParserTend615(Periodparsing);
            FileInfo f = new FileInfo("/home/alex/RiderProjects/ParserTenders/ParserTenders/bin/pprf615NotificationEF_204250000011800008_16370095.xml");
            t615.ParsingXml(f, "br", 32, TypeFile615.TypeTen615);*/
            Log.Logger("Добавили tender615", AddTender615);
            Log.Logger("Обновили tender615", UpdateTender615);
            Log.Logger("Добавили LotCancel615", AddLotCancel615);
            Log.Logger("Добавили Cancel615", AddCancel615);
            Log.Logger("Добавили DateChange615", AddDateChange615);
            Log.Logger("Время окончания парсинга Tenders615");
        }

        private static void ParserTender223(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Tenders223");
            try
            {
                var t223 = new ParserTend223(Periodparsing);
                t223.Parsing();
                t223.ParserLostTens();
                t223.ParsingAst();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            /*ParserTend223 t223 = new ParserTend223(Periodparsing);
            FileInfo f = new FileInfo("/home/alex/RiderProjects/ParserTenders/ParserTenders/bin/Release/222.xml");
            t223.ParsingXml(f, "br", 32, TypeFile223.PurchaseNotice);*/
            Log.Logger("Добавили tender223", AddTender223);
            Log.Logger("Обновили tender223", UpdateTender223);
            Log.Logger("Добавили LotCancel223", AddLotCancel223);
            Log.Logger("Добавили Cancel223", AddCancel223);
            Log.Logger("Время окончания парсинга Tenders223");
        }

        private static void ParserAtt(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Attach");
            try
            {
                var att = new ParserAttach(TypeArguments.Attach);
                att.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            /*FileInfo fileInf = new FileInfo("");
            if (fileInf.Exists)
            {
                //fileInf.Delete();
                Console.WriteLine($"Скачали файл" );
            }
            else
            {
                Console.WriteLine($"Не удалось скачать файл ");
            }*/
            Log.Logger("Добавили attach", AddAttach);
            Log.Logger("Не добавили attach", NotAddAttach);
            Log.Logger("Время окончания парсинга Attach");
        }

        private static void ParserSign223(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Sign223");
            try
            {
                var s = new ParserSgn223(Periodparsing);
                s.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            /*ParserSgn223 t223 = new ParserSgn223(Periodparsing);
            FileInfo f = new FileInfo("/home/alex/Рабочий стол/parser/contract_Belgorodskaya_obl_20160202_000000_20160202_235959_daily_027.xml");
            t223.ParsingXml(f, "br", 32);*/
            Log.Logger("Добавили Sign223", AddSign223);
            Log.Logger("Обновили Sign223", UpdateSign223);
            Log.Logger("Время окончания парсинга Sign223");
        }

        private static void ParserGpb(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Gpb");
            try
            {
                var p = new ParserGpb(Periodparsing);
                p.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            Log.Logger("Добавили Gpb", AddGazprom);
            Log.Logger("Обновили Gpb", UpGazprom);
            AddGazprom = 0;
            UpGazprom = 0;
            try
            {
                var d = new ParserGpbGaz(Periodparsing);
                d.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            /*ParserGpb p = new ParserGpb(Periodparsing);
            var l = new Dictionary<int, int> {[1] = 6};
            p.ParsingProc(new ProcedureGpB
            {
                RegistryNumber = "ГП609177",
                Lots = l,
                ScoringDate = DateTime.MinValue,
                BiddingDate = DateTime.MinValue,
                EndDate = DateTime.MinValue
            });*/
            Log.Logger("Добавили GpbGaz", AddGazprom);
            Log.Logger("Обновили GpbGaz", UpGazprom);
            Log.Logger("Время окончания парсинга Gpb");
        }

        private static void ParserExp(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Explanation");
            try
            {
                var p = new ParserExp(arg);
                p.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            Log.Logger("Добавили Explanation", AddClarification223);
            Log.Logger("Время окончания парсинга Explanation");
        }

        private static void ParserGntWeb(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга GntWeb");
            try
            {
                var p = new ParserGntWeb(arg);
                p.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            /*GntWebTender t = new GntWebTender
            {
                UrlTender = "https://www.gazneftetorg.ru/trades/energo/ProposalRequest/?action=view&id=30493#lot_1",
                UrlOrg = "https://www.gazneftetorg.ru/firms/view_firm.html?id=7kGR7S9qSRoUHpJCr6Z5GQ%3D%3D&fi=87721",
                Entity =
                    "Запрос предложений № 139447",
                MaxPrice = 654372.88m,
                DateEnd = DateTime.Parse("10.01.2018 15:00"),
                DateOpen = DateTime.Parse("19.12.2017 10:00"),
                DatePub = DateTime.Parse("11.12.2017 18:30"),
                DateRes = DateTime.Parse("27.12.2017 12:30"),
                TypeGnT = new TypeGnt()
                {
                    Type = GntType.ProposalRequest,
                    UrlType = "/trades/energo/ProposalRequest/?action=list_published&from=",
                    UrlTypeList =
                        "https://www.gazneftetorg.ru/trades/energo/ProposalRequest/?action=list_published&from=0"
                }
            };
            t.Parse();*/
            Log.Logger("Добавили GntWeb", AddGntWeb);
            Log.Logger("Обновили GntWeb", UpGntWeb);
            Log.Logger("Время окончания парсинга GntWeb");
        }

        private static void ParserObTorgWeb(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга ObTorgWeb");
            try
            {
                var p = new ParserObTorgWeb(arg);
                p.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            /*ObTorgWebTender t = new ObTorgWebTender
            {
                UrlTender = "https://www.oborontorg.ru/market/view.html?action=view_public_offer&type=1560&id=127906",
                UrlOrg = "https://www.oborontorg.ru/firms/view_firm.html?id=lPuLZUP1Ije8U3PQDTcVnFeL_Sg0Y69rp_XwAiD4K5bCsM3hEpoucjTOy_whnFrvzk9ZAB0DPi6JltQm6cf31g",
                Entity =
                    "Запрос предложений № 139447",
                MaxPrice = 654372.88m,
                DateEnd = DateTime.Parse("10.01.2018 15:00"),
                DateOpen = DateTime.Parse("19.12.2017 10:00"),
                DatePub = DateTime.Parse("11.12.2017 18:30"),
                DateRes = DateTime.Parse("27.12.2017 12:30"),
                TypeObTorgT = new TypeObTorg()
                {
                    Type = ObTorgType.Auction,
                    UrlType = "/market/?action=list_public_auctions&type=1560&status_group=sg_published&from=",
                    UrlTypeList =
                        "https://www.oborontorg.ru/market/?action=list_public_auctions&type=1560&status_group=sg_published&from=0"
                }
            };
            t.ParseAuction();*/
            Log.Logger("Добавили ObTorgWeb", AddObTorgWeb);
            Log.Logger("Обновили ObTorgWeb", UpObTorgWeb);
            Log.Logger("Время окончания парсинга ObTorgWeb");
        }

        private static void ParserSpecTorgTorgWeb(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга SpecTorgWeb");
            try
            {
                var p = new ParserSpecTorgWeb(arg);
                p.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            Log.Logger("Добавили SpecTorgWeb", AddSpecTorgWeb);
            Log.Logger("Обновили SpecTorgWeb", UpSpecTorgWeb);
            Log.Logger("Время окончания парсинга SpecTorgWeb");
        }

        private static void ParserWeb(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Web");
            try
            {
                var p = new ParserTendersWeb(arg);
                p.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            Log.Logger("Добавили tender44", AddTender44);
            Log.Logger("Обновили tender44", UpdateTender44);
            Log.Logger("Добавили tender223", AddTender223);
            Log.Logger("Обновили tender223", UpdateTender223);
            Log.Logger($"Количество скачиваний {DownloadString.MaxDownload}");
            Log.Logger("Время окончания парсинга Web");
        }

        private static void ParserWeb44(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Web44");
            try
            {
                var p = new ParserTendersWeb44(arg);
                p.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            Log.Logger("Добавили tender44", AddTender44);
            Log.Logger("Обновили tender44", UpdateTender44);
            Log.Logger("Добавили tender504", AddTender504);
            Log.Logger("Обновили tender504", UpdateTender504);
            Log.Logger($"Количество скачиваний {DownloadString.MaxDownload}");
            Log.Logger("Время окончания парсинга Web44");
        }

        private static void ParserMrsk(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Mrsk");
            try
            {
                var p = new ParserMrsk(arg);
                p.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            Log.Logger("Добавили Mrsk", AddMrsk);
            Log.Logger("Обновили Mrsk", UpMrsk);
            Log.Logger("Время окончания парсинга");
        }

        private static void ParserRosneft(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Rosneft");
            try
            {
                var p = new ParserRosneft(arg);
                p.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            Log.Logger("Добавили Rosneft", AddRosneft);
            Log.Logger("Обновили Rosneft", UpRosneft);
            Log.Logger("Время окончания парсинга");
        }

        private static void ParserSakhalin(TypeArguments arg)
        {
            Log.Logger($"Время начала парсинга {arg}");
            try
            {
                var p = new ParserSakhalin(arg);
                p.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            Log.Logger($"Добавили {arg}", AddSakhalin);
            Log.Logger($"Обновили {arg}", UpSakhalin);
            Log.Logger("Время окончания парсинга");
        }

        private static void ParserTektorgGazprom(TypeArguments arg)
        {
            Log.Logger($"Время начала парсинга {arg}");
            try
            {
                var p = new ParserTektorgGazprom(arg);
                p.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            Log.Logger($"Добавили {arg}", AddTektorgGazprom);
            Log.Logger($"Обновили {arg}", UpTektorgGazprom);
            Log.Logger("Время окончания парсинга");
        }

        private static void ParserTektorgInterRao(TypeArguments arg)
        {
            Log.Logger($"Время начала парсинга {arg}");
            try
            {
                var p = new ParserTektorgInterRao(arg);
                p.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            Log.Logger($"Добавили {arg}", AddTektorgInterRao);
            Log.Logger($"Обновили {arg}", UpTektorgInterRao);
            Log.Logger("Время окончания парсинга");
        }

        private static void ParserTektorgRzd(TypeArguments arg)
        {
            Log.Logger($"Время начала парсинга {arg}");
            try
            {
                var p = new ParserTektorgRzd(arg);
                p.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            Log.Logger($"Добавили {arg}", AddTektorgRzd);
            Log.Logger($"Обновили {arg}", UpTektorgRzd);
            Log.Logger("Время окончания парсинга");
        }

        private static void ParserSignProj44(TypeArguments arg)
        {
            Log.Logger($"Время начала парсинга SignProj44");
            try
            {
                var p = new ParserSignProj44(arg);
                p.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            Log.Logger($"Добавили SignProj44", AddTenderSignProj44);
            Log.Logger("Время окончания парсинга");
        }
        
        private static void ParserRequestQ44(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга RequestQ44");
            try
            {
                var t44 = new ParserRequestQ44(Periodparsing);
                t44.Parsing();
            }
            catch (Exception e)
            {
                Log.Logger(e);
            }

            /*ParserTend44 t44 = new ParserTend44(Periodparsing);
            FileInfo f =
                new FileInfo(
                    "/home/alex/RiderProjects/ParserTenders/ParserTenders/bin/Release/222.xml");
            t44.ParsingXml(f, "br", 32, TypeFile44.TypeTen504);*/

            Log.Logger("Добавили tender44", AddRequestQ44);
            Log.Logger("Обновили tender44", UpdateRequestQ44);
            Log.Logger("Время окончания парсинга RequestQ44");
        }
    }
}