using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum SelectionIdKind
    {
        [Display(Name = "Reference")] Reference = 1,
        [Display(Name = "Direct")] Direct = 2
    }
}
