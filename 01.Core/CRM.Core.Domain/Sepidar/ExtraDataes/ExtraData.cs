using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.Sepidar.ExtraDataes
{
    public class ExtraData
    {
        public int ExtraDataID { get; set; }
        public string EntityTypeName { get; set; }
        public int EntityRef { get; set; }
        public string Note { get; set; }


    }
}
