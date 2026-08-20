using System.Collections.Generic;
using CRM.Core.Domain.ASettings.Entities;
using PreFactorEntity = CRM.Core.Domain.PreFactors.Entities.PreFactor;

namespace CRM.EndPoint.FrontEnd.Models.Cartable;

public class TaminViewModel {
   public IList<PreFactorEntity> PreFactors {get; set; }
   public ASetting Setting { get; set; }
   public string UserId { get; set; }
   public bool IsAdmin { get; set; }
}