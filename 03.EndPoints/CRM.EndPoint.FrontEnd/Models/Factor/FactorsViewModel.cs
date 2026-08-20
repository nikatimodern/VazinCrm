using System.Collections.Generic;
using CRM.Core.Domain.ASettings.Entities;
using FactorEntity = CRM.Core.Domain.Factors.Entities.Factor;

namespace CRM.EndPoint.FrontEnd.Models.Factor {
    public class FactorsViewModel {
        public IList<FactorEntity> Factors { get; set; }
        public ASetting _Setting { get; set; }
    }
}