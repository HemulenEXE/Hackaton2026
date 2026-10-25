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
        [SerializeField] private FlowerForm _form = FlowerForm.Flowers6;

        private BotanicalColor _colorData;

        public BotanicalColor ColorData => _colorData;
        public FlowerForm Form => _form;

        
        protected override void Awake()
        {
            base.Awake();
            if (_potRenderer == null)
            {
                _potRenderer = GetComponent<SpriteRenderer>();
            }

            if (_petalsRenderer == null)
            {
                var petalsTransform = transform.Find("Petals");
                if (petalsTransform != null)
                {
                    _petalsRenderer = petalsTransform.GetComponent<SpriteRenderer>();
                }
            }
        }

        public void Initialize(string colorId,FlowerForm form,Sprite flowerSprite,Texture2D colorMask,Material maskMaterial)
        {
            _colorId = colorId;
            _form = form;
            _colorData = ColorRegistry.GetColor(colorId);

            if (_petalsRenderer == null)
            {
                Debug.LogError("Flower petals renderer is missing.", this);
                return;
            }

            _petalsRenderer.sprite = flowerSprite;
            if (flowerSprite == null || colorMask == null || maskMaterial == null)
            {
                Debug.LogError($"Flower visuals are not configured for form '{form}'.", this);
                return;
            }

            _petalsRenderer.color = Color.white;
            if (_potRenderer != null)
            {
                _potRenderer.color = Color.white;
                _petalsRenderer.sortingLayerID = _potRenderer.sortingLayerID;
                _petalsRenderer.sortingOrder = _potRenderer.sortingOrder + 1;
            }
            _petalsRenderer.sharedMaterial = maskMaterial;
            var propertyBlock = new MaterialPropertyBlock();
            _petalsRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture("_MaskTex", colorMask);
            propertyBlock.SetColor("_FlowerColor", _colorData.Color);
            _petalsRenderer.SetPropertyBlock(propertyBlock);
        }

        public void SetRenderers(SpriteRenderer pot, SpriteRenderer stem, SpriteRenderer petals, SpriteRenderer pistil)
        {
            _potRenderer = pot;
            _stemRenderer = stem;
            _petalsRenderer = petals;
            _pistilRenderer = pistil;
        }

        public override string GetDragTitle()
        {
            var color = ColorRegistry.GetColor(_colorId);
            string hex = ColorUtility.ToHtmlStringRGB(color.Color);
            return $"<color=#{hex}><b>{color.DisplayName}</b></color> <size=11><color=#94A3B8>({color.BotanicalName})</color></size>";
        }

        public override string GetDragSubtitle()
        {
            var color = ColorRegistry.GetColor(_colorId);
            string baseInfo = $"Форма: {_form.ToDisplayName()} · Ранг {color.Tier}";

            if (GameManager.Instance != null && GameManager.Instance.CurrentOrder != null)
            {
                var order = GameManager.Instance.CurrentOrder;
                if (_colorId == order.TargetColorId)
                {
                    if (!order.RequiresFlowerForm || _form == order.TargetFlowerForm)
                    {
                        return $"{baseInfo}  <color=#4ADE80><b>★ Целевой образец!</b></color>";
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

        public override float DragTooltipYOffset => 1.55f;
    }
}
