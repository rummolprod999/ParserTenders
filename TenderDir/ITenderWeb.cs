﻿namespace ParserTenders.TenderDir
{
    public interface ITenderWeb
    {
        string EtpName { get; set; }
        string EtpUrl { get; set; }
        int TypeFz { get; set; }
    }
}