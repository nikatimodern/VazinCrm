using System;
using CRM.Core.Domain.Accountancies.Repositories;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.Core.Domain.Sepidar.Parties.Entities;
using CRM.Core.Domain.Sepidar.Quotations.Entites;
using CRM.Core.Domain.Sepidar.Payments.Entites;
using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using CRM.Core.Domain.Sepidar.DLs.Entites;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using static CRM.Core.Domain.ExternalSoftwares.ExternalSoftwareIds;

namespace CRM.EndPoint.FrontEnd.Services.ExternalMappings
{
    public sealed partial class ExternalMappingService
    {
        public sealed class MappingConflictException : Exception
        {
            public MappingConflictException(string message) : base(message) { }
        }

        private const string ExternalUserEntityName = "User";


        public bool SupportsMapping(Guid softwareId) =>
            softwareId == Sepidar1SoftwareId || softwareId == Sepidar2SoftwareId;

        private readonly CRMContextDb db;
        private readonly Sepidar1ContextDb sdb;
        private readonly Sepidar2ContextDb s2Db;
        private readonly UserManager<AppUser> userManager;
        private readonly ILogger<ExternalMappingService> _logger;
        private readonly IDepositFinancialRepository depositRepository;

        public ExternalMappingService(
            CRMContextDb db,
            Sepidar1ContextDb sdb,
            Sepidar2ContextDb s2Db,
            UserManager<AppUser> userManager,
            ILogger<ExternalMappingService> logger, IDepositFinancialRepository depositRepository)
        {
            this.db = db;
            this.sdb = sdb;
            this.s2Db = s2Db;
            this.userManager = userManager;
            _logger = logger;
            this.depositRepository = depositRepository;
        }
    }
}
