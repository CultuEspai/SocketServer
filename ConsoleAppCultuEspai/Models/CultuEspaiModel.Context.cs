﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Este código se generó a partir de una plantilla.
//
//     Los cambios manuales en este archivo pueden causar un comportamiento inesperado de la aplicación.
//     Los cambios manuales en este archivo se sobrescribirán si se regenera el código.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ConsoleAppCultuEspai.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class espaiCulturalEntities : DbContext
    {
        public espaiCulturalEntities()
            : base("name=espaiCulturalEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<CaracteristiquesSales> CaracteristiquesSales { get; set; }
        public virtual DbSet<Entrades> Entrades { get; set; }
        public virtual DbSet<Esdeveniments> Esdeveniments { get; set; }
        public virtual DbSet<Sales> Sales { get; set; }
        public virtual DbSet<Usuaris> Usuaris { get; set; }
        public virtual DbSet<Xats> Xats { get; set; }
    }
}
