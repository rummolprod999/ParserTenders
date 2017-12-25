namespace ParserTenders
{
    public class CustomerGpB : OrganizerGpB
    {
        public string Ogrn { get; set; }
        public string CustomerRegNumber { get; set; }
        public CustomerGpB()
        {
            Ogrn = "";
            CustomerRegNumber = "";
        }
    }
}