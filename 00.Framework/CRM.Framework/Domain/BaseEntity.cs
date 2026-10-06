using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Framework.Domain
{
    public class BaseEntity
    {
        //public BaseEntity()
        //{
        //    if (Id == Guid.Empty)
        //    {
        //        Id = Guid.NewGuid();
        //    }
        //}
        public Guid Id { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.Now;
        public DateTime? ModifyDate { get; set; } = DateTime.Now;
        public string CreateDateTxt { get; set; } = BaseServices.GetDateTimePersian();
        public string ModifyDateTxt { get; set; } = BaseServices.GetDateTimePersian();
        public bool IsDeleted { get; set; } = false;
        //public EnStatus enStatus { get; set; } = EnStatus.حذف_نشده;
        public string UserCreatorID { get; set; }
        public string UserCreator { get; set; }
        public string UserModifyID { get; set; }
        public string UserModify { get; set; }
        public string CommentText { get; set; }

        [NotMapped]
        public string UserImage { get; set; }
    }

    public enum EnStatus
    {
        حذف_نشده=0,
        حذف_شده=1,
        حذف_شده_سیستمی=2,
        
    }
    


}
