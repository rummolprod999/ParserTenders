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
            OrganiserCustomerId = organiserCustomerId;
            Inn = inn;
            Kpp = kpp;
            FullName = fullName;
            PostAddress = postAddress;
            FactAddress = factAddress;
            ResponsibleRole = responsibleRole;
            ContactPerson = contactPerson;
            ContactEmail = contactEmail;
            ContactPhone = contactPhone;
            ContactFax = contactFax;
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