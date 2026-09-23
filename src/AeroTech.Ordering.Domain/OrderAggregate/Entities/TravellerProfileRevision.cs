using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class TravellerProfileRevision : Entity<long>
    {
        private TravellerProfileRevision()
        {
        }

        public TravellerProfileRevision(
            long id,
            long travellerId,
            string givenName,
            string? surname,
            bool noSurname,
            DateOnly? dateOfBirth,
            Gender? gender,
            int? nationalityId,
            int? countryOfResidenceId,
            long createdByChangeId,
            DateTimeOffset createdAt)
        {
            if (string.IsNullOrWhiteSpace(givenName))
                throw ExceptionFactory.FirstNameIsRequired();

            if (string.IsNullOrWhiteSpace(surname) && !noSurname)
                throw ExceptionFactory.SurnameIsRequired();

            Id = id;
            TravellerId = travellerId;
            GivenName = givenName.Trim().ToUpperInvariant();
            Surname = noSurname ? null : surname!.Trim().ToUpperInvariant();
            NoSurname = noSurname;
            DateOfBirth = dateOfBirth;
            Gender = gender;
            NationalityId = nationalityId;
            CountryOfResidenceId = countryOfResidenceId;
            CreatedByChangeId = createdByChangeId;
            CreatedAt = createdAt;
        }

        public long TravellerId { get; private set; }

        public string GivenName { get; private set; } = default!;

        public string? Surname { get; private set; }

        public bool NoSurname { get; private set; }

        public DateOnly? DateOfBirth { get; private set; }

        public Gender? Gender { get; private set; }

        public int? NationalityId { get; private set; }

        public int? CountryOfResidenceId { get; private set; }

        public long CreatedByChangeId { get; private set; }

        public long? SupersededByChangeId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }
    }
}
