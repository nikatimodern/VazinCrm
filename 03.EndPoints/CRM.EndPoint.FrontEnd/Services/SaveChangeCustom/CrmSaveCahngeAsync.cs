using CRM.Core.Domain.Carizos.Entities;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using DocumentFormat.OpenXml.Spreadsheet;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Services.SaveChangeCustom
{
    public class CrmSaveCahngeAsync
    {
        private readonly CRMContextDb db;
        private readonly IHubContext<TaskHub> hub;

        public CrmSaveCahngeAsync(CRMContextDb db, IHubContext<TaskHub> hub)
        {
            this.db = db;
            this.hub = hub;
        }


        public async Task SaveChange()
        {
            var enti = db.ChangeTracker.Entries<Carizo>();//اگر جدول کاریزو ترک شده باشه یعنی مقدارش اضاف یا ویرایش شده باشه
            if (enti.Any())
            {
                await db.SaveChangesAsync();
                await hub.Clients.All.SendAsync("RefreshTasks");
            }
            else
            {
                await db.SaveChangesAsync();
            }
        }




    }
}
