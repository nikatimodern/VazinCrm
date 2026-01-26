using System.Collections.Generic;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.PreFactors.Entities;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.Cartable;

public class TaminViewModel {
   public IList<PreFactor> PreFactors {get; set; }
   public ASetting Setting { get; set; }

}