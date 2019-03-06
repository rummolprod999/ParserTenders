using System;
using System.IO;
using System.Xml;

namespace ParserTenders
{
    public class GetSettings
    {
        public readonly string Database;
        public readonly string LogGntWeb;
        public readonly string LogMrsk;
        public readonly string LogObTorgWeb;
        public readonly string LogPathAttach;
        public readonly string LogPathExp223;
        public readonly string LogPathGazProm;
        public readonly string LogPathSign223;
        public readonly string LogPathTektorgGazprom;
        public readonly string LogPathTektorgInterRao;
        public readonly string LogPathTektorgRzd;
        public readonly string LogPathTenders223;
        public readonly string LogPathTenders44;
        public readonly string LogPathTenders615;
        public readonly string LogPathTendersWeb;
        public readonly string LogPathTendersWeb44;
        public readonly string LogRosneft;
        public readonly string LogSakhalin;
        public readonly string LogSpecTorgWeb;
        public readonly int MaxThread;
        public readonly int MaxTryDown;
        public readonly string PassDb;
        public readonly int Port;
        public readonly string Prefix;
        public readonly string Server;
        public readonly string TempGntWeb;
        public readonly string TempMrsk;
        public readonly string TempObTorgWeb;
        public readonly string TempPathAttach;
        public readonly string TempPathExp223;
        public readonly string TempPathGazProm;
        public readonly string TempPathSign223;
        public readonly string TempPathTektorgGazprom;
        public readonly string TempPathTektorgInterRao;
        public readonly string TempPathTektorgRzd;
        public readonly string TempPathTenders223;
        public readonly string TempPathTenders44;
        public readonly string TempPathTenders615;
        public readonly string TempPathTendersWeb;
        public readonly string TempPathTendersWeb44;
        public readonly string TempRosneft;
        public readonly string TempSakhalin;
        public readonly string TempSpecTorgWeb;
        public readonly string UserDb;
        public readonly string Years;

        public GetSettings()
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(Program.PathProgram + Path.DirectorySeparatorChar + "setting_tenders.xml");
            XmlElement xRoot = xDoc.DocumentElement;
            if (xRoot != null)
            {
                foreach (XmlNode xnode in xRoot)
                {
                    switch (xnode.Name)
                    {
                        case "database":
                            Database = xnode.InnerText;
                            break;
                        case "tempdir_tenders44":
                            TempPathTenders44 = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_tenders44":
                            LogPathTenders44 = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_tenders615":
                            TempPathTenders615 = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_tenders615":
                            LogPathTenders615 = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_tenders223":
                            TempPathTenders223 = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_tenders223":
                            LogPathTenders223 = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_attach":
                            LogPathAttach = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_attach":
                            TempPathAttach = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_sign223":
                            LogPathSign223 = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_sign223":
                            TempPathSign223 = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_gazprom":
                            LogPathGazProm = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_gazprom":
                            TempPathGazProm = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_exp223":
                            LogPathExp223 = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_exp223":
                            TempPathExp223 = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_gntweb":
                            LogGntWeb = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_gntweb":
                            TempGntWeb = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_obtorgweb":
                            LogObTorgWeb = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_obtorgweb":
                            TempObTorgWeb = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_spectorgweb":
                            LogSpecTorgWeb = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_spectorgweb":
                            TempSpecTorgWeb = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_tenders_web":
                            LogPathTendersWeb = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_tenders_web":
                            TempPathTendersWeb = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_tenders_web44":
                            LogPathTendersWeb44 =
                                $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_tenders_web44":
                            TempPathTendersWeb44 =
                                $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_mrsk":
                            LogMrsk = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_mrsk":
                            TempMrsk = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_rosneft":
                            LogRosneft = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_rosneft":
                            TempRosneft = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_sakhalin":
                            LogSakhalin = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_sakhalin":
                            TempSakhalin = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_tektorg_gazprom":
                            LogPathTektorgGazprom =
                                $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_tektorg_gazprom":
                            TempPathTektorgGazprom =
                                $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_tektorg_interrao":
                            LogPathTektorgInterRao =
                                $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_tektorg_interrao":
                            TempPathTektorgInterRao =
                                $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_tektorg_rzd":
                            LogPathTektorgRzd =
                                $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_tektorg_rzd":
                            TempPathTektorgRzd =
                                $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "prefix":
                            Prefix = xnode.InnerText;
                            break;
                        case "userdb":
                            UserDb = xnode.InnerText;
                            break;
                        case "passdb":
                            PassDb = xnode.InnerText;
                            break;
                        case "server":
                            Server = xnode.InnerText;
                            break;
                        case "port":
                            Port = Int32.TryParse(xnode.InnerText, out Port) ? Int32.Parse(xnode.InnerText) : 3306;
                            break;
                        case "max_thread":
                            MaxThread = Int32.TryParse(xnode.InnerText, out MaxThread)
                                ? Int32.Parse(xnode.InnerText)
                                : 20;
                            break;
                        case "max_try_down":
                            MaxTryDown = Int32.TryParse(xnode.InnerText, out MaxTryDown)
                                ? Int32.Parse(xnode.InnerText)
                                : 250;
                            break;
                        case "years":
                            Years = xnode.InnerText;
                            break;
                    }
                }
            }

            if (String.IsNullOrEmpty(LogPathTenders44) || String.IsNullOrEmpty(TempPathTenders44) ||
                String.IsNullOrEmpty(Database) || String.IsNullOrEmpty(UserDb) || String.IsNullOrEmpty(Server) ||
                String.IsNullOrEmpty(Years) || String.IsNullOrEmpty(TempPathTenders223) ||
                String.IsNullOrEmpty(LogPathTenders223) || String.IsNullOrEmpty(LogPathAttach) ||
                String.IsNullOrEmpty(TempPathAttach) ||
                String.IsNullOrEmpty(TempPathSign223) ||
                String.IsNullOrEmpty(LogPathSign223) || String.IsNullOrEmpty(LogPathGazProm) ||
                String.IsNullOrEmpty(TempPathGazProm) || String.IsNullOrEmpty(LogPathExp223) ||
                String.IsNullOrEmpty(TempPathExp223) || String.IsNullOrEmpty(LogGntWeb) ||
                String.IsNullOrEmpty(TempGntWeb) || String.IsNullOrEmpty(LogObTorgWeb) ||
                String.IsNullOrEmpty(TempObTorgWeb) || String.IsNullOrEmpty(LogSpecTorgWeb) ||
                String.IsNullOrEmpty(TempSpecTorgWeb) || String.IsNullOrEmpty(LogPathTendersWeb) ||
                String.IsNullOrEmpty(TempPathTendersWeb) || String.IsNullOrEmpty(LogMrsk) ||
                String.IsNullOrEmpty(TempMrsk) || String.IsNullOrEmpty(LogRosneft) ||
                String.IsNullOrEmpty(TempRosneft) || String.IsNullOrEmpty(LogSakhalin) ||
                String.IsNullOrEmpty(TempSakhalin) || String.IsNullOrEmpty(LogPathTektorgGazprom) ||
                String.IsNullOrEmpty(TempPathTektorgGazprom) || String.IsNullOrEmpty(LogPathTektorgInterRao) ||
                String.IsNullOrEmpty(TempPathTektorgInterRao) || String.IsNullOrEmpty(LogPathTektorgRzd) ||
                String.IsNullOrEmpty(TempPathTektorgRzd) || String.IsNullOrEmpty(LogPathTenders615) ||
                String.IsNullOrEmpty(TempPathTenders615) || String.IsNullOrEmpty(LogPathTendersWeb44) ||
                String.IsNullOrEmpty(TempPathTendersWeb44))
            {
                Console.WriteLine("Некоторые поля в файле настроек пустые");
                Environment.Exit(0);
            }
        }
    }
}