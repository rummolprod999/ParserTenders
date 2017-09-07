using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ParserTenders
{
    public class ParserSgn223 : Parser
    {
        protected DataTable DtRegion;

        private string[] file_sign223 = new[]
            {"contract_"};
        
        public ParserSgn223(TypeArguments arg) : base(arg)
        {
        }

        public override void Parsing()
        {
            DtRegion = GetRegions();
            foreach (DataRow row in DtRegion.Rows)
            {
                List<String> arch = new List<string>();
                string PathParse = "";
                string RegionPath = (string) row["path223"];
                switch (Program.Periodparsing)
                {
                    case (TypeArguments.Last223):
                        PathParse = $"/out/published/{RegionPath}/contract/";
                        arch = GetListArchLast(PathParse, RegionPath);
                        break;
                    case (TypeArguments.Daily223):
                        PathParse = $"/out/published/{RegionPath}/contract/daily/";
                        arch = GetListArchDaily(PathParse, RegionPath);
                        break;
                }
            }
        }
        
        public override List<String> GetListArchLast(string PathParse, string RegionPath)
        {
            List<string> archtemp = new List<string>();
            /*FtpClient ftp = ClientFtp44();*/
            archtemp = GetListFtp223(PathParse);
            List<String> years_search = Program.Years.Select(y => $"contract_{RegionPath}{y}").ToList();
            return archtemp.Where(a => years_search.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)).ToList();
        }
    }
}