using CRM.Core.Domain.Cities.Entities;
using System.Collections.Generic;

namespace CRM.Core.Domain.States.Entities
{
    public class State
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<City> Cities { get; set; }

    }
}
