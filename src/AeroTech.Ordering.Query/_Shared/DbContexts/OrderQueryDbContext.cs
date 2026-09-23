using AeroTech.Ordering.Query.OrderAggregate.Models;
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

        public DbSet<OrderReadModel> Orders => Set<OrderReadModel>();

        public DbSet<OrderTravellerReadModel> OrderTravellers => Set<OrderTravellerReadModel>();

        public DbSet<OrderTravellerDocumentReadModel> OrderTravellerDocuments => Set<OrderTravellerDocumentReadModel>();

        public DbSet<OrderJourneyReadModel> OrderJourneys => Set<OrderJourneyReadModel>();

        public DbSet<OrderSegmentReadModel> OrderSegments => Set<OrderSegmentReadModel>();

        public DbSet<OrderItemReadModel> OrderItems => Set<OrderItemReadModel>();

        public DbSet<OrderServiceReadModel> OrderServices => Set<OrderServiceReadModel>();

        public DbSet<OrderPricingLineReadModel> OrderPricingLines => Set<OrderPricingLineReadModel>();

        public DbSet<OrderPricingAllocationReadModel> OrderPricingAllocations => Set<OrderPricingAllocationReadModel>();

        public DbSet<OrderContactReadModel> OrderContacts => Set<OrderContactReadModel>();

        public DbSet<OrderContactPointReadModel> OrderContactPoints => Set<OrderContactPointReadModel>();

        public DbSet<OrderRemarkReadModel> OrderRemarks => Set<OrderRemarkReadModel>();

        public DbSet<CustomerReadModel> Customers => Set<CustomerReadModel>();

        public DbSet<CurrencyReadModel> Currencies => Set<CurrencyReadModel>();

        public DbSet<AirportReadModel> Airports => Set<AirportReadModel>();

        public DbSet<AirlineReadModel> Airlines => Set<AirlineReadModel>();

        public DbSet<CountryReadModel> Countries => Set<CountryReadModel>();

        public DbSet<AirlineOfficeReadModel> AirlineOffices => Set<AirlineOfficeReadModel>();

        public DbSet<TravelAgencyOfficeReadModel> TravelAgencyOffices => Set<TravelAgencyOfficeReadModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(ReadModelSchema);
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderQueryDbContext).Assembly);

            MapReferenceReadModel<CustomerReadModel>(modelBuilder, "Customers");
            MapReferenceReadModel<CurrencyReadModel>(modelBuilder, "Currencies");
            MapReferenceReadModel<AirportReadModel>(modelBuilder, "Airports");
            MapReferenceReadModel<AirlineReadModel>(modelBuilder, "Airlines");
            MapReferenceReadModel<CountryReadModel>(modelBuilder, "Countries");
            MapReferenceReadModel<AirlineOfficeReadModel>(modelBuilder, "AirlineOffices");
            MapReferenceReadModel<TravelAgencyOfficeReadModel>(modelBuilder, "TravelAgencyOffices");
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
