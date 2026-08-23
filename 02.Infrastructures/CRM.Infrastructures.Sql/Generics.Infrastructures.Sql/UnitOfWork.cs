using CRM.Core.Domain;
using CRM.Infrastructures.Sql;

namespace Generics.Infrastructures.Sql
{

    public class UnitOfWork : IUnitOfWork
    {
        private readonly CRMContextDb db;

        public UnitOfWork(CRMContextDb db)
        {
            this.db = db;
        }





    }
}
