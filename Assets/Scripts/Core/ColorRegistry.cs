using System.Collections.Generic;
using UnityEngine;

namespace GardenMerger.Core
{
    public struct BotanicalColor
    {
        public string Id;
        public string DisplayName;
        public string BotanicalName;
        public Color Color;
        public int Tier;

        public BotanicalColor(string id, string displayName, string botanicalName, string hex, int tier)
        {
            Id = id;
            DisplayName = displayName;
            BotanicalName = botanicalName;
            ColorUtility.TryParseHtmlString(hex, out Color parsedColor);
            Color = parsedColor;
            Tier = tier;
        }
    }

    public static class ColorRegistry
    {
        private static readonly Dictionary<string, BotanicalColor> _colors = new Dictionary<string, BotanicalColor>
        {
            // Base Colors (Tier 0)
            { "Red", new BotanicalColor("Red", "Красный", "Scarlet Poppy", "#E63946", 0) },
            { "Blue", new BotanicalColor("Blue", "Синий", "Azure Bell", "#1D3557", 0) },
            { "White", new BotanicalColor("White", "Белый", "Alabaster Lily", "#F1FAEE", 0) },

            // Tier 1 Hybrids
            { "Pink", new BotanicalColor("Pink", "Розовый", "Blushing Camellia", "#FFB4C2", 1) },
            { "Purple", new BotanicalColor("Purple", "Фиолетовый", "Velvet Iris", "#7B2CBF", 1) },
            { "SkyBlue", new BotanicalColor("SkyBlue", "Голубой", "Morning Hydrangea", "#A8DADC", 1) },

            // Tier 2 Complex Hybrids
            { "Lavender", new BotanicalColor("Lavender", "Лавандовый", "Dusk Lavender", "#CDB4DB", 2) },
            { "Lilac", new BotanicalColor("Lilac", "Лиловый", "Royal Lilac", "#D8B4E2", 2) },
            { "Magenta", new BotanicalColor("Magenta", "Маджента", "Imperial Orchid", "#C1121F", 2) },
            { "Crimson", new BotanicalColor("Crimson", "Багряный", "Garnet Rose", "#9D0208", 2) },
            { "Navy", new BotanicalColor("Navy", "Тёмно-синий", "Midnight Petunia", "#03045E", 2) },
            { "Indigo", new BotanicalColor("Indigo", "Индиго", "Deep Indigo", "#3F37C9", 2) },

            // Fallback
            { "AshBell", new BotanicalColor("AshBell", "Пепельный сорняк", "Ash Bell", "#8D99AE", -1) }
        };

        public static BotanicalColor GetColor(string colorId)
        {
            if (_colors.TryGetValue(colorId, out var color))
            {
                return color;
            }
            return _colors["AshBell"];
        }

        public static bool TryGetColor(string colorId, out BotanicalColor color)
        {
            return _colors.TryGetValue(colorId, out color);
        }

        public static string CombineColors(string colorA, string colorB)
        {
            if (colorA == colorB) return colorA;

            string key = string.Compare(colorA, colorB, System.StringComparison.Ordinal) < 0
                ? $"{colorA}+{colorB}"
                : $"{colorB}+{colorA}";

            switch (key)
            {
                case "Red+White": return "Pink";
                case "Blue+Red": return "Purple";
                case "Blue+White": return "SkyBlue";
                case "Pink+SkyBlue": return "Lavender";
                case "Purple+Red": return "Magenta";
                case "Purple+White": return "Lilac";
                case "Pink+Red": return "Crimson";
                case "Blue+SkyBlue": return "Navy";
                case "Purple+SkyBlue": return "Indigo";
                default: return "AshBell";
            }
        }
    }
}
