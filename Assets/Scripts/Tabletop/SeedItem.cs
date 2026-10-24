using GardenMerger.Core;
using UnityEngine;

namespace GardenMerger.Tabletop
{
    public class SeedItem : DraggableItem
    {
        [SerializeField] private SpriteRenderer _seedRenderer;
        [SerializeField] private FlowerForm _form = FlowerForm.Flowers6;

        public FlowerForm Form => _form;

        public void Initialize(string colorId, FlowerForm form = FlowerForm.Flowers6)
        {
            _colorId = colorId;
            _form = form;
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
