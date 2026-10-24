using UnityEngine;

namespace GardenMerger.Configs
{
    [CreateAssetMenu(fileName = "NewSeedConfig", menuName = "GardenMerger/Seed Config")]
    public class SeedConfigSO : ScriptableObject
    {
        [SerializeField] private string _seedId;
        [SerializeField] private string _colorId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _botanicalName;
        [SerializeField] private Color _displayColor = Color.white;
        [SerializeField] private int _tier;

        public string SeedId => _seedId;
        public string ColorId => _colorId;
        public string DisplayName => _displayName;
        public string BotanicalName => _botanicalName;
        public Color DisplayColor => _displayColor;
        public int Tier => _tier;

        public void Initialize(string seedId, string colorId, string displayName, string botanicalName, Color color, int tier)
        {
            _seedId = seedId;
            _colorId = colorId;
            _displayName = displayName;
            _botanicalName = botanicalName;
            _displayColor = color;
            _tier = tier;
        }
    }
}
