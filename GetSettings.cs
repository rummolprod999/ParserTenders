using System;
using System.Xml;
using System.IO;

namespace ParserTenders
{
    public class GetSettings
    {
        public readonly string Database;
        public readonly string TempPathTenders44;
        public readonly string LogPathTenders44;
        public readonly string TempPathTenders223;
        public readonly string LogPathTenders223;
        public readonly string LogPathAttach;
        public readonly string TempPathAttach;
        public readonly string LogPathSign223;
        public readonly string TempPathSign223;
        public readonly string Prefix;
        public readonly string UserDb;
        public readonly string PassDb;
        public readonly string Server;
        public readonly int Port;
        public readonly string Years;
        public readonly int MaxThread;
        public readonly int MaxTryDown;

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
                String.IsNullOrEmpty(LogPathSign223))
            {
                Console.WriteLine("Некоторые поля в файле настроек пустые");
                Environment.Exit(0);
            }
        }
    }
}