using UnityEngine;

internal static class ItemTooltipTextFormatter
{
    public static string FormatType(ItemData itemData) => itemData == null ? string.Empty : $"Тип: {ItemTypeTextFormatter.ToRussianText(itemData.ItemType)}";

    public static string FormatWeight(int amount, float unitWeight, float totalWeight)
    {
        return amount > 1 ? $"Вес: {FormatWeightValue(totalWeight)} ({FormatWeightValue(unitWeight)})" : $"Вес: {FormatWeightValue(totalWeight)}";
    }

    public static string FormatDurability(float durabilityPercent, Color durabilityColor)
    {
        string percentColor = ColorUtility.ToHtmlStringRGBA(durabilityColor);
        return $"Прочность: <color=#{percentColor}>{durabilityPercent:00.0}%</color>";
    }

    public static AmmoTooltipTextData FormatAmmoDetails(ItemData itemData, GameProjectSettings settings) => itemData != null && itemData.ItemType == ItemType.Ammo ? new(itemData, settings) : null;

    public static string FormatArmorRecoilReduction(ItemData itemData, GameProjectSettings settings)
    {
        float reductionPercent = itemData == null ? 0f : itemData.ArmorRecoilReductionPercent;

        if (Mathf.Approximately(reductionPercent, 0f))
        {
            return string.Empty;
        }

        return $"Гашение отдачи: {FormatColoredValue($"{FormatNumber(reductionPercent)}%", reductionPercent, false, settings)}";
    }

    public static ModuleTooltipTextData FormatModuleDetails(ItemTooltipData item, GameProjectSettings settings)
    {
        ItemData itemData = item.ItemData;

        if (itemData == null)
        {
            return null;
        }

        if (itemData.ItemType == ItemType.Module)
        {
            return new ModuleTooltipTextData(
                FormatAmmoModifier("Отдача", itemData.ModuleWeaponRecoilPercentModifier, settings),
                FormatAmmoModifier("Износ", itemData.ModuleWeaponDurabilityLossPercentModifier, settings),
                FormatMagazineSize(itemData.ModuleMagazineCapacity, itemData.ModuleMagazineCapacity, settings),
                FormatAccuracy(itemData.ModuleAccuracyMinutesOfAngleModifier, itemData.ModuleAccuracyMinutesOfAngleModifier, true, settings),
                FormatErgonomics(itemData.ModuleErgonomicsModifier, itemData.ModuleErgonomicsModifier, true, settings));
        }

        if (itemData.WeaponData == null)
        {
            return null;
        }

        int magazineCapacity = WeaponModuleSupport.GetInstalledMagazineCapacity(item.InstalledModules);
        float baseAccuracy = itemData.WeaponData.AccuracyMinutesOfAngle;
        float effectiveAccuracy = WeaponModuleSupport.GetAccuracyMinutesOfAngle(itemData.WeaponData, item.InstalledModules);
        float baseErgonomics = itemData.WeaponData.BaseErgonomics;
        float effectiveErgonomics = WeaponModuleSupport.GetErgonomics(itemData.WeaponData, item.InstalledModules);

        return new ModuleTooltipTextData(
            FormatAmmoModifier("Отдача", WeaponModuleSupport.GetRecoilPercentModifier(item.InstalledModules), settings),
            FormatAmmoModifier("Износ", WeaponModuleSupport.GetDurabilityLossPercentModifier(item.InstalledModules), settings),
            FormatMagazineSize(magazineCapacity, magazineCapacity > 0 ? magazineCapacity - itemData.WeaponData.MagazineCapacity : 0f, settings),
            FormatAccuracy(effectiveAccuracy, effectiveAccuracy - baseAccuracy, false, settings),
            FormatErgonomics(effectiveErgonomics, effectiveErgonomics - baseErgonomics, false, settings));
    }

    internal static string FormatArmorPenetrationClassification(float armorPenetration, int armorClass, GameProjectSettings settings)
    {
        float classMinimum = (armorClass - 1) * 10f + 1f;
        float valueWithinClass = armorPenetration - classMinimum;

        if (valueWithinClass < 2f)
        {
            return FormatClassification(AmmoClassification.VeryLow, settings);
        }

        if (valueWithinClass < 4f)
        {
            return FormatClassification(AmmoClassification.Low, settings);
        }

        if (valueWithinClass < 6f)
        {
            return FormatClassification(AmmoClassification.Medium, settings);
        }

        if (valueWithinClass < 8f)
        {
            return FormatClassification(AmmoClassification.High, settings);
        }

        return FormatClassification(AmmoClassification.VeryHigh, settings);
    }

    internal static string FormatRicochetChanceClassification(float ricochetChance, GameProjectSettings settings)
    {
        if (ricochetChance <= 0.2f)
        {
            return FormatClassification(AmmoClassification.VeryLow, settings);
        }

        if (ricochetChance <= 0.4f)
        {
            return FormatClassification(AmmoClassification.Low, settings);
        }

        if (ricochetChance <= 0.6f)
        {
            return FormatClassification(AmmoClassification.Medium, settings);
        }

        if (ricochetChance <= 0.8f)
        {
            return FormatClassification(AmmoClassification.High, settings);
        }

        return FormatClassification(AmmoClassification.VeryHigh, settings);
    }

    internal static string FormatSignedPercent(float value)
    {
        string sign = value >= 0f ? "+" : string.Empty;
        return $"{sign}{FormatNumber(value)}%";
    }

    internal static string FormatAmmoModifier(string label, float value, GameProjectSettings settings)
    {
        if (Mathf.Approximately(value, 0f))
        {
            return string.Empty;
        }

        return $"{label}: {FormatColoredValue(FormatSignedPercent(value), value, true, settings)}";
    }

    internal static string FormatMagazineSize(int magazineCapacity, float capacityModifier, GameProjectSettings settings)
    {
        return magazineCapacity > 0
            ? $"Размер магазина: {FormatColoredValue(magazineCapacity.ToString(), capacityModifier, false, settings)}"
            : string.Empty;
    }

    internal static string FormatAccuracy(float accuracyMinutesOfAngle, float accuracyModifier, bool signed, GameProjectSettings settings)
    {
        if (signed && Mathf.Approximately(accuracyMinutesOfAngle, 0f))
        {
            return string.Empty;
        }

        string value = signed ? FormatSignedNumber(accuracyMinutesOfAngle) : FormatNumber(accuracyMinutesOfAngle);
        string valueWithUnit = $"{value} МОА";
        return $"Точность: {FormatColoredValue(valueWithUnit, accuracyModifier, true, settings)}";
    }

    internal static string FormatErgonomics(float ergonomics, float ergonomicsModifier, bool signed, GameProjectSettings settings)
    {
        if (signed && Mathf.Approximately(ergonomics, 0f))
        {
            return string.Empty;
        }

        string value = signed ? FormatSignedNumber(ergonomics) : FormatNumber(ergonomics);
        return $"Эргономика: {FormatColoredValue(value, ergonomicsModifier, false, settings)}";
    }

    internal static string FormatNumber(float value) => value.ToString("0.##");

    private static string FormatSignedNumber(float value)
    {
        string sign = value >= 0f ? "+" : string.Empty;
        return $"{sign}{FormatNumber(value)}";
    }

    private static string FormatColoredValue(string value, float modifier, bool lowerIsBetter, GameProjectSettings settings)
    {
        Color color = Color.white;

        if (settings != null && Mathf.Approximately(modifier, 0f) == false)
        {
            bool isBeneficial = lowerIsBetter ? modifier < 0f : modifier > 0f;
            color = isBeneficial ? settings.PositiveAmmoModifierColor : settings.NegativeAmmoModifierColor;
        }

        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{value}</color>";
    }

    private static string FormatClassification(AmmoClassification classification, GameProjectSettings settings)
    {
        string text = classification switch
        {
            AmmoClassification.VeryLow => "Очень низкий",
            AmmoClassification.Low => "Низкий",
            AmmoClassification.Medium => "Средний",
            AmmoClassification.High => "Высокий",
            AmmoClassification.VeryHigh => "Очень высокий",
            _ => string.Empty,
        };

        Color color = settings == null
            ? Color.white
            : classification switch
            {
                AmmoClassification.VeryLow => settings.VeryLowAmmoClassificationColor,
                AmmoClassification.Low => settings.LowAmmoClassificationColor,
                AmmoClassification.Medium => settings.MediumAmmoClassificationColor,
                AmmoClassification.High => settings.HighAmmoClassificationColor,
                AmmoClassification.VeryHigh => settings.VeryHighAmmoClassificationColor,
                _ => Color.white,
            };

        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
    }

    private static string FormatWeightValue(float weight)
    {
        float normalizedWeight = Mathf.Max(0f, weight);

        if (Mathf.Approximately(normalizedWeight, 1f))
        {
            return "<color=orange>1</color> кг";
        }

        if (normalizedWeight < 1f)
        {
            return $"<color=orange>{normalizedWeight * 1000f}</color> г";
        }

        return $"<color=orange>{normalizedWeight:0.000}</color> кг";
    }

    private enum AmmoClassification
    {
        VeryLow = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        VeryHigh = 4,
    }
}

internal sealed class AmmoTooltipTextData
{
    private const int ARMOR_CLASS_COUNT = 6;

    public AmmoTooltipTextData(ItemData ammoData, GameProjectSettings settings)
    {
        FleshDamageText = $"Урон по живым тканям: {ItemTooltipTextFormatter.FormatNumber(ammoData.AmmoFleshDamage)}";
        ArmorPenetrationText = $"Бронепробитие: {ItemTooltipTextFormatter.FormatNumber(ammoData.AmmoArmorPenetration)}";
        BulletSpeedText = $"Скорость полёта пули: {ItemTooltipTextFormatter.FormatNumber(ammoData.AmmoBulletVelocityMetersPerSecondFallback)} м/с";
        BulletMassText = $"Масса пули: {ItemTooltipTextFormatter.FormatNumber(ammoData.AmmoBulletMassGrams)} гр";
        BulletDiameterText = $"Диаметр пули: {ItemTooltipTextFormatter.FormatNumber(ammoData.AmmoBulletDiameterMillimeters)} мм";
        RicochetChanceText = $"Шанс рикошета: {ItemTooltipTextFormatter.FormatRicochetChanceClassification(ammoData.AmmoRicochetChance, settings)}";
        RecoilModifierText = ItemTooltipTextFormatter.FormatAmmoModifier("Отдача", ammoData.AmmoWeaponRecoilPercentModifier, settings);
        DurabilityLossModifierText = ItemTooltipTextFormatter.FormatAmmoModifier("Износ", ammoData.AmmoWeaponDurabilityLossPercentModifier, settings);
        ArmorClassTexts = new string[ARMOR_CLASS_COUNT];

        for (int i = 0; i < ArmorClassTexts.Length; i++)
        {
            ArmorClassTexts[i] = ItemTooltipTextFormatter.FormatArmorPenetrationClassification(ammoData.AmmoArmorPenetration, i + 1, settings);
        }
    }

    public string FleshDamageText { get; }
    public string ArmorPenetrationText { get; }
    public string BulletSpeedText { get; }
    public string BulletMassText { get; }
    public string BulletDiameterText { get; }
    public string RicochetChanceText { get; }
    public string RecoilModifierText { get; }
    public string DurabilityLossModifierText { get; }
    public string[] ArmorClassTexts { get; }
}

internal sealed class ModuleTooltipTextData
{
    public ModuleTooltipTextData(string recoilModifierText, string durabilityLossModifierText, string magazineSizeText, string accuracyText, string ergonomicsText)
    {
        RecoilModifierText = recoilModifierText;
        DurabilityLossModifierText = durabilityLossModifierText;
        MagazineSizeText = magazineSizeText;
        AccuracyText = accuracyText;
        ErgonomicsText = ergonomicsText;
    }

    public string RecoilModifierText { get; }
    public string DurabilityLossModifierText { get; }
    public string MagazineSizeText { get; }
    public string AccuracyText { get; }
    public string ErgonomicsText { get; }
}
