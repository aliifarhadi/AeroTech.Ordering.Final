using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;

namespace AeroTech.Ordering.Domain.OrderAggregate.Entities
{
    public sealed class OrderMealService : OrderAncillaryService
    {
        private OrderMealService()
        {
        }

        public OrderMealService(
            OrderAncillaryServiceArgs args,
            MealKind mealKind,
            string? mealCode,
            string? menuItemRef,
            string? dietaryCode,
            string? exclusiveMealFamilyCode)
            : base(args, AncillaryProfile.Meal)
        {
            EnsurePresent(VariantCode != AncillaryVariant.FreeSpecialMeal || !string.IsNullOrWhiteSpace(mealCode), nameof(MealCode));
            EnsurePresent(VariantCode != AncillaryVariant.PaidPreorderMeal || !string.IsNullOrWhiteSpace(menuItemRef), nameof(MenuItemRef));

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
    }
}
