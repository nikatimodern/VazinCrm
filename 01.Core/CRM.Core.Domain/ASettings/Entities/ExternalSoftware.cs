using System;
using System.Collections.Generic;
using CRM.Framework;

namespace CRM.Core.Domain.ASettings.Entities;

public class ExternalSoftware {
    public Guid Id { get; set; }
    public string Title { get; set; }

    public IList<ExternalMapping> ExternalMappings { get; set; }

    public DateTime CreateDate { get; set; } = DateTime.Now;
    public string CreateDateTxt { get; set; } = BaseServices.GetDateTimePersian();
}