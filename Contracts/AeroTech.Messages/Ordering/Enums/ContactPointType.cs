using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum ContactPointType
    {
        [Display(Name = "Email")] Email = 1,

        [Display(Name = "Phone")] Phone = 2,

        [Display(Name = "SMS")] Sms = 3,

        [Display(Name = "WhatsApp")] WhatsApp = 4,

        [Display(Name = "Telegram")] Telegram = 5
    }
}
