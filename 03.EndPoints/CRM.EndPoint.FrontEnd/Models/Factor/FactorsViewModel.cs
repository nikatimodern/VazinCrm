using System.Collections.Generic;
using CRM.Core.Domain.ASettings.Entities;

namespace CRM.EndPoint.FrontEnd.Models.Factor
{
    public class FactorsViewModel
    {

        public IList<CRM.Core.Domain.Factors.Entities.Factor> factors { get; set; }
        public bool Financial { get; set; }
        public IList<string> Rols { get; set; }
        public ASetting _Setting { get; set; }
        

    }
}
