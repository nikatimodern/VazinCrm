using CRM.Framework.Domain;
using System;

namespace CRM.Core.Domain.Carizos.Entities
{
    public class TaskUser : BaseEntity
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public Guid TaskId { get; set; }
        public bool completed { get; set; } = false;
    }




}
