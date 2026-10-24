using GardenMerger.Core;
using UnityEngine;

namespace GardenMerger.Tabletop
{
    public class SeedItem : DraggableItem
    {
        [SerializeField] private SpriteRenderer _seedRenderer;

        protected override void Awake()
        {
            base.Awake();
            if (_seedRenderer == null)
            {
                _seedRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public void Initialize(string colorId)
        {
            _colorId = colorId;
            BotanicalColor colorData = ColorRegistry.GetColor(colorId);

            if (_seedRenderer == null)
            {
                _seedRenderer = GetComponent<SpriteRenderer>();
            }

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
