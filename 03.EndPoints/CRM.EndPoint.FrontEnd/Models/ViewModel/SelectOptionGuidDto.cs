using System;

namespace CRM.EndPoint.FrontEnd.Models.ViewModel
{
    public class SelectOptionGuidDto
    {
        public Guid id { get; set; }
        public string text { get; set; }
        public bool? Target { get; set; } = false;

    }
}
