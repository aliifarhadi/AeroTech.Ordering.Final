namespace AeroTech.Ordering.Domain.ElectronicTicketAggregate
{
    public static class LocalVoidPolicy
    {
        public static DateTimeOffset? DeadlineFor(DateTimeOffset issuedAt, string? issuingOfficeTimeZoneId)
        {
            if (string.IsNullOrWhiteSpace(issuingOfficeTimeZoneId)
                || !TimeZoneInfo.TryFindSystemTimeZoneById(issuingOfficeTimeZoneId, out var timeZone))
                return null;

            var nextLocalDay = TimeZoneInfo.ConvertTime(issuedAt, timeZone).Date.AddDays(1);

            return new DateTimeOffset(nextLocalDay, OffsetAtStartOf(nextLocalDay, timeZone));
        }

        private static TimeSpan OffsetAtStartOf(DateTime localMidnight, TimeZoneInfo timeZone)
        {
            if (timeZone.IsAmbiguousTime(localMidnight))
                return timeZone.GetAmbiguousTimeOffsets(localMidnight).Max();

            return timeZone.IsInvalidTime(localMidnight)
                ? timeZone.GetUtcOffset(localMidnight.AddTicks(-1))
                : timeZone.GetUtcOffset(localMidnight);
        }
    }
}
