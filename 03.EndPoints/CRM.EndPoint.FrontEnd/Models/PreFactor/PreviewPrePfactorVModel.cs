using System.Collections.Generic;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Company.Entities;
using CRM.EndPoint.FrontEnd.Models.Factor;
using PreFactorEntity = CRM.Core.Domain.PreFactors.Entities.PreFactor;

namespace CRM.EndPoint.FrontEnd.Models.PreFactor
{
    public class PreviewPrePfactorVModel
    {
        public PreFactorEntity PreFactor { get; set; }
        public Company Company { get; set; }
//        public Customer _Customer { get; set; }
        public string Siq { get; set; }
        public string Mohr { get; set; }
        public string MohrR { get; set; }
        public string Logo { get; set; }
        public ASetting _Setting { get; set; }
        public List<BaseDescriptionVModel> TermsPreFactor { get; set; }
    }


}
