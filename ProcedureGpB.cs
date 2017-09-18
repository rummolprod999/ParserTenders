using System.Collections.Generic;

namespace ParserTenders
{
    public class ProcedureGpB
    {
        public string RegistryNumber { get; set;}
        public Dictionary<int, int> Lots { get; set;}
        public  string Xml { get; set;}
        public string IdXml { get; set;}
        public string Version { get; set;}
        public string DatePublished { get; set;}
    }
}