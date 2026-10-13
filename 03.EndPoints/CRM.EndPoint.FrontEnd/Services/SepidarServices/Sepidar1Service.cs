using CRM.Infrastructures.Sql;

namespace CRM.EndPoint.FrontEnd.Services.SepidarServices;

public class Sepidar1Service : SepidarService {
    public Sepidar1Service(Sepidar1ContextDb sdb, CRMContextDb db) : base(sdb, db)
    {
        
    }
}