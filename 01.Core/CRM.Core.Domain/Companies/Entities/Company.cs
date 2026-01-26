using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.Company.Entities
{
    public class Company:BaseEntity
    {
        public string CompanyName { get; set; }
        public string CompanyBrandName { get; set; }
        public string ShenaseMeli { get; set; }
        public string EghtesadiCode { get; set; }
        public string NumberRegister { get; set; }
        public string Tel { get; set; }
        public string Mobile { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
        public IList<CompanyFile> CompanyFiles { get; set; }

    }

    public class CompanyFile
    {
        public Guid Id { get; set; }
        public string Filename { get; set; }
        public string Filetype { get; set; }
        public string fileExten { get; set; }
        public byte[] file { get; set; }
        public string FileUrl { get; set; }

    }

}
