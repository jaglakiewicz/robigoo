/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

using Microsoft.EntityFrameworkCore;
using Server.Models;

#endregion

namespace Server.Data
{
    public class AppDbContext : DbContext
    {
        #region Declarations

        #endregion

        #region Constructor

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        #endregion

        #region Properties

        public DbSet<TodoItem> Todos { get; set; }
        public DbSet<Inspection> Inspections { get; set; }
        public DbSet<InspectionItem> InspectionItems { get; set; }
        public DbSet<User> Users { get; set; }

        #endregion

        #region Methods - Public

        #endregion

        #region Methods - Private

        #endregion
    }
}
