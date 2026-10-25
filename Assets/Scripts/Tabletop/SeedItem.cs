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

        public override string GetDragTitle()
        {
            var color = ColorRegistry.GetColor(_colorId);
            string hex = ColorUtility.ToHtmlStringRGB(color.Color);
            return $"<color=#{hex}><b>Семечко: {color.DisplayName}</b></color> <size=11><color=#94A3B8>({color.BotanicalName})</color></size>";
        }

        public override string GetDragSubtitle()
        {
            var color = ColorRegistry.GetColor(_colorId);
            string baseInfo = $"Вырастет: {_form.ToDisplayName()} · Ранг {color.Tier}";

            if (GameManager.Instance != null && GameManager.Instance.CurrentOrder != null)
            {
                var order = GameManager.Instance.CurrentOrder;
                if (_colorId == order.TargetColorId)
                {
                    if (!order.RequiresFlowerForm || _form == order.TargetFlowerForm)
                    {
                        return $"{baseInfo}  <color=#4ADE80><b>★ Подходит для исследования!</b></color>";
                    }
                    else
                    {
                        return $"{baseInfo}  <color=#FBBF24><b>(Требуется форма: {order.TargetFlowerForm.ToDisplayName()})</b></color>";
                    }
                }
            }

            return baseInfo;
        }

        public override Color GetDragColor()
        {
            return ColorRegistry.GetColor(_colorId).Color;
        }

        public override float DragTooltipYOffset => 1.05f;
    }
}
