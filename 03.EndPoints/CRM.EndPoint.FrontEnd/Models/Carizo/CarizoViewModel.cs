using System.Collections.Generic;
using CRM.Core.Domain.AppUsers.Entities;

namespace CRM.EndPoint.FrontEnd.Models.Carizo
{
    public class CarizoViewModel
    {
        public int loc{ get; set; }
        public int _Type{ get; set; }
        public List<Core.Domain.Carizos.Entities.Carizo> Data { get; set; }

        public AppUser CurrentUser { get; set; }

    }
}
