using System.Collections.Generic;
using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Models.Taxation;

public sealed record TaxMemoryRow(TaxMemory Memory, string SandboxReadiness);
public sealed record TaxProductListModel(TaxProductPage Products, string Search, bool Unmapped);
