internal static class ItemTypeTextFormatter
{
    public static string ToRussianText(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.Weapon => "Оружие",
            ItemType.Ammo => "Боеприпас",
            ItemType.Armor => "Броня",
            ItemType.Helmet => "Шлем",
            ItemType.Detector => "Детектор",
            ItemType.Artifact => "Артефакт",
            ItemType.Consumable => "Расходник",
            ItemType.Knife => "Нож",
            ItemType.Pistol => "Пистолет",
            ItemType.Quest => "Квестовый предмет",
            ItemType.Misc => "Разное",
            _ => itemType.ToString(),
        };
    }
}
