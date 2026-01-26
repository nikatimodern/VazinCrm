using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using System.Collections.Generic;


namespace CRM.EndPoint.FrontEnd.Models.ViewModel.PreFactorVm
{
    public class PrefactorViewModel
    {
        public IList<PreFactor> data { get; set; }
        public IList<AppUser> UserTamin { get; set; }
        public ASetting _Setting { get; set; }
        public IList<string> rols { get; set; } 

    }
}
