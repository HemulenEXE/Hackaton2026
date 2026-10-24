using GardenMerger.Core;
using UnityEngine;

namespace GardenMerger.Tabletop
{
    public class SeedItem : DraggableItem
    {
        [SerializeField] private SpriteRenderer _seedRenderer;

        public void Initialize(string colorId)
        {
            _colorId = colorId;
            BotanicalColor colorData = ColorRegistry.GetColor(colorId);

            if (_seedRenderer != null)
            {
                _seedRenderer.color = colorData.Color;
            }
        }

        public void SetRenderer(SpriteRenderer renderer)
        {
            _seedRenderer = renderer;
        }
    }
}
