using System.Collections.Generic;
using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Models.Taxation;

public sealed record TaxMemoryRow(TaxMemory Memory, string SandboxReadiness, bool CanCheckV1 = false);
public sealed record TaxProductListModel(TaxProductPage Products, string Search, bool Unmapped);
