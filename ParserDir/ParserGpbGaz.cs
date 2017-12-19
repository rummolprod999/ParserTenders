namespace ParserTenders.ParserDir
{
    public class ParserGpbGaz : ParserGpb
    {
        
        public ParserGpbGaz(TypeArguments a) : base(a)
        {
            _urlListTenders = "https://etpgaz.gazprombank.ru/api/procedures.php?late=0";
            _urlTender = "https://etpgaz.gazprombank.ru/api/procedures.php?regid=";
            _urlCustomerId = "https://etpgaz.gazprombank.ru/api/company.php?id=";
            _urlCustomerInnKpp = "https://etpgaz.gazprombank.ru/api/company.php?inn={inn}&kpp={kpp}";
            _etpUrl = "https://etpgaz.gazprombank.ru/";
        }
    }
}