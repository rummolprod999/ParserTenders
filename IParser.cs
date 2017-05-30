using System.Data;

namespace ParserTenders
{
    public interface IParser
    {
        void Parsing();
        DataTable GetRegions();
        WorkWithFtp ClientFtp44();
        void GetListFileArch(string Arch, string PathParse, string region);
        string GetArch(string Arch, string PathParse);
    }
}