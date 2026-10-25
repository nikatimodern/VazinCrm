using System.Collections.Generic;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ASettings.Entities;
using PreFactorEntity = CRM.Core.Domain.PreFactors.Entities.PreFactor;

namespace CRM.EndPoint.FrontEnd.Models.PreFactors
{
    public class PrefactorViewModel
    {
        public IList<PreFactorEntity> data { get; set; }
        public IList<AppUser> UserTamin { get; set; }
        public ASetting _Setting { get; set; }
        public IList<string> rols { get; set; } 

    }
}
