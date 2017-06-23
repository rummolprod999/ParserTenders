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
        public readonly string Prefix;
        public readonly string UserDB;
        public readonly string PassDB;
        public readonly string Server;
        public readonly int Port;
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
                        case "tempdir_tenders223":
                            TempPathTenders223 = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "logdir_tenders223":
                            LogPathTenders223 = $"{Program.PathProgram}{Path.DirectorySeparatorChar}{xnode.InnerText}";
                            break;
                        case "prefix":
                            Prefix = xnode.InnerText;
                            break;
                        case "userdb":
                            UserDB = xnode.InnerText;
                            break;
                        case "passdb":
                            PassDB = xnode.InnerText;
                            break;
                        case "server":
                            Server = xnode.InnerText;
                            break;
                        case "port":
                            Port = Int32.TryParse(xnode.InnerText, out Port) ? Int32.Parse(xnode.InnerText) : 3306;
                            break;
                        case "years":
                            Years = xnode.InnerText;
                            break;
                    }
                }
            }

            if (String.IsNullOrEmpty(LogPathTenders44) || String.IsNullOrEmpty(TempPathTenders44) ||
                String.IsNullOrEmpty(Database) || String.IsNullOrEmpty(UserDB) || String.IsNullOrEmpty(Server) ||
                String.IsNullOrEmpty(Years))
            {
                Console.WriteLine("Некоторые поля в файле настроек пустые");
                Environment.Exit(0);
            }
        }
    }
}