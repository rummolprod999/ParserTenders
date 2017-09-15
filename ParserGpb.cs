namespace ParserTenders
{
    public class ParserGpb : ParserWeb
    {
        private string UrlListTenders;
        private string UrlTender;
        private string UrlCustomerId;
        private string UrlCustomerInnKpp;

        public ParserGpb(TypeArguments a) : base(a)
        {
            UrlListTenders = "https://etp.gpb.ru/api/procedures.php?late=0";
            UrlTender = "https://etp.gpb.ru/api/procedures.php?regid=";
            UrlCustomerId = "https://etp.gpb.ru/api/company.php?id=";
            UrlCustomerInnKpp = "https://etp.gpb.ru/api/company.php?inn={inn}&kpp={kpp}";
        }

        public override void Parsing()
        {
            string xml = DownloadString.DownL(UrlListTenders);
            if (xml.Length < 100)
            {
                Log.Logger("Получили пустую строку со списком торгов", UrlListTenders);
                return;
            }
        }
    }
}