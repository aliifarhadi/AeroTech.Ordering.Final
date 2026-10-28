using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum SelectionFieldType
    {
        [Display(Name = "Integer")] Integer = 1,
        [Display(Name = "Decimal")] Decimal = 2,
        [Display(Name = "Text")] Text = 3,
        [Display(Name = "Boolean")] Boolean = 4,
        [Display(Name = "Code")] Code = 5,
        [Display(Name = "Code Set")] CodeSet = 6,
        [Display(Name = "Reference")] Reference = 7,
        [Display(Name = "Dimensions Cm")] DimensionsCm = 8,
        [Display(Name = "Date Time With Offset")] DateTimeWithOffset = 9
    }
}
