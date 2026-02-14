using System.Collections.Generic;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Supplies.Entities;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Supply;

public class AddSupplyFactorVModel {
    public SupplyFactor SupplyFactor { get; set; }
    public IList<Attachment> Attachments { get; set; }
}