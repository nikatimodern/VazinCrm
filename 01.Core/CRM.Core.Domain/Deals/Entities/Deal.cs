using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Deals.Enums;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Reports.Entities;
using CRM.Framework.Domain;

namespace CRM.Core.Domain.Deals.Entities;

public class Deal : BaseEntity {
    public string Title { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string CustomerName { get; set; }
    public int? CustomerCode { get; set; }
    public string MobileNumber { get; set; }
    public DealStatusEnum Status { get; set; } = DealStatusEnum.باز;
    public int SerialNumber { get; set; }
    public int CountNum { get; set; } = 0;
    public byte CusSex { get; set; } = 1;
    public byte CusJuridical { get; set; }
    public string CusCompanyName { get; set; }
    public string CusMelliCode { get; set; }
    public string CusTel { get; set; }
    public string CusEghtesadiCode { get; set; }
    public string CusNumberRegister { get; set; }
    public string CusShenaseMeli { get; set; }
    public string CusAddress { get; set; }
    public string CusPostalCode { get; set; }
    public string CusProvince { get; set; }
    public string CusCity { get; set; }
    public Guid? CusWorkSenf { get; set; }
    public string CusWorkSenfTxt { get; set; }
    public string CusWorkSenfDescription { get; set; }
    public Guid? CusAcquaintanceWay { get; set; }
    public string CusAcquaintanceWayTxt { get; set; }

    public bool Suspend { get; set; }
    public DealFinancialStausEnum FinancialStatus { get; set; }

    
    public ICollection<Factor> Factors { get; set; }
    public ICollection<Accountancy> Accountancies { get; set; }
    public ICollection<PreFactor> PreFactors { get; set; }
    public ICollection<Report> Reports { get; set; } 
    public ICollection<OrderHead> OrderHeads { get; set; } 

    [NotMapped] public Counts Counts { get; set; } = new Counts();
    [NotMapped] public List<AppUser> UserIdsForSuspend { get; set; }
}

public class Counts {
    public int PreFactor { get; set; }
    public int Factor { get; set; }
    public int Report { get; set; }
    public int Accountancy { get; set; }
    public int OrderHead { get; set; }
}