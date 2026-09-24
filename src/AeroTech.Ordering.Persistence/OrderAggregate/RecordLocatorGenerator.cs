using System.Security.Cryptography;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;

namespace AeroTech.Ordering.Persistence.OrderAggregate
{
    public sealed class RecordLocatorGenerator : IRecordLocatorGenerator
    {
        private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        private const int Length = 6;

        public string Generate() => RandomNumberGenerator.GetString(Alphabet, Length);
    }
}
