using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Messages.Ancillary.Enums;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class MealChoice : ValueObject
    {
        private MealChoice()
        {
        }

        public MealChoice(
            MealKind mealKind,
            string? mealCode,
            string? menuItemRef,
            string? dietaryCode,
            string? exclusiveMealFamilyCode)
        {
            MealKind = mealKind;
            MealCode = mealCode;
            MenuItemRef = menuItemRef;
            DietaryCode = dietaryCode;
            ExclusiveMealFamilyCode = exclusiveMealFamilyCode;
        }

        public MealKind MealKind { get; private set; }

        public string? MealCode { get; private set; }

        public string? MenuItemRef { get; private set; }

        public string? DietaryCode { get; private set; }

        public string? ExclusiveMealFamilyCode { get; private set; }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return MealKind;
            yield return MealCode;
            yield return MenuItemRef;
            yield return DietaryCode;
            yield return ExclusiveMealFamilyCode;
        }
    }
}
