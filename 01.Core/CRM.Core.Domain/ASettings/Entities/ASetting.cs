using CRM.Framework.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRM.Core.Domain.ASettings.Entities
{
    public class ASetting
    {
        public Guid Id { get; set; }
        public bool AttAcceptHanding { get; set; } = false;
        public bool AttPfPayment { get; set; } = false;
        public bool AttFPayment { get; set; } = false;
        public bool SmallOffice { get; set; } = false;
        public bool? Sepidar { get; set; } = false;
        public bool? CrmFactors { get; set; } = false;
        public bool? FactorTowStep { get; set; } = false;
        public SetAmountRoleEnum SetAmountByRoleType { get; set; } = SetAmountRoleEnum.فروش;
        public bool? FinancialConfirm { get; set; } = false; // حذف شود
        public bool? AccountancyRequired { get; set; } = false;
        public bool CustomerCanChooseInvoiceType { get; set; } = false;

        public bool SetSupplyAmountByTamin { get; set; } = false;
        public bool SetSupplyByTamin { get; set; } = false;

    }

    public enum SetAmountRoleEnum {
        فروش = 1,
        تامین = 2,
        فروش_تامین = 3
    }
}
