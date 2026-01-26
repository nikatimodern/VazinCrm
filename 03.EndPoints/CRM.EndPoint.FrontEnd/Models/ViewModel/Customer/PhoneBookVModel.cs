using System;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Customer
{
    public class PhoneBookVModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public int CustomerCode { get; set; }
        public DateTime? ModifyDate { get; set; }
        public string NormalizedAgentName { get; set; }
        public string NormalizedCompanyName { get; set; }

    }
}
