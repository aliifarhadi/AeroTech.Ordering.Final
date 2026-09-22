namespace AeroTech.Framework.Presentation.Options
{
    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Authority { get; set; } = default!;

        public string Audience { get; set; } = default!;

        public bool RequireHttpsMetadata { get; set; } = true;
    }
}
