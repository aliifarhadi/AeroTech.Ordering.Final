using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum ServiceLocationType
    {
        [Display(Name = "Airport")] Airport = 1,
        [Display(Name = "City")] City = 2,
        [Display(Name = "Country")] Country = 3
    }
}
