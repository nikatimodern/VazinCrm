using System.Collections.Generic;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxMemorySettings;
using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Models.Taxation;

public sealed record TaxMemoryRow(TaxMemory Memory, string Status);
public sealed record TaxMemoryAdminPage(IReadOnlyList<TaxMemoryRow> Memories, IReadOnlyList<TaxCertificateOption> Certificates);
public sealed record TaxMemorySettingsForm(TaxMemory Memory, IReadOnlyList<TaxCertificateOption> Certificates, string Heading);
public sealed record TaxProductListModel(TaxProductPage Products, string Search, bool Unmapped);
