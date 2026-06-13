using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Transfers.Enums;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Transfers.Entities;

public class Transfer : BaseEntity {
    public int TransferNumber { get; set; }
    public decimal Amount { get; set; }
    public string FromAccountTxt { get; set; }
    public Guid? FromAccountId { get; set; }
    public string ToAccountTxt { get; set; }
    public Guid? ToAccountId { get; set; }


    public TransferTypeEnum TransferType { get; set; }
    public DateTime? TransferDateMiladi { get; set; }
    public string TransferDate { get; set; }

    public string Description { get; set; }

    [NotMapped] public IList<Attachment> Attachments { get; set; }
}