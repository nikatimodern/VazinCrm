using System;

namespace CRM.EndPoint.FrontEnd.Models {
    public class SelectOptionDto<T>
    {
        public T Value { get; set; }
        public string Text { get; set; }
    }

    public class SelectOptionIdDto {
        public Guid id { get; set; }
        public string text { get; set; }
        public bool? Target { get; set; } = false;
    }
}