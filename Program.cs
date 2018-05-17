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
            "last44, prev44, curr44, last223, daily223, attach, lastsign223, dailysign223, gpb, lastexp223, dailyexp223, gntweb, obtorgweb, spectorgweb, web, mrsk, rosneft, sakhalin, tekgpm, interrao, rzd";

        private static string _database;
        private static string _tempPath44;
        private static string _logPath44;
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
        private static string _prefix;
        private static string _user;
        private static string _pass;
        private static string _server;
        private static int _port;
        private static int _maxthread;
        private static int _maxtrydown;
        private static List<string> _years = new List<string>();
        public static string Database => _database;
        public static string Prefix => _prefix;
        public static string User => _user;
        public static string Pass => _pass;
        public static string Server => _server;
        public static int Port => _port;
        public static List<string> Years => _years;
        public static readonly DateTime LocalDate = DateTime.Now;
        public static int DownCount => _maxtrydown;
        public static int MaxThread => _maxthread;

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
                switch (Periodparsing)
                {
                    case TypeArguments.Curr44:
                    case TypeArguments.Prev44:
                    case TypeArguments.Last44:
                        return _tempPath44;
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
                        return _logPath44;
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
                    default:
                        return "";
                }
            }
        }

        public static string TableContractsSign;
        public static string TableSuppliers;
        public static string TableArchiveSign223;
        public static string TableArchiveExp223;
        public static int AddTender44 = 0;
        public static int AddTenderSign = 0;
        public static int AddDateChange = 0;
        public static int AddProlongation = 0;
        public static int AddOrgChange = 0;
        public static int AddLotCancel = 0;
        public static int AddCancel = 0;
        public static int AddCancelFailure = 0;
        public static int AddTender223 = 0;
        public static int AddAttach = 0;
        public static int NotAddAttach = 0;
        public static int AddSign223 = 0;
        public static int UpdateSign223 = 0;
        public static int AddGazprom = 0;
        public static int AddClarification = 0;
        public static int AddClarification223 = 0;
        public static int AddGntWeb = 0;
        public static int AddObTorgWeb = 0;
        public static int AddSpecTorgWeb = 0;
        public static int AddMrsk = 0;
        public static int AddRosneft = 0;
        public static int AddSakhalin = 0;
        public static int AddTektorgGazprom = 0;
        public static int AddTektorgInterRao = 0;
        public static int AddTektorgRzd = 0;

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(
                    $"Недостаточно аргументов для запуска, используйте {Arguments} в качестве аргумента");
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
                default:
                    Console.WriteLine(
                        $"Неправильно указан аргумент, используйте {Arguments}");
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
            _user = set.UserDb;
            _pass = set.PassDb;
            _tempPath44 = set.TempPathTenders44;
            _tempPath223 = set.TempPathTenders223;
            _server = set.Server;
            _port = set.Port;
            _logAttach = set.LogPathAttach;
            _tempAttach = set.TempPathAttach;
            _maxthread = set.MaxThread;
            _maxtrydown = set.MaxTryDown;
            string tmp = set.Years;
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
            TableArchiveSign223 = $"{Prefix}arhiv_tender223_sign";
            TableArchiveExp223 = $"{Prefix}arhiv_explanation223";
            TableContractsSign = $"{Prefix}contract_sign";
            TableSuppliers = $"{Prefix}supplier";
            string[] tempYears = tmp.Split(new char[] {','});

            foreach (var s in tempYears.Select(v => $"_{v.Trim()}"))
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

            switch (arg)
            {
                case TypeArguments.Curr44:
                case TypeArguments.Last44:
                case TypeArguments.Prev44:
                    FileLog = $"{LogPath}{Path.DirectorySeparatorChar}Tenders44_{LocalDate:dd_MM_yyyy}.log";
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(arg), arg, null);
            }
        }

        private static void ParserTender44(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Tenders44");
            ParserTend44 t44 = new ParserTend44(Periodparsing);
            t44.Parsing();
            /*ParserTend44 t44 = new ParserTend44(Periodparsing);
            FileInfo f = new FileInfo("/home/alex/RiderProjects/ParserTenders/ParserTenders/bin/fcsNotificationINM111_0342100025718000009_16271439.xml");
            t44.ParsingXml(f, "br", 32, TypeFile44.TypeTen44);*/
            Log.Logger("Добавили tender44", AddTender44);
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

        private static void ParserTender223(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Tenders223");
            ParserTend223 t223 = new ParserTend223(Periodparsing);
            t223.Parsing();
            /*ParserTend223 t223 = new ParserTend223(Periodparsing);
            FileInfo f = new FileInfo("/home/alex/Рабочий стол/parser/purchaseNotice_Belgorodskaya_obl_20170201_000000_20170228_235959_015.xml");
            t223.ParsingXML(f, "br", 32, TypeFile223.purchaseNotice);*/
            Log.Logger("Добавили tender223", AddTender223);
            Log.Logger("Время окончания парсинга Tenders223");
        }

        private static void ParserAtt(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Attach");
            ParserAttach att = new ParserAttach(TypeArguments.Attach);
            att.Parsing();
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
            ParserSgn223 s = new ParserSgn223(Periodparsing);
            s.Parsing();
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
            ParserGpb p = new ParserGpb(Periodparsing);
            p.Parsing();
            Log.Logger("Добавили Gpb", AddGazprom);
            AddGazprom = 0;
            ParserGpbGaz d = new ParserGpbGaz(Periodparsing);
            d.Parsing();
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
            Log.Logger("Время окончания парсинга Gpb");
        }

        private static void ParserExp(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Explanation");
            ParserExp p = new ParserExp(arg);
            p.Parsing();
            Log.Logger("Добавили Explanation", AddClarification223);
            Log.Logger("Время окончания парсинга Explanation");
        }

        private static void ParserGntWeb(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга GntWeb");
            ParserGntWeb p = new ParserGntWeb(arg);
            p.Parsing();
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
            Log.Logger("Время окончания парсинга GntWeb");
        }

        private static void ParserObTorgWeb(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга ObTorgWeb");
            ParserObTorgWeb p = new ParserObTorgWeb(arg);
            p.Parsing();
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
            Log.Logger("Время окончания парсинга ObTorgWeb");
        }

        private static void ParserSpecTorgTorgWeb(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга SpecTorgWeb");
            ParserSpecTorgWeb p = new ParserSpecTorgWeb(arg);
            p.Parsing();
            Log.Logger("Добавили SpecTorgWeb", AddSpecTorgWeb);
            Log.Logger("Время окончания парсинга SpecTorgWeb");
        }

        private static void ParserWeb(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Web");
            ParserTendersWeb p = new ParserTendersWeb(arg);
            p.Parsing();
            Log.Logger("Добавили tender44", AddTender44);
            Log.Logger("Добавили tender223", AddTender223);
            Log.Logger("Время окончания парсинга Web");
        }

        private static void ParserMrsk(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Mrsk");
            ParserMrsk p = new ParserMrsk(arg);
            p.Parsing();
            Log.Logger("Добавили Mrsk", AddMrsk);
        }

        private static void ParserRosneft(TypeArguments arg)
        {
            Log.Logger("Время начала парсинга Rosneft");
            ParserRosneft p = new ParserRosneft(arg);
            p.Parsing();
            Log.Logger("Добавили Rosneft", AddRosneft);
        }

        private static void ParserSakhalin(TypeArguments arg)
        {
            Log.Logger($"Время начала парсинга {arg}");
            ParserSakhalin p = new ParserSakhalin(arg);
            p.Parsing();
            Log.Logger($"Добавили {arg}", AddSakhalin);
        }

        private static void ParserTektorgGazprom(TypeArguments arg)
        {
            Log.Logger($"Время начала парсинга {arg}");
            ParserTektorgGazprom p = new ParserTektorgGazprom(arg);
            p.Parsing();
            Log.Logger($"Добавили {arg}", AddTektorgGazprom);
        }
        
        private static void ParserTektorgInterRao(TypeArguments arg)
        {
            Log.Logger($"Время начала парсинга {arg}");
            ParserTektorgInterRao p = new ParserTektorgInterRao(arg);
            p.Parsing();
            Log.Logger($"Добавили {arg}", AddTektorgInterRao);
        }
        
        private static void ParserTektorgRzd(TypeArguments arg)
        {
            Log.Logger($"Время начала парсинга {arg}");
            ParserTektorgRzd p = new ParserTektorgRzd(arg);
            p.Parsing();
            Log.Logger($"Добавили {arg}", AddTektorgRzd);
        }
    }
}