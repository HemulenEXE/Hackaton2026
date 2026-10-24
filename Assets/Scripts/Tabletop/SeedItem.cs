using GardenMerger.Core;
using UnityEngine;

namespace GardenMerger.Tabletop
{
    public class SeedItem : DraggableItem
    {
        [SerializeField] private SpriteRenderer _seedRenderer;
        [SerializeField] private FlowerForm _form = FlowerForm.Flowers6;

        public FlowerForm Form => _form;

        
        protected override void Awake()
        {
            base.Awake();
            if (_seedRenderer == null)
            {
                _seedRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public void Initialize(string colorId, FlowerForm form = FlowerForm.Flowers6)
        {
            _colorId = colorId;
            _form = form;
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
