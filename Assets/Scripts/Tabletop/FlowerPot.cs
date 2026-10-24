using GardenMerger.Core;
using UnityEngine;

namespace GardenMerger.Tabletop
{
    public class FlowerPot : DraggableItem
    {
        [SerializeField] private SpriteRenderer _potRenderer;
        [SerializeField] private SpriteRenderer _stemRenderer;
        [SerializeField] private SpriteRenderer _petalsRenderer;
        [SerializeField] private SpriteRenderer _pistilRenderer;

        private BotanicalColor _colorData;

        public BotanicalColor ColorData => _colorData;

        public void Initialize(string colorId)
        {
            _colorId = colorId;
            _colorData = ColorRegistry.GetColor(colorId);

            if (_petalsRenderer != null)
            {
                _petalsRenderer.color = _colorData.Color;
            }
        }

        public void SetRenderers(SpriteRenderer pot, SpriteRenderer stem, SpriteRenderer petals, SpriteRenderer pistil)
        {
            _potRenderer = pot;
            _stemRenderer = stem;
            _petalsRenderer = petals;
            _pistilRenderer = pistil;
        }
    }
}
