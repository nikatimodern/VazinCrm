using System;

namespace CRM.Core.Domain.Factors.Entities
{
    public class FactorDescription
    {

        public int? Id { get; set; }
        public int? Order { get; set; }
        public string Text { get; set; }
        public string Dir { get; set; }
        public Guid? factorId { get; set; }
        public bool IsDeleted { get; set; } = false;

    }
}
