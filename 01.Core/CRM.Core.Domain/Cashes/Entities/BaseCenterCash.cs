using CRM.Framework.Domain;

namespace CRM.Core.Domain.Cashes.Entities;

public class BaseCenterCash : BaseEntity {
    public int Code { get; set; }
    public string Title { get; set; }
}