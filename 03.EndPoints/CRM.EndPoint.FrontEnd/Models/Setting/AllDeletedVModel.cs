using System;
using CRM.Core.Domain;

namespace CRM.EndPoint.FrontEnd.Models.Setting
{
    public class AllDeletedVModel
    {
        
        public Guid EntityId { get; set; }
        public EntityTypeEnum EntityType { get; set; }
        public string EntityTypeTxt { get; set; }
        public string CodeNumber { get; set; }
        public string title { get; set; }
        public DateTime ModifyDate { get; set; }
        public string ModifyDateTxt { get; set; }
        public string ModifyUser { get; set; }
    }
}
