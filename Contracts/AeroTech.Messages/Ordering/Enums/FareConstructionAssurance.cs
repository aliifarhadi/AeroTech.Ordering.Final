using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum FareConstructionAssurance
    {
        [Display(Name = "Source Provided")] SourceProvided = 1,
        [Display(Name = "Opaque")] Opaque = 2,
    }
}
