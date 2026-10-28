using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum CancellationReasonKind
    {
        [Display(Name = "Voluntary")] Voluntary = 1,
        [Display(Name = "Involuntary")] Involuntary = 2,
        [Display(Name = "Supplier Failure")] SupplierFailure = 3,
        [Display(Name = "No Show")] NoShow = 4
    }
}
