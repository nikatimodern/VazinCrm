using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.PreFactors.Entities
{
    public class BaseDescription
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public string Text { get; set; }
        public string UserId { get; set; }
        public string Dir { get; set; } = "RTL";
        public bool? Official { get; set; }
        public byte Type { get; set; } = 0; //0=prefactor -- 1=factor
    }
}
