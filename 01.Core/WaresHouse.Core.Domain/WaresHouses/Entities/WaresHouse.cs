using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wares.Core.Domain.WaresHouses.Entities
{
    public class WaresHouse : BaseEntity
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductSmallName { get; set; }
        public int Unit { get; set; }
        public string UnitTxt { get; set; }
        public ImpExpEnum _Type { get; set; }//1 or 2
        public string _TypeTxt { get; set; }//ورود یا خروج
        public string date { get; set; }
        public string DateMiladi { get; set; }

    }


    public enum ImpExpEnum
    {
       ورود=1,
       خروج=2
    }

}
