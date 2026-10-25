using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql {
    public class Sepidar2ContextDb : SepidarContextDbBase {
        public Sepidar2ContextDb(DbContextOptions<Sepidar2ContextDb> op) : base(op) { }
    }
}