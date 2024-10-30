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
        public readonly string LogPathSignProj44;
        public readonly string LogPathTektorgGazprom;
        public readonly string LogPathTektorgInterRao;
        public readonly string LogPathTektorgRzd;
        public readonly string LogPathTenders223;
        public readonly string LogPathTenders44;
        public readonly string LogPathReq44;
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
        public readonly string UseWeb;
        public readonly string Server;
        public readonly string TempGntWeb;
        public readonly string TempMrsk;
        public readonly string TempObTorgWeb;
        public readonly string TempPathAttach;
        public readonly string TempPathExp223;
        public readonly string TempPathGazProm;
        public readonly string TempPathSign223;
        public readonly string TempPathSignProj44;
        public readonly string TempPathTektorgGazprom;
        public readonly string TempPathTektorgInterRao;
        public readonly string TempPathTektorgRzd;
        public readonly string TempPathTenders223;
        public readonly string TempPathTenders44;
        public readonly string TempPathReq44;
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
            var xDoc = new XmlDocument();
            xDoc.Load(Program.PathProgram + Path.DirectorySeparatorChar + "setting_tenders.xml");
            var xRoot = xDoc.DocumentElement;
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
                        case "logdir_sign_proj44":
                            LogPathSignProj44 =
                                $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_sign_proj44":
                            TempPathSignProj44 =
                                $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_preq44":
                            LogPathReq44 =
                                $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "tempdir_preq44":
                            TempPathReq44 =
                                $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "prefix":
                            Prefix = xnode.InnerText;
                            break;
                        case "useweb":
                            UseWeb = xnode.InnerText;
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
                            Port = int.TryParse(xnode.InnerText, out Port) ? int.Parse(xnode.InnerText) : 3306;
                            break;
                        case "max_thread":
                            MaxThread = int.TryParse(xnode.InnerText, out MaxThread)
                                ? int.Parse(xnode.InnerText)
                                : 20;
                            break;
                        case "max_try_down":
                            MaxTryDown = int.TryParse(xnode.InnerText, out MaxTryDown)
                                ? int.Parse(xnode.InnerText)
                                : 250;
                            break;
                        case "years":
                            Years = xnode.InnerText;
                            break;
                    }
                }
            }

            if (string.IsNullOrEmpty(LogPathTenders44) || string.IsNullOrEmpty(TempPathTenders44) ||
                string.IsNullOrEmpty(Database) || string.IsNullOrEmpty(UserDb) || string.IsNullOrEmpty(Server) ||
                string.IsNullOrEmpty(Years) || string.IsNullOrEmpty(TempPathTenders223) ||
                string.IsNullOrEmpty(LogPathTenders223) || string.IsNullOrEmpty(LogPathAttach) ||
                string.IsNullOrEmpty(TempPathAttach) ||
                string.IsNullOrEmpty(TempPathSign223) ||
                string.IsNullOrEmpty(LogPathSign223) || string.IsNullOrEmpty(LogPathGazProm) ||
                string.IsNullOrEmpty(TempPathGazProm) || string.IsNullOrEmpty(LogPathExp223) ||
                string.IsNullOrEmpty(TempPathExp223) || string.IsNullOrEmpty(LogGntWeb) ||
                string.IsNullOrEmpty(TempGntWeb) || string.IsNullOrEmpty(LogObTorgWeb) ||
                string.IsNullOrEmpty(TempObTorgWeb) || string.IsNullOrEmpty(LogSpecTorgWeb) ||
                string.IsNullOrEmpty(TempSpecTorgWeb) || string.IsNullOrEmpty(LogPathTendersWeb) ||
                string.IsNullOrEmpty(TempPathTendersWeb) || string.IsNullOrEmpty(LogMrsk) ||
                string.IsNullOrEmpty(TempMrsk) || string.IsNullOrEmpty(LogRosneft) ||
                string.IsNullOrEmpty(TempRosneft) || string.IsNullOrEmpty(LogSakhalin) ||
                string.IsNullOrEmpty(TempSakhalin) || string.IsNullOrEmpty(LogPathTektorgGazprom) ||
                string.IsNullOrEmpty(TempPathTektorgGazprom) || string.IsNullOrEmpty(LogPathTektorgInterRao) ||
                string.IsNullOrEmpty(TempPathTektorgInterRao) || string.IsNullOrEmpty(LogPathTektorgRzd) ||
                string.IsNullOrEmpty(TempPathTektorgRzd) || string.IsNullOrEmpty(LogPathTenders615) ||
                string.IsNullOrEmpty(TempPathTenders615) || string.IsNullOrEmpty(LogPathTendersWeb44) ||
                string.IsNullOrEmpty(TempPathTendersWeb44) || string.IsNullOrEmpty(LogPathSignProj44) ||
                string.IsNullOrEmpty(TempPathReq44) || string.IsNullOrEmpty(LogPathReq44) ||
                string.IsNullOrEmpty(TempPathSignProj44))
            {
                Console.WriteLine("Некоторые поля в файле настроек пустые");
                Environment.Exit(0);
            }
        }
    }
}