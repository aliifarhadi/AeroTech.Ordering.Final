using AeroTech.Framework.Core.Domain.Extensions;

namespace AeroTech.Ordering.Query._Shared.Enums
{
    public sealed record EnumValueDto(int Value, string Name, string Title)
    {
        public static EnumValueDto Of<TEnum>(TEnum value) where TEnum : struct, Enum
        {
            var name = value.ToString();
            var title = ((Enum)(object)value).GetDisplayName();

            return new EnumValueDto(Convert.ToInt32(value), name, string.IsNullOrWhiteSpace(title) ? name : title);
        }

        public static EnumValueDto? OfNullable<TEnum>(TEnum? value) where TEnum : struct, Enum
            => value is null ? null : Of(value.Value);
    }
}
