using System;

namespace CRM.Core.Domain.Asterisk.Entities
{
    public class Cdr
    {
        public DateTime calldate { get; set; }
        public string clid { get; set; }
        public string src { get; set; }
        public string dst { get; set; }
        public string dcontext { get; set; }
        public string channel { get; set; }
        public string dstchannel { get; set; }
        public string lastapp { get; set; }
        public string lastdata { get; set; }
        public int duration { get; set; }
        public int billsec { get; set; }
        public string disposition { get; set; }
        public int amaflags { get; set; }
        public string accountcode { get; set; }
        public string uniqueid { get; set; }
        public string userfield { get; set; }
        public string recordingfile { get; set; }
        public string cnum { get; set; }
        public string cnam { get; set; }
        public string outbound_cnum { get; set; }
        public string outbound_cnam { get; set; }
        public string dst_cnam { get; set; }
        public string did { get; set; }
    }
}