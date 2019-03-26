using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ParserTenders.ParserDir
{
    public class ParserSignProj44 : Parser
    {
        private string[] _fileSign = new[] {"cpcontractsign_"};

        protected DataTable DtRegion;

        public ParserSignProj44(TypeArguments arg) : base(arg)
        {
        }

        public override void Parsing()
        {
            DtRegion = GetRegions();
            foreach (DataRow row in DtRegion.Rows)
            {
                var arch = new List<string>();
                var pathParse = "";
                var regionPath = (string) row["path"];
                switch (Program.Periodparsing)
                {
                    case TypeArguments.Last44:
                        pathParse = $"/fcs_regions/{regionPath}/contractprojects/";
                        arch = GetListArchLast(pathParse, regionPath);
                        break;
                    case TypeArguments.Curr44:
                        pathParse = $"/fcs_regions/{regionPath}/contractprojects/currMonth/";
                        arch = GetListArchCurr(pathParse, regionPath);
                        break;
                    case TypeArguments.Prev44:
                        pathParse = $"/fcs_regions/{regionPath}/contractprojects/prevMonth/";
                        arch = GetListArchPrev(pathParse, regionPath);
                        break;
                }

                if (arch.Count == 0)
                {
                    Log.Logger("Получен пустой список архивов", pathParse);
                    continue;
                }

                foreach (var v in arch)
                {
                    GetListFileArch(v, pathParse, (string) row["conf"], (int) row["id"]);
                }
            }
        }

        public override List<String> GetListArchLast(string pathParse, string regionPath)
        {
            var archtemp = GetListFtp44(pathParse);
            var yearsSearch = Program.Years.Select(y => $"contractproject_{regionPath}{y}").ToList();
            yearsSearch.AddRange(Program.Years.Select(y => $"contractproject{y}").ToList());
            return archtemp.Where(a => yearsSearch.Any(t => a.IndexOf(t, StringComparison.Ordinal) != -1)).ToList();
        }
    }
}