using AeroTech.Ordering.ReferenceData.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.ReferenceData.Persistence
{
    public sealed class ReferenceDbContext : DbContext
    {
        public const string Schema = "ReferenceData";
        public const string MigrationsHistorySchema = "dbo";
        public const string MigrationsHistoryTable = "__ReferenceDataMigrationHistory";

        public ReferenceDbContext(DbContextOptions<ReferenceDbContext> options) : base(options)
        {
        }

        public DbSet<CurrencyReadModel> Currencies => Set<CurrencyReadModel>();
        public DbSet<AirlineReadModel> Airlines => Set<AirlineReadModel>();
        public DbSet<CityReadModel> Cities => Set<CityReadModel>();
        public DbSet<CountryReadModel> Countries => Set<CountryReadModel>();
        public DbSet<AirportReadModel> Airports => Set<AirportReadModel>();
        public DbSet<AirportTerminalReadModel> AirportTerminals => Set<AirportTerminalReadModel>();
        public DbSet<CustomerReadModel> Customers => Set<CustomerReadModel>();
        public DbSet<OperatorSettingsReadModel> OperatorSettings => Set<OperatorSettingsReadModel>();
        public DbSet<AirlineOfficeReadModel> AirlineOffices => Set<AirlineOfficeReadModel>();
        public DbSet<TravelAgencyReadModel> TravelAgencies => Set<TravelAgencyReadModel>();
        public DbSet<TravelAgencyOfficeReadModel> TravelAgencyOffices => Set<TravelAgencyOfficeReadModel>();
        public DbSet<ReferenceSyncState> SyncStates => Set<ReferenceSyncState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);

            modelBuilder.Entity<CurrencyReadModel>(entity =>
            {
                entity.ToTable("Currencies");
                entity.HasKey(currency => currency.Id);
                entity.Property(currency => currency.Id).ValueGeneratedNever();
                entity.Property(currency => currency.Code).HasMaxLength(8);
            });

            modelBuilder.Entity<AirlineReadModel>(entity =>
            {
                entity.ToTable("Airlines");
                entity.HasKey(airline => airline.Id);
                entity.Property(airline => airline.Id).ValueGeneratedNever();
                entity.Property(airline => airline.IataCode).HasMaxLength(8);
                entity.HasIndex(airline => airline.IataCode);
            });

            modelBuilder.Entity<CityReadModel>(entity =>
            {
                entity.ToTable("Cities");
                entity.HasKey(city => city.Id);
                entity.Property(city => city.Id).ValueGeneratedNever();
                entity.Property(city => city.IataCode).HasMaxLength(8);
                entity.HasIndex(city => city.IataCode);
            });

            modelBuilder.Entity<CountryReadModel>(entity =>
            {
                entity.ToTable("Countries");
                entity.HasKey(country => country.Id);
                entity.Property(country => country.Id).ValueGeneratedNever();
                entity.Property(country => country.Alpha2Code).HasMaxLength(2).IsRequired();
                entity.Property(country => country.Alpha3Code).HasMaxLength(3);
                entity.Property(country => country.PhoneCode).HasMaxLength(8);
                entity.HasIndex(country => country.Alpha2Code).IsUnique();
                entity.HasIndex(country => country.Alpha3Code);
            });

            modelBuilder.Entity<AirportReadModel>(entity =>
            {
                entity.ToTable("Airports");
                entity.HasKey(airport => airport.Id);
                entity.Property(airport => airport.Id).ValueGeneratedNever();
                entity.Property(airport => airport.IataCode).HasMaxLength(8);
                entity.Property(airport => airport.IkaoCode).HasMaxLength(8);
                entity.HasIndex(airport => airport.IataCode);
            });

            modelBuilder.Entity<AirportTerminalReadModel>(entity =>
            {
                entity.ToTable("AirportTerminals");
                entity.HasKey(terminal => terminal.Id);
                entity.Property(terminal => terminal.Id).ValueGeneratedNever();
                entity.Property(terminal => terminal.Number).HasMaxLength(16);
                entity.HasIndex(terminal => terminal.AirportId);
            });

            modelBuilder.Entity<CustomerReadModel>(entity =>
            {
                entity.ToTable("Customers");
                entity.HasKey(customer => customer.Id);
                entity.Property(customer => customer.Id).ValueGeneratedNever();
                entity.Property(customer => customer.CustomerNumber).HasMaxLength(64).IsRequired();
                entity.Property(customer => customer.SubjectName).HasMaxLength(200).IsRequired();
                entity.Property(customer => customer.PreferredLanguageCode).HasMaxLength(10);
                entity.Property(customer => customer.SuspensionReasonCode).HasMaxLength(50);
                entity.Property(customer => customer.ClosureReasonCode).HasMaxLength(50);
                entity.HasIndex(customer => customer.CustomerNumber);
                entity.HasIndex(customer => customer.TravelAgencyId);
            });

            modelBuilder.Entity<OperatorSettingsReadModel>(entity =>
            {
                entity.ToTable("OperatorSettings");
                entity.HasKey(settings => settings.Id);
                entity.Property(settings => settings.Id).ValueGeneratedNever();
                entity.Property(settings => settings.ScopeKey).HasMaxLength(64).IsRequired();
                entity.Property(settings => settings.DefaultLanguageCode).HasMaxLength(10);
                entity.Property(settings => settings.DefaultTimeZoneId).HasMaxLength(100);
                entity.HasIndex(settings => settings.ScopeKey).IsUnique();
            });

            modelBuilder.Entity<AirlineOfficeReadModel>(entity =>
            {
                entity.ToTable("AirlineOffices");
                entity.HasKey(office => office.Id);
                entity.Property(office => office.Id).ValueGeneratedNever();
                entity.Property(office => office.Code).HasMaxLength(50).IsRequired();
                entity.Property(office => office.Name).HasMaxLength(150).IsRequired();
                entity.Property(office => office.OrganisationUnitName).HasMaxLength(150);
                entity.Property(office => office.ParentOfficeName).HasMaxLength(150);
                entity.Property(office => office.LegalEntityLegalName).HasMaxLength(200).IsRequired();
                entity.Property(office => office.TimeZoneId).HasMaxLength(100);
                entity.HasIndex(office => office.Code);
                entity.HasIndex(office => office.ParentOfficeId);
            });

            modelBuilder.Entity<TravelAgencyReadModel>(entity =>
            {
                entity.ToTable("TravelAgencies");
                entity.HasKey(agency => agency.Id);
                entity.Property(agency => agency.Id).ValueGeneratedNever();
                entity.Property(agency => agency.Code).HasMaxLength(50).IsRequired();
                entity.Property(agency => agency.LegalName).HasMaxLength(200).IsRequired();
                entity.Property(agency => agency.TradingName).HasMaxLength(200);
                entity.Property(agency => agency.ParentAgencyLegalName).HasMaxLength(200);
                entity.Property(agency => agency.PreferredLanguageCode).HasMaxLength(10);
                entity.Property(agency => agency.TerminationReasonCode).HasMaxLength(50);
                entity.Property(agency => agency.PrimaryAccreditationTypeCode).HasMaxLength(50);
                entity.Property(agency => agency.PrimaryAccreditationIdentifier).HasMaxLength(150);
                entity.HasIndex(agency => agency.Code);
                entity.HasIndex(agency => agency.ParentAgencyId);
            });

            modelBuilder.Entity<TravelAgencyOfficeReadModel>(entity =>
            {
                entity.ToTable("TravelAgencyOffices");
                entity.HasKey(office => office.Id);
                entity.Property(office => office.Id).ValueGeneratedNever();
                entity.Property(office => office.Code).HasMaxLength(50).IsRequired();
                entity.Property(office => office.Name).HasMaxLength(150).IsRequired();
                entity.Property(office => office.TravelAgencyLegalName).HasMaxLength(200).IsRequired();
                entity.Property(office => office.ParentOfficeName).HasMaxLength(150);
                entity.Property(office => office.TimeZoneId).HasMaxLength(100);
                entity.HasIndex(office => office.Code);
                entity.HasIndex(office => office.TravelAgencyId);
                entity.HasIndex(office => office.ParentOfficeId);
            });

            modelBuilder.Entity<ReferenceSyncState>(entity =>
            {
                entity.ToTable("ReferenceDataSyncStates");
                entity.HasKey(state => state.Id);
                entity.Property(state => state.Id).HasMaxLength(64).ValueGeneratedNever();
            });

            base.OnModelCreating(modelBuilder);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<string>().HaveMaxLength(256);
        }
    }
}
