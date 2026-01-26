using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models
{
    public class InDataPaternAccepting
    {
        public string link { get; set; }
        public string number { get; set; }
    }

    public class SendMessagePaternModel
    {
        public string op { get; set; }
        public string user { get; set; }
        public string pass { get; set; }
        public string fromNum { get; set; }
        public string toNum { get; set; }
        public string patternCode { get; set; }
        public List<InDataPaternAccepting> inputData { get; set; }
    }




}
