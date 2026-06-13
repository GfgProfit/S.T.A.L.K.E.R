public enum ItemType
{
    Misc = 0,
    Weapon = 1,
    Ammo = 2,
    Armor = 3,
    Helmet = 4,
    Detector = 5,
    Artifact = 6,
    Consumable = 7,
    Knife = 8,
    Pistol = 9,
    Quest = 10
}

public static class ItemTypeFormatter
{
    public static string ToRussianText(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon:
                return "Оружие";
            case ItemType.Ammo:
                return "Боеприпасы";
            case ItemType.Armor:
                return "Броня";
            case ItemType.Helmet:
                return "Шлем";
            case ItemType.Detector:
                return "Детектор";
            case ItemType.Artifact:
                return "Артефакт";
            case ItemType.Consumable:
                return "Расходник";
            case ItemType.Knife:
                return "Нож";
            case ItemType.Pistol:
                return "Пистолет";
            case ItemType.Quest:
                return "Квестовый предмет";
            case ItemType.Misc:
                return "Разное";
            default:
                return itemType.ToString();
        }
    }
}
