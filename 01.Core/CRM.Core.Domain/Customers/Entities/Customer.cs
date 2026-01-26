using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Contacts.Entities;
using CRM.Core.Domain.Contracts.Entities;
using CRM.Core.Domain.Customers.Enum;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Domain.Deals.Entities;

namespace CRM.Core.Domain.Customers.Entities
{
    public class Customer : BaseEntity
    {
        public int CustomerCode { get; set; }
        public byte Juridical { get; set; }
        public byte Sex { get; set; } = 1;
        public string CompanyName { get; set; }
        public string NormalizedCompanyName { get; set; }
        public string EghtesadiCode { get; set; }
        public string NumberRegister { get; set; }
        public string ShenaseMeli { get; set; }
        public string AgentName { get; set; }
        public string NormalizedAgentName { get; set; }
        //public string AgentFamily { get; set; }
        public string AgentMeliCode { get; set; }
        //public string AgentShenasNameNumber { get; set; }
        public string Tel { get; set; }
        public string Mobile { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string Resposibility { get; set; }
        public string Internal { get; set; }
        public string ReAgent { get; set; }
        public Guid? WorkSenf { get; set; }
        public string WorkSenfTxt { get; set; }
        public string WorkSenfDescription { get; set; }
        public Guid? AcquaintanceWay { get; set; }
        public string AcquaintanceWayTxt { get; set; }
        public IList<OrderHead> orderHeads { get; set; }
        public IList<PreFactor> PreFactors { get; set; }
        //public IList<Factor> Factors { get; set; }
        public IList<UserCustomer> UserCustomers { get; set; }
        public IList<Accountancy> Accountancies { get; set; }
        public IList<Contract> Contracts { get; set; }
        public IList<Contact> Contacts { get; set; }
        public IList<Deal> Deals { get; set; }


        public DateTime? DateSort { get; set; }
        public string DateSortTxt { get; set; }
        public bool Accepted { get; set; } = false;
        public bool Success { get; set; } = false;//لید هایی که حتی یک فاکتور دارند وضعیتشان به فعال(مشتری) تغییر می کند کسانی که این فیلدشان فالس هست هنوز مشتری نشدند 
        public bool Export { get; set; } = false;//مشتری هایی که حداقل یک فاکتور صادراتی دارند جزو مشتریان صادراتی محصوب میشوند وضعیت این فیلد آنها فعال می شود
        public RoleTypeEnum RoleType { get; set; }
        public string RoleTypeTxt { get; set; }

        public string FinancialCustomerName { get; set; } // نام سپیدار مشتری
        public int? FinancialCustomerId { get; set; } // آیدی سپیدار مشتری

        [NotMapped]
        public string SelExpertRel { get; set; }


    }
}
