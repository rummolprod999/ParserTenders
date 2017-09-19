namespace ParserTenders
{
    public class OrganizerGpB
    {
        public OrganizerGpB() : this("", "", "", "", "", "", "", "", "", "", "")
        {
        }

        private OrganizerGpB(string OrganiserCustomerId, string Inn, string Kpp, string FullName, string PostAddress,
            string FactAddress, string ResponsibleRole, string ContactPerson, string ContactEmail, string ContactPhone,
            string ContactFax)
        {
            this.OrganiserCustomerId = OrganiserCustomerId;
            this.Inn = Inn;
            this.Kpp = Kpp;
            this.FullName = FullName;
            this.PostAddress = PostAddress;
            this.FactAddress = FactAddress;
            this.ResponsibleRole = ResponsibleRole;
            this.ContactPerson = ContactPerson;
            this.ContactEmail = ContactEmail;
            this.ContactPhone = ContactPhone;
            this.ContactFax = ContactFax;
        }

        public string OrganiserCustomerId { get; set; }
        public string Inn { get; set; }
        public string Kpp { get; set; }
        public string FullName { get; set; }
        public string PostAddress { get; set; }
        public string FactAddress { get; set; }
        public string ResponsibleRole { get; set; }
        public string ContactPerson { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string ContactFax { get; set; }
    }
}