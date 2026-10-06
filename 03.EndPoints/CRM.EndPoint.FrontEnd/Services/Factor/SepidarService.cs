using System.Collections.Generic;
using CRM.Infrastructures.Sql;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.EndPoint.FrontEnd.Models.Factor;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.Factor {
    public class SepidarService(Sepidar1ContextDb s1Db) {
        public async Task<Invoice> GetInvoiceAsync(int? invoiceId)
        {
            return await s1Db.vwInvoice
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InvoiceId == invoiceId);
        }

        public async Task<Invoice> GetInvoiceWithItemsAsync(int invoiceId)
        {
            return await s1Db.vwInvoice
                .AsNoTracking()
                .Include(x => x.InvoiceItems)
                .FirstOrDefaultAsync(x => x.InvoiceId == invoiceId);
        }

        public async Task<List<Invoice>> GetInvoicesByQuotationNumberAsync(int? quotationNumber)
        {
            return await s1Db.vwInvoice
                .AsNoTracking()
                .Where(x => x.QuotationNumber == quotationNumber)
                .ToListAsync();
        }

        public async Task<List<SepidarInvoiceDto>> GetInvoicesByQuotationNumbersAsync(int[] serialNumbers, CancellationToken ct = default)
        {
            try
            {
                return await s1Db.vwInvoice
                    .AsNoTracking()
                    .Where(x => x.QuotationNumber.HasValue && Enumerable.Contains(serialNumbers, x.QuotationNumber.Value))
                    .Select(x => new SepidarInvoiceDto {
                        QuotationNumber = x.QuotationNumber.Value,
                        CreationDate = x.CreationDate
                    })
                    .ToListAsync(cancellationToken: ct);
            }
            catch (SqlException)
            {
                return null;
            }
        }


        public async Task<SepidarInvoiceDto> GetInvoiceSummaryAsync(int invoiceId)
        {
            return await s1Db.vwInvoice
                .AsNoTracking()
                .Where(x => x.InvoiceId == invoiceId)
                .Select(x => new SepidarInvoiceDto {
                    InvoiceId = x.InvoiceId,
                    Number = x.Number,
                    Date = x.Date,
                    DateTXT = x.DateTXT,
                    CustomerPartyRef = x.CustomerPartyRef,
                    CustomerPartyName = x.CustomerPartyName
                })
                .FirstOrDefaultAsync();
        }


        public string GetNoteInvoice(int? refId)
        {
            var note = s1Db.ExtraData
                .Where(f => f.EntityTypeName == "SG.Sales.InvoiceManagement.Common.DsInvoice" && f.EntityRef == refId)
                .FirstOrDefault().Note;
            return note;
        }
    }
}