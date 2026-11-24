using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Models.Taxation;

public sealed record TaxationPreviewModel(InvoicePreview Preview, TaxDispatchDetails Dispatch, bool CanContactSandbox,
    string Error = null);
