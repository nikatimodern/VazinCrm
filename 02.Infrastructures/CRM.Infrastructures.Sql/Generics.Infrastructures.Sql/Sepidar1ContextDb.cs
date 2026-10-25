using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql {
    public class Sepidar1ContextDb : SepidarContextDbBase {
        public Sepidar1ContextDb(DbContextOptions<Sepidar1ContextDb> op) : base(op)
        {
            
        }
    }
}