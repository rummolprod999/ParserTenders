using System.Data;

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
        }
    }
}