using System;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.RemainedPf
{
    public class AllItemsViewModel
    {

        public Guid? PfId { get; set; }
        public Guid? PfItemid { get; set; }
        public string Product { get; set; }
        public string PfWeightCount { get; set; }
        public string FWeightCount { get; set; }
        public string Remained { get; set; }
        public bool RemainedFlag { get; set; }

    }
}
