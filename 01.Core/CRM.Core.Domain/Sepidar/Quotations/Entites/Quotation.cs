namespace CRM.Core.Domain.Sepidar.Quotations.Entites
{
    public class Quotation
    {
        public int QuotationId { get; set; }
        public int CustomerPartyRef { get; set; }
        public string CustomerPartyName { get; set; }
        public int Number { get; set; }
        public decimal Price { get; set; }
        public decimal Tax { get; set; }
        public decimal NetPrice { get; set; }



    }
}
