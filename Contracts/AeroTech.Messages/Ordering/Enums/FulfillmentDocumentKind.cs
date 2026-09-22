using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum FulfillmentDocumentKind
    {
        [Display(Name = "None")] None = 1,
        [Display(Name = "ETKT")] Etkt = 2,
        [Display(Name = "EMD-A")] EmdA = 3,
        [Display(Name = "EMD-S")] EmdS = 4,
        [Display(Name = "Unresolved")] Unresolved = 5,
    }
}
