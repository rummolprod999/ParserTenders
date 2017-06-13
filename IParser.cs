using System.Data;
using FluentFTP;

namespace ParserTenders
{
    public interface IParser
    {
        void Parsing();
        DataTable GetRegions();
        void GetListFileArch(string Arch, string PathParse, string region);
        void GetListFileArch(string Arch, string PathParse, string region, int region_id);
        void GetListFileArch(string Arch, string PathParse, string region, int region_id, string purchase);
        string GetArch44(string Arch, string PathParse);
        string GetArch223(string Arch, string PathParse);
    }
}