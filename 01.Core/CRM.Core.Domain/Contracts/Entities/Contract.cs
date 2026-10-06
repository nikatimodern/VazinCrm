using CRM.Core.Domain.Attachments.Entities;
using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM.Core.Domain.Contracts.Entities
{
   
    public class Contract:BaseEntity
    {
        [Required]
        public string CTitle { get; set; }
        [Required]
        public string Employer { get; set; }
        [Required]
        public string Contractor { get; set; }
        //public Guid Customer { get; set; }
        //public string CustomerTxt { get; set; }
        public DateTime? fromDate { get; set; }
        [Required]
        public string fromDateSh { get; set; }
        public DateTime? toDate { get; set; }
        [Required]
        public string toDateSh { get; set; }
        public string description { get; set; }

        [NotMapped]
        public IList<Attachment> Attachments { get; set; }

    }
}
