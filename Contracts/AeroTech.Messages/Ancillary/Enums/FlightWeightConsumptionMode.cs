using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum FlightWeightConsumptionMode
    {
        [Display(Name = "Fixed Kg Per Accepted Unit")] FixedKgPerAcceptedUnit = 1,
        [Display(Name = "Accepted Weight Kg")] AcceptedWeightKg = 2
    }
}
