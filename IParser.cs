using System.Data;
using FluentFTP;

namespace ParserTenders
{
    public interface IParser
    {
        void Parsing();
        DataTable GetRegions();
        void GetListFileArch(string arch, string pathParse, string region);
        void GetListFileArch(string arch, string pathParse, string region, int regionId);
        void GetListFileArch(string arch, string pathParse, string region, int regionId, string purchase);
        string GetArch44(string arch, string pathParse);
        string GetArch223(string arch, string pathParse);
    }
}