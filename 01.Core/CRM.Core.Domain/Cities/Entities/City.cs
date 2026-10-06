using CRM.Core.Domain.States.Entities;

namespace CRM.Core.Domain.Cities.Entities
{
    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string StateTXT { get; set; }
        public int StateID { get; set; }
        public State State { get; set; }




    }
}
