using System;
using CRM.Core.Domain.Cartables.Entities;

// ReSharper disable InconsistentNaming

namespace CRM.Core.Domain.LogSystems.Entities {
    public class LogSystem {
        public Guid Id { get; set; }
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public int? EntitySerialNumber { get; set; }
        public string ParentEntityId { get; set; }
        public string ParentEntityName { get; set; }
        public DateTime ChangeTime { get; set; }
        public string ChangeTimeTxt { get; set; }
        public string ChangedBy { get; set; }
        public string ChangedById { get; set; }
        public LogActionEnum ActionType { get; set; }
        public string ActionTypeTxt { get; set; }
        public string ChangeDetails { get; set; }
        public string CommentTxt { get; set; }
        public string Description { get; set; }
        public Cartable Cartable { get; set; }
    }

    public enum LogActionEnum {
        ثبت = 1,
        ویرایش = 2,
        حذف = 3,
        بازیابی = 4,
        ورود_کاربر = 5,
        خروج_کاربر = 6,
        تغییر_وضعیت = 7,
        تایید_و_تغییر_وضعیت = 8,
        رد_و_تغییر_وضعیت = 9
    }
}