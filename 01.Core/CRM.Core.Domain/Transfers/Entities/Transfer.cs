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

    private TransferStatusEnum _status = TransferStatusEnum.ثبت_اولیه;

    public TransferStatusEnum Status {
        get => _status;
        set {
            _status = value;
            StatusTxt = _status.ToString().Replace("_", " ");
        }
    }

    public string StatusTxt { get; private set; }

    public string Description { get; set; }

    [NotMapped] public IList<Attachment> Attachments { get; set; }
}