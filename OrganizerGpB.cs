namespace ParserTenders
{
    public class OrganizerGpB
    {
        public OrganizerGpB() : this("", "", "", "", "", "", "", "", "", "", "")
        {
        }

        private OrganizerGpB(string organiserCustomerId, string inn, string kpp, string fullName, string postAddress,
            string factAddress, string responsibleRole, string contactPerson, string contactEmail, string contactPhone,
            string contactFax)
        {
            this.OrganiserCustomerId = organiserCustomerId;
            this.Inn = inn;
            this.Kpp = kpp;
            this.FullName = fullName;
            this.PostAddress = postAddress;
            this.FactAddress = factAddress;
            this.ResponsibleRole = responsibleRole;
            this.ContactPerson = contactPerson;
            this.ContactEmail = contactEmail;
            this.ContactPhone = contactPhone;
            this.ContactFax = contactFax;
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