using System;

namespace CRM.Framework.Domain
{
    public class JunctionBaseEntity
    {

        public DateTime CreateDate { get; set; } = DateTime.Now;
        public DateTime? ModifyDate { get; set; } = DateTime.Now;
        public string CreateDateTxt { get; set; } = BaseServices.GetDateTimePersian();
        public string ModifyDateTxt { get; set; } = BaseServices.GetDateTimePersian();
        public bool IsDeleted { get; set; } = false;

    }
}
