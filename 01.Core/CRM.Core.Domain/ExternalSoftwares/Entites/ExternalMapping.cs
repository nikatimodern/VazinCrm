using System;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Framework;

namespace CRM.Core.Domain.ExternalSoftwares.Entites;

public class ExternalMapping {
    public Guid Id { get; set; }
    public string EntityName { get; set; }
    public Guid EntityId { get; set; }
    public Guid ExternalSoftwareId { get; set; }
    public ExternalSoftware ExternalSoftware { get; set; }

    public ExternalMappingTypeEnum MapType { get; set; }
    public string MapTypeTxt { get; set; }

    public string ExEntityName { get; set; } // نام انتیتی ثبت شده در نرم افزار خروجی
    public string ExEntityId { get; set; } // ایدی ثبت شده در نرم افزار خروجی


    public DateTime CreateDate { get; set; } = DateTime.Now;
    public string CreateDateTxt { get; set; } = BaseServices.GetDateTimePersian();

    public string UserCreatorID { get; set; }
    public string UserCreator { get; set; }
}