using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Identity.Enums
{
    public enum VerificationPurpose
    {
        [Display(Name = "Registration")] Registration = 1,
        [Display(Name = "Login")] Login = 2,
        [Display(Name = "Password Reset")] PasswordReset = 3,
        [Display(Name = "Identifier Change")] IdentifierChange = 4
    }
}
