using CRM.Core.Domain.Taxation;
using Microsoft.AspNetCore.DataProtection;

namespace CRM.EndPoint.FrontEnd.Composition;

internal sealed class TaxPreviewProtector(IDataProtectionProvider provider) : ITaxPreviewProtector
{
    private readonly IDataProtector protector = provider.CreateProtector("Taxation.InvoicePreview.v1");
    public string Protect(string preview) => protector.Protect(preview);
    public string Unprotect(string preview) => protector.Unprotect(preview);
}
