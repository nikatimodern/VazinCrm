using System;
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
using CRM.Core.Domain.ASettings.Enums;
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

namespace CRM.EndPoint.FrontEnd.Services.ExternalMappings
{
    public sealed partial class ExternalMappingService
    {
        public sealed class MappingConflictException : Exception
        {
            public MappingConflictException(string message) : base(message) { }
        }

        private static readonly Guid Sepidar1SoftwareId =
            Guid.Parse("2f3d8024-6c2c-491f-c921-08df26b00138");
        private static readonly Guid Sepidar2SoftwareId =
            Guid.Parse("d6c3e8a7-1435-47d5-0489-08df26c5e2ca");

        private readonly CRMContextDb db;
        private readonly Sepidar1ContextDb sdb;
        private readonly Sepidar2ContextDb s2Db;
        private readonly UserManager<AppUser> userManager;
        private readonly ILogger<ExternalMappingService> _logger;

        public ExternalMappingService(
            CRMContextDb db,
            Sepidar1ContextDb sdb,
            Sepidar2ContextDb s2Db,
            UserManager<AppUser> userManager,
            ILogger<ExternalMappingService> logger)
        {
            this.db = db;
            this.sdb = sdb;
            this.s2Db = s2Db;
            this.userManager = userManager;
            _logger = logger;
        }
    }
}
