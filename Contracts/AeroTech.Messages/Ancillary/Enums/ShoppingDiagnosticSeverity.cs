using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum ShoppingDiagnosticSeverity
    {
        [Display(Name = "Info")] Info = 1,
        [Display(Name = "Warning")] Warning = 2,
        [Display(Name = "Blocker")] Blocker = 3
    }
}
