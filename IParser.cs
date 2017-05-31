using System.Data;

namespace ParserTenders
{
    public interface IParser
    {
        void Parsing();
        DataTable GetRegions();
        WorkWithFtp ClientFtp44();
        void GetListFileArch(string Arch, string PathParse, string region);
        void GetListFileArch(string Arch, string PathParse, string region, int region_id);
        string GetArch44(string Arch, string PathParse);
    }
}