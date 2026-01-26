using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Carizos.Entities;
using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel.CarizoVm
{
    public class CarizoViewModel
    {
        public int loc{ get; set; }
        public int _Type{ get; set; }
        public List<Carizo> Data { get; set; }

        public AppUser CurrentUser { get; set; }

    }
}
