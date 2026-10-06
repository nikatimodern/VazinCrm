using System;

namespace CRM.Core.Domain.PreFactors.Entities
{
    public class PreFactorDescription
    {
        public int? Id { get; set; }
        public int? Order { get; set; }
        public string Text { get; set; }
        public string Dir { get; set; }
        public Guid? PrefactorId { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool? Official { get; set; }
    }
}
