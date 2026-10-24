using System;
using System.Collections.Generic;
using UnityEngine;

namespace GardenMerger.Core
{
    /// <summary>
    /// Полный перечень всех 14 цветов в колористической системе Garden Merger.
    /// </summary>
    public enum FlowerColor
    {
        White = 0,    // Белый (Единственный базовый исток)
        Red = 1,      // Красный (Утепление белого)
        Blue = 2,     // Синий (Холодание белого)
        Pink = 3,     // Розовый
        SkyBlue = 4,  // Голубой
        Purple = 5,   // Фиолетовый
        Coral = 6,    // Коралловый
        Lavender = 7, // Лавандовый
        Lilac = 8,    // Лиловый
        Magenta = 9,  // Маджента
        Indigo = 10,  // Индиго
        IceBlue = 11, // Ледяной
        Ruby = 12,    // Рубиновый
        Grey = 13     // Пепельный сорняк (Инертный тупик)
    }

    /// <summary>
    /// Метаданные и параметры отображения цвета (название, ботаническое имя, Unity Color, hex, ранг).
    /// </summary>
    [Serializable]
    public struct BotanicalColor
    {
        public FlowerColor Type;
        public string DisplayName;
        public string BotanicalName;
        public Color Color;
        public string Hex;
        public int Tier;

        public BotanicalColor(FlowerColor type, string displayName, string botanicalName, string hex, int tier)
        {
            Type = type;
            DisplayName = displayName;
            BotanicalName = botanicalName;
            Hex = hex;
            Tier = tier;
            ColorUtility.TryParseHtmlString(hex, out Color parsedColor);
            Color = parsedColor;
        }

        public override string ToString() => $"{DisplayName} ({BotanicalName}) [{Hex}]";
    }

    /// <summary>
    /// Алиас для обратной совместимости с BotanicalColorInfo.
    /// </summary>
    public struct BotanicalColorInfo
    {
        public FlowerColor Type;
        public string DisplayName;
        public string BotanicalName;
        public Color Color;
        public string Hex;
        public int Tier;

        public BotanicalColorInfo(BotanicalColor c)
        {
            Type = c.Type;
            DisplayName = c.DisplayName;
            BotanicalName = c.BotanicalName;
            Color = c.Color;
            Hex = c.Hex;
            Tier = c.Tier;
        }

        public static implicit operator BotanicalColor(BotanicalColorInfo info) =>
            new BotanicalColor(info.Type, info.DisplayName, info.BotanicalName, info.Hex, info.Tier);
        public static implicit operator BotanicalColorInfo(BotanicalColor c) =>
            new BotanicalColorInfo(c);
    }

    /// <summary>
    /// Центральный реестр цветов, словарь метаданных и функции преобразований:
    /// 1. Merge (Смешивание двух цветов)
    /// 2. Warm (Утепление цвета)
    /// 3. Cool (Холодание цвета)
    /// А также словари отображения цветов enum в названия и Unity Color.
    /// </summary>
    public static class ColorRegistry
    {
        /// <summary>
        /// Главный словарь сопоставления enum FlowerColor с полными данными сорта.
        /// </summary>
        public static readonly Dictionary<FlowerColor, BotanicalColor> ColorInfoMap = new Dictionary<FlowerColor, BotanicalColor>
        {
            { FlowerColor.White,    new BotanicalColor(FlowerColor.White,    "Белый",            "Alabaster Lily",       "#F1FAEE", 0) },
            { FlowerColor.Red,      new BotanicalColor(FlowerColor.Red,      "Красный",          "Scarlet Poppy",        "#E63946", 1) },
            { FlowerColor.Blue,     new BotanicalColor(FlowerColor.Blue,     "Синий",            "Azure Bell",           "#1D3557", 1) },
            { FlowerColor.Pink,     new BotanicalColor(FlowerColor.Pink,     "Розовый",          "Blushing Camellia",    "#FFB4C2", 2) },
            { FlowerColor.SkyBlue,  new BotanicalColor(FlowerColor.SkyBlue,  "Голубой",          "Morning Hydrangea",    "#A8DADC", 2) },
            { FlowerColor.Purple,   new BotanicalColor(FlowerColor.Purple,   "Фиолетовый",       "Velvet Iris",          "#7B2CBF", 2) },
            { FlowerColor.Coral,    new BotanicalColor(FlowerColor.Coral,    "Коралловый",       "Sunset Dahlia",        "#F28482", 3) },
            { FlowerColor.Lavender, new BotanicalColor(FlowerColor.Lavender, "Лавандовый",       "Dusk Lavender",        "#CDB4DB", 3) },
            { FlowerColor.Lilac,    new BotanicalColor(FlowerColor.Lilac,    "Лиловый",          "Royal Lilac",          "#D8B4E2", 3) },
            { FlowerColor.Magenta,  new BotanicalColor(FlowerColor.Magenta,  "Маджента",         "Imperial Orchid",      "#C1121F", 3) },
            { FlowerColor.Indigo,   new BotanicalColor(FlowerColor.Indigo,   "Индиго",           "Deep Indigo",          "#3F37C9", 3) },
            { FlowerColor.IceBlue,  new BotanicalColor(FlowerColor.IceBlue,  "Ледяной",          "Frost Orchid",         "#E0FAFF", 3) },
            { FlowerColor.Ruby,     new BotanicalColor(FlowerColor.Ruby,     "Рубиновый",        "Garnet Rose",          "#9D0208", 4) },
            { FlowerColor.Grey,     new BotanicalColor(FlowerColor.Grey,     "Пепельный сорняк", "Ash Bell",             "#8D99AE", -1) }
        };

        /// <summary>
        /// Словарь отображения enum цвета в отображаемое русское название.
        /// </summary>
        public static readonly Dictionary<FlowerColor, string> ColorNames = new Dictionary<FlowerColor, string>
        {
            { FlowerColor.White,    "Белый" },
            { FlowerColor.Red,      "Красный" },
            { FlowerColor.Blue,     "Синий" },
            { FlowerColor.Pink,     "Розовый" },
            { FlowerColor.SkyBlue,  "Голубой" },
            { FlowerColor.Purple,   "Фиолетовый" },
            { FlowerColor.Coral,    "Коралловый" },
            { FlowerColor.Lavender, "Лавандовый" },
            { FlowerColor.Lilac,    "Лиловый" },
            { FlowerColor.Magenta,  "Маджента" },
            { FlowerColor.Indigo,   "Индиго" },
            { FlowerColor.IceBlue,  "Ледяной" },
            { FlowerColor.Ruby,     "Рубиновый" },
            { FlowerColor.Grey,     "Пепельный сорняк" }
        };

        /// <summary>
        /// Словарь отображения enum цвета в Unity Color.
        /// </summary>
        public static readonly Dictionary<FlowerColor, Color> UnityColors = new Dictionary<FlowerColor, Color>
        {
            { FlowerColor.White,    ColorInfoMap[FlowerColor.White].Color },
            { FlowerColor.Red,      ColorInfoMap[FlowerColor.Red].Color },
            { FlowerColor.Blue,     ColorInfoMap[FlowerColor.Blue].Color },
            { FlowerColor.Pink,     ColorInfoMap[FlowerColor.Pink].Color },
            { FlowerColor.SkyBlue,  ColorInfoMap[FlowerColor.SkyBlue].Color },
            { FlowerColor.Purple,   ColorInfoMap[FlowerColor.Purple].Color },
            { FlowerColor.Coral,    ColorInfoMap[FlowerColor.Coral].Color },
            { FlowerColor.Lavender, ColorInfoMap[FlowerColor.Lavender].Color },
            { FlowerColor.Lilac,    ColorInfoMap[FlowerColor.Lilac].Color },
            { FlowerColor.Magenta,  ColorInfoMap[FlowerColor.Magenta].Color },
            { FlowerColor.Indigo,   ColorInfoMap[FlowerColor.Indigo].Color },
            { FlowerColor.IceBlue,  ColorInfoMap[FlowerColor.IceBlue].Color },
            { FlowerColor.Ruby,     ColorInfoMap[FlowerColor.Ruby].Color },
            { FlowerColor.Grey,     ColorInfoMap[FlowerColor.Grey].Color }
        };

        #region Transformations (Merge, Warm, Cool)

        /// <summary>
        /// 1. Смешивание двух цветов (Binary Merge).
        /// Одинаковые цвета дают идентичный цвет.
        /// Несочетаемые и высокоранговые кроссы дают Grey.
        /// </summary>
        public static FlowerColor Merge(FlowerColor a, FlowerColor b)
        {
            if (a == b) return a;
            if (a == FlowerColor.Grey || b == FlowerColor.Grey) return FlowerColor.Grey;

            // Сортируем для однозначности пары
            FlowerColor first = a < b ? a : b;
            FlowerColor second = a < b ? b : a;

            switch (first)
            {
                case FlowerColor.White:
                    switch (second)
                    {
                        case FlowerColor.Red: return FlowerColor.Pink;
                        case FlowerColor.Blue: return FlowerColor.SkyBlue;
                        case FlowerColor.Pink: return FlowerColor.Pink;
                        case FlowerColor.SkyBlue: return FlowerColor.IceBlue;
                        case FlowerColor.Purple: return FlowerColor.Lilac;
                        case FlowerColor.Coral: return FlowerColor.Pink;
                        case FlowerColor.Lavender: return FlowerColor.Lilac;
                        case FlowerColor.Lilac: return FlowerColor.White;
                        case FlowerColor.Magenta: return FlowerColor.Lilac;
                        case FlowerColor.Indigo: return FlowerColor.SkyBlue;
                        case FlowerColor.IceBlue: return FlowerColor.IceBlue;
                        case FlowerColor.Ruby: return FlowerColor.Pink;
                    }
                    break;

                case FlowerColor.Red:
                    switch (second)
                    {
                        case FlowerColor.Blue: return FlowerColor.Purple;
                        case FlowerColor.Pink: return FlowerColor.Coral;
                        case FlowerColor.Purple: return FlowerColor.Magenta;
                        case FlowerColor.Coral: return FlowerColor.Ruby;
                        case FlowerColor.Magenta: return FlowerColor.Ruby;
                        case FlowerColor.Ruby: return FlowerColor.Ruby;
                    }
                    break;

                case FlowerColor.Blue:
                    switch (second)
                    {
                        case FlowerColor.SkyBlue: return FlowerColor.SkyBlue;
                        case FlowerColor.Purple: return FlowerColor.Indigo;
                        case FlowerColor.Indigo: return FlowerColor.Blue;
                        case FlowerColor.IceBlue: return FlowerColor.SkyBlue;
                    }
                    break;

                case FlowerColor.Pink:
                    switch (second)
                    {
                        case FlowerColor.SkyBlue: return FlowerColor.Lavender;
                        case FlowerColor.Purple: return FlowerColor.Magenta;
                        case FlowerColor.Coral: return FlowerColor.Pink;
                        case FlowerColor.Lavender: return FlowerColor.Lilac;
                        case FlowerColor.Lilac: return FlowerColor.Lilac;
                        case FlowerColor.Magenta: return FlowerColor.Magenta;
                        case FlowerColor.Ruby: return FlowerColor.Coral;
                    }
                    break;

                case FlowerColor.SkyBlue:
                    switch (second)
                    {
                        case FlowerColor.Purple: return FlowerColor.Indigo;
                        case FlowerColor.Lavender: return FlowerColor.SkyBlue;
                        case FlowerColor.Indigo: return FlowerColor.Indigo;
                        case FlowerColor.IceBlue: return FlowerColor.IceBlue;
                    }
                    break;

                case FlowerColor.Purple:
                    switch (second)
                    {
                        case FlowerColor.Lavender: return FlowerColor.Lilac;
                        case FlowerColor.Lilac: return FlowerColor.Lilac;
                        case FlowerColor.Magenta: return FlowerColor.Purple;
                        case FlowerColor.Indigo: return FlowerColor.Indigo;
                    }
                    break;

                case FlowerColor.Coral:
                    switch (second)
                    {
                        case FlowerColor.Magenta: return FlowerColor.Ruby;
                        case FlowerColor.Ruby: return FlowerColor.Ruby;
                    }
                    break;

                case FlowerColor.Lavender:
                    if (second == FlowerColor.Lilac) return FlowerColor.Lilac;
                    break;

                case FlowerColor.Magenta:
                    if (second == FlowerColor.Ruby) return FlowerColor.Ruby;
                    break;
            }

            return FlowerColor.Grey;
        }

        /// <summary>
        /// 2. Утепление цвета (Warming): сдвиг цвета в сторону теплого полюса.
        /// Белый при утеплении рождает Красный.
        /// </summary>
        public static FlowerColor Warm(FlowerColor color)
        {
            switch (color)
            {
                case FlowerColor.White: return FlowerColor.Red;      // Белый исток воспламеняется в Красный
                case FlowerColor.Red: return FlowerColor.Ruby;     // Углубление в Рубин
                case FlowerColor.Blue: return FlowerColor.Purple;   // Нагрев синего сдвигает к фиолетовому
                case FlowerColor.Ruby: return FlowerColor.Ruby;     // Предел тепла
                case FlowerColor.Coral: return FlowerColor.Ruby;     // Нагрев коралла
                case FlowerColor.Magenta: return FlowerColor.Red;      // Отжиг в чистый Красный
                case FlowerColor.Pink: return FlowerColor.Red;      // Возврат к базовому Красному
                case FlowerColor.Lilac: return FlowerColor.Pink;     // Сдвиг в тёплый Розовый
                case FlowerColor.Lavender: return FlowerColor.Lilac;    // Сдвиг в Лиловый
                case FlowerColor.Purple: return FlowerColor.Magenta;  // Сдвиг в Мадженту
                case FlowerColor.SkyBlue: return FlowerColor.White;    // Возврат к Белому
                case FlowerColor.IceBlue: return FlowerColor.SkyBlue;  // Оттаивание в Голубой
                case FlowerColor.Indigo: return FlowerColor.Blue;     // Возврат к Синему
                case FlowerColor.Grey: return FlowerColor.Grey;     // Инертный пепел
                default: return FlowerColor.Grey;
            }
        }

        /// <summary>
        /// 3. Холодание цвета (Cooling): сдвиг цвета в сторону холодного полюса.
        /// Белый при холодании замерзает в Синий.
        /// </summary>
        public static FlowerColor Cool(FlowerColor color)
        {
            switch (color)
            {
                case FlowerColor.White: return FlowerColor.Blue;      // Белый исток замерзает в Синий
                case FlowerColor.Red: return FlowerColor.Magenta;   // Остывание в Мадженту
                case FlowerColor.Blue: return FlowerColor.Indigo;    // Углубление в Индиго
                case FlowerColor.Ruby: return FlowerColor.Red;       // Остывание в Красный
                case FlowerColor.Coral: return FlowerColor.Red;       // Остывание в Красный
                case FlowerColor.Magenta: return FlowerColor.Purple;    // Остывание в Фиолетовый
                case FlowerColor.Pink: return FlowerColor.Lilac;     // Остывание в Лиловый
                case FlowerColor.Lilac: return FlowerColor.Lavender;  // Остывание в Лаванду
                case FlowerColor.Lavender: return FlowerColor.SkyBlue;   // Возврат в Голубой
                case FlowerColor.Purple: return FlowerColor.Blue;      // Возврат к базовому Синему
                case FlowerColor.SkyBlue: return FlowerColor.IceBlue;   // Замерзание в Ледяной
                case FlowerColor.IceBlue: return FlowerColor.IceBlue;   // Предел холода
                case FlowerColor.Indigo: return FlowerColor.Indigo;    // Предел холода
                case FlowerColor.Grey: return FlowerColor.Grey;      // Инертный пепел
                default: return FlowerColor.Grey;
            }
        }

        #endregion

        #region Helper & Lookup Methods

        public static BotanicalColor GetInfo(FlowerColor color)
        {
            if (ColorInfoMap.TryGetValue(color, out var info))
            {
                return info;
            }
            return ColorInfoMap[FlowerColor.Grey];
        }

        public static BotanicalColor GetColor(FlowerColor color)
        {
            return GetInfo(color);
        }

        public static Color GetUnityColor(FlowerColor color)
        {
            return GetInfo(color).Color;
        }

        public static string GetDisplayName(FlowerColor color)
        {
            return GetInfo(color).DisplayName;
        }

        public static string GetBotanicalName(FlowerColor color)
        {
            return GetInfo(color).BotanicalName;
        }

        public static bool TryParseColor(string colorId, out FlowerColor color)
        {
            return Enum.TryParse(colorId, true, out color);
        }

        // Обратная совместимость для строковых вызовов
        public static BotanicalColor GetColor(string colorId)
        {
            if (TryParseColor(colorId, out FlowerColor color))
            {
                return GetInfo(color);
            }
            return ColorInfoMap[FlowerColor.Grey];
        }

        public static string CombineColors(string colorA, string colorB)
        {
            if (TryParseColor(colorA, out FlowerColor a) && TryParseColor(colorB, out FlowerColor b))
            {
                return Merge(a, b).ToString();
            }
            return FlowerColor.Grey.ToString();
        }

        #endregion
    }
}
