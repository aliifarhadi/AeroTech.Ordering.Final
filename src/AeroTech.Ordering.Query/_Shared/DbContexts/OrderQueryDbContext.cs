using AeroTech.Ordering.ReferenceData.Persistence;
using AeroTech.Ordering.ReferenceData.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.Query._Shared.DbContexts
{
    public sealed class OrderQueryDbContext : DbContext
    {
        public const string ReadModelSchema = "ReadModel";
        public const string MigrationsHistorySchema = "dbo";
        public const string MigrationsHistoryTable = "__QueriesMigrationHistory";

        public OrderQueryDbContext(DbContextOptions<OrderQueryDbContext> options) : base(options)
        {
        }

        public DbSet<CustomerReadModel> Customers => Set<CustomerReadModel>();

        public DbSet<CurrencyReadModel> Currencies => Set<CurrencyReadModel>();

        public DbSet<AirportReadModel> Airports => Set<AirportReadModel>();

        public DbSet<AirlineReadModel> Airlines => Set<AirlineReadModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(ReadModelSchema);
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderQueryDbContext).Assembly);

            MapReferenceReadModel<CustomerReadModel>(modelBuilder, "Customers");
            MapReferenceReadModel<CurrencyReadModel>(modelBuilder, "Currencies");
            MapReferenceReadModel<AirportReadModel>(modelBuilder, "Airports");
            MapReferenceReadModel<AirlineReadModel>(modelBuilder, "Airlines");
        }

        private static void MapReferenceReadModel<TEntity>(ModelBuilder modelBuilder, string table)
            where TEntity : class
            => modelBuilder.Entity<TEntity>(entity =>
            {
                entity.ToTable(table, ReferenceDbContext.Schema, builder => builder.ExcludeFromMigrations());
                entity.HasKey("Id");
            });

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
            configurationBuilder.Properties<string>().HaveMaxLength(256);
        }
    }
}
