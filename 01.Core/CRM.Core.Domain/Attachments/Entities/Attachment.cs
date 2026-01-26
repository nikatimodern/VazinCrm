using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.Contracts.Entities;
using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace CRM.Core.Domain.Attachments.Entities
{
    public class Attachment : BaseEntity
    {
        public _AttachType _AttachType { get; set; }
        public string _AttachTypeTxt { get; set; }
        public DateTime? AttachDate { get; set; }
        public string AttachDateTxt { get; set; }

        
        public Guid? EntityId { get; set; }
        public string Url { get; set; }
        public string Filename { get; set; }
        public string Filetype { get; set; }
        public string fileExten { get; set; }
        public byte[] File { get; set; }

        
        public Guid? contractId { get; set; }
    }




}
