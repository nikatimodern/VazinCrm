using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Attachments.Entities;
using System.Collections.Generic;


namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Accountency
{
    public class AccViewModel
    {
        public Accountancy AccData { get; set; }
        public List<Attachment> ListAttach { get; set; }

    }
}
