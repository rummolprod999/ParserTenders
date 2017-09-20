using System;
using System.Collections.Generic;

namespace ParserTenders
{
    public class ProcedureGpB
    {
        public string RegistryNumber { get; set;}
        public Dictionary<int, int> Lots { get; set;}
        public  string Xml { get; set;}
        public string IdXml { get; set;}
        public int Version { get; set;}
        public DateTime DatePublished { get; set;}
        public string Href { get; set;}
        public string PurchaseObjectInfo { get; set;}
        public DateTime DateVersion { get; set;}
        public string NoticeVersion { get; set;}
        public string Printform { get; set;}
        public int IdOrg { get; set;}
        public int IdPlacingWay { get; set;}
        public int IdEtp { get; set;}
        public DateTime EndDate { get; set;}
        public DateTime ScoringDate { get; set;}
        public DateTime BiddingDate { get; set;}
    }
}