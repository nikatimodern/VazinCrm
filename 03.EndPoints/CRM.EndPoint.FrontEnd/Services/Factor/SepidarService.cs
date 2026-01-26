using CRM.Infrastructures.Sql;
using System.Linq;

namespace CRM.EndPoint.FrontEnd.Services.Factor
{
    public class SepidarService
    {
        private readonly Sepidar1ContextDb s1Db;

        public SepidarService(Sepidar1ContextDb S1db)
        {
            s1Db = S1db;
        }

        public  string getNoteFactor(int? refId)
        {
            var note= s1Db.ExtraData 
                        .Where(f => f.EntityTypeName == "SG.Sales.InvoiceManagement.Common.DsInvoice" && f.EntityRef == refId)
                        .FirstOrDefault().Note;
            return note ;
        }


    }
}
