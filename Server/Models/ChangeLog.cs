using System;
using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class ChangeLog
    {
        [Key]
        public long Id { get; set; }

        public string EntityName { get; set; } = string.Empty;
        
        public string EntityId { get; set; } = string.Empty;

        public string Changes { get; set; } = string.Empty;

        public string Who { get; set; } = string.Empty;

        public DateTime When { get; set; } = DateTime.UtcNow;
    }
}
