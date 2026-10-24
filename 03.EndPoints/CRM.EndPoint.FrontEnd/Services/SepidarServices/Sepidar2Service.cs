using CRM.Infrastructures.Sql;

namespace CRM.EndPoint.FrontEnd.Services.SepidarServices;

public class Sepidar2Service : SepidarService {
    public Sepidar2Service(Sepidar2ContextDb sdb) : base(sdb) { }
}