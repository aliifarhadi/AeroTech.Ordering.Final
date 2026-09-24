namespace AeroTech.Ordering.Consumers.Jobs
{
    public sealed class ReservationDeadlineOptions
    {
        public const string SectionName = "ReservationDeadline";

        public int PollIntervalSeconds { get; set; }

        public int PollBatchSize { get; set; }
    }
}
