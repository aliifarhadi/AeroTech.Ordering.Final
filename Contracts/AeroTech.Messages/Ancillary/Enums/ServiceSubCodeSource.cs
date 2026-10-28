using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum ServiceSubCodeSource
    {
        [Display(Name = "Industry")] Industry = 1,
        [Display(Name = "Carrier Defined")] CarrierDefined = 2
    }
}
