using System;
using System.Text;
using UnityEngine;

internal static class ItemTooltipTextFormatter
{
    private static readonly char[] _wordSeparators = { ' ', '\t', '\n', '\r' };

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

    public static string WrapDescription(string text, int wordsPerLine)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        if (wordsPerLine <= 0)
        {
            return text.Trim();
        }

        string[] words = text.Split(_wordSeparators, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new(text.Length);

        for (int i = 0; i < words.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(i % wordsPerLine == 0 ? '\n' : ' ');
            }

            builder.Append(words[i]);
        }

        return builder.ToString();
    }

    public static AmmoTooltipTextData FormatAmmoDetails(ItemData itemData, GameProjectSettings settings) => itemData != null && itemData.ItemType == ItemType.Ammo ? new(itemData, settings) : null;

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
                itemData.ModuleWeaponRecoilPercentModifier,
                itemData.ModuleWeaponDurabilityLossPercentModifier,
                itemData.ModuleMagazineCapacity,
                settings);
        }

        if (itemData.WeaponData == null)
        {
            return null;
        }

        return new ModuleTooltipTextData(
            WeaponModuleSupport.GetRecoilPercentModifier(item.InstalledModules),
            WeaponModuleSupport.GetDurabilityLossPercentModifier(item.InstalledModules),
            WeaponModuleSupport.GetInstalledMagazineCapacity(item.InstalledModules),
            settings);
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

        string text = $"{label}: {FormatSignedPercent(value)}";

        Color color = settings == null
            ? Color.white
            : value > 0f
                ? settings.NegativeAmmoModifierColor
                : settings.PositiveAmmoModifierColor;
        return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
    }

    internal static string FormatNumber(float value) => value.ToString("0.##");

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
            return $"<color=orange>{Mathf.RoundToInt(normalizedWeight * 1000f)}</color> г";
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
    public ModuleTooltipTextData(float recoilModifier, float durabilityLossModifier, int magazineCapacity, GameProjectSettings settings)
    {
        RecoilModifierText = ItemTooltipTextFormatter.FormatAmmoModifier("Отдача", recoilModifier, settings);
        DurabilityLossModifierText = ItemTooltipTextFormatter.FormatAmmoModifier("Износ", durabilityLossModifier, settings);
        MagazineSizeText = magazineCapacity > 0
            ? $"Размер магазина: {magazineCapacity}"
            : string.Empty;
    }

    public string RecoilModifierText { get; }
    public string DurabilityLossModifierText { get; }
    public string MagazineSizeText { get; }
}
