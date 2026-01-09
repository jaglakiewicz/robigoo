/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

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
