using CRM.Core.Domain.Cartables.Enums;
using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CRM.Core.Domain.LogSystems.Entities;

namespace CRM.Core.Domain.Cartables.Entities {
    public class Cartable : BaseEntity {
        public CartableEnum? FromCartable { get; set; }
        public string FromCartableTxt { get; set; }


        public CartableEnum? ToCartable { get; set; }
        public string ToCartableTxt { get; set; }


        public LogSystem? LogSystem { get; set; }
        public Guid? LogSystemId { get; set; }
    }
}