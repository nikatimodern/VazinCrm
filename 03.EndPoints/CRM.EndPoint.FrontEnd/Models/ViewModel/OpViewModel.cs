using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel
{
    public class OpViewModel
    {
        public int id { get; set; }
        public string text { get; set; }
    }
    public class OpViewModelGuid
    {
        public Guid id { get; set; }
        public string text { get; set; }
    }
}
