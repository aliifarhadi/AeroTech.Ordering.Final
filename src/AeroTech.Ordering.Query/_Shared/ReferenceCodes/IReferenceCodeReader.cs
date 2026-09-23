namespace AeroTech.Ordering.Query._Shared.ReferenceCodes
{
    public interface IReferenceCodeReader
    {
        Task<ReferenceCodes> ReadAsync(ReferenceCodeKeys keys, CancellationToken cancellationToken = default);
    }
}
