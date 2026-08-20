using System.Collections.Generic;
using CRM.Core.Domain.Attachments.Entities;
using AccountancyEntity = CRM.Core.Domain.Accountancies.Entities.Accountancy;

namespace CRM.EndPoint.FrontEnd.Models.Accountancy
{
    public class AccViewModel
    {
        public AccountancyEntity AccData { get; set; }
        public List<Attachment> ListAttach { get; set; }

    }
}
