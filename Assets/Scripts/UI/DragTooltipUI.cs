using System;
using GardenMerger.Tabletop;
using UnityEngine;
using UnityEngine.UI;

namespace GardenMerger.UI
{
    /// <summary>
    /// Отображает красивую, четкую плашку с информацией о цветке или семечке
    /// прямо над перетаскиваемым объектом на игровом столе.
    /// </summary>
    public class DragTooltipUI : MonoBehaviour
    {
        public static DragTooltipUI Instance { get; private set; }

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Image _bgImage;
        private Outline _bgOutline;
        private Shadow _bgShadow;

        private RectTransform _canvasRect;
        private Camera _mainCamera;

        private Image _swatchImage;
        private Text _titleText;
        private Text _subtitleText;

        private float _currentAlpha = 0f;
        private float _scaleProgress = 0f;
        private bool _isFirstFrame = true;
        private DraggableItem _lastItem;

        public static void EnsureInstance()
        {
            if (Instance != null) return;

            var existing = FindFirstObjectByType<DragTooltipUI>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            var canvas = FindSceneCanvas();
            if (canvas == null) return;

            var tooltipObj = new GameObject("DragTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(DragTooltipUI));
            tooltipObj.transform.SetParent(canvas.transform, false);
        }

        private static Canvas FindSceneCanvas()
        {
            var mainCanvas = GameObject.Find("MainCanvas");
            if (mainCanvas != null)
            {
                var c = mainCanvas.GetComponent<Canvas>();
                if (c != null) return c;
            }

            return FindFirstObjectByType<Canvas>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _mainCamera = Camera.main;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _canvasRect = canvas.GetComponent<RectTransform>();
            }

            BuildUIHierarchy();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
            _currentAlpha = 0f;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void BuildUIHierarchy()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null) _rectTransform = gameObject.AddComponent<RectTransform>();

            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot = new Vector2(0.5f, 0f);
            _rectTransform.sizeDelta = new Vector2(420f, 62f);

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            _bgImage = GetComponent<Image>();
            if (_bgImage == null) _bgImage = gameObject.AddComponent<Image>();
            _bgImage.color = new Color(0.06f, 0.10f, 0.08f, 0.94f);

            _bgOutline = GetComponent<Outline>();
            if (_bgOutline == null) _bgOutline = gameObject.AddComponent<Outline>();
            _bgOutline.effectDistance = new Vector2(1.5f, -1.5f);

            _bgShadow = GetComponent<Shadow>();
            if (_bgShadow == null) _bgShadow = gameObject.AddComponent<Shadow>();
            _bgShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            _bgShadow.effectDistance = new Vector2(0f, -5f);

            Font customFont = null;
            var anyText = FindFirstObjectByType<Text>();
            if (anyText != null && anyText.font != null)
            {
                customFont = anyText.font;
            }

            // Swatch Container
            var swatchObj = transform.Find("Swatch");
            if (swatchObj == null)
            {
                var sObj = new GameObject("Swatch", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
                sObj.transform.SetParent(transform, false);
                swatchObj = sObj.transform;
            }
            var swatchRect = swatchObj.GetComponent<RectTransform>();
            swatchRect.anchorMin = new Vector2(0f, 0.5f);
            swatchRect.anchorMax = new Vector2(0f, 0.5f);
            swatchRect.pivot = new Vector2(0f, 0.5f);
            swatchRect.anchoredPosition = new Vector2(14f, 0f);
            swatchRect.sizeDelta = new Vector2(28f, 28f);

            _swatchImage = swatchObj.GetComponent<Image>();
            var swatchOutline = swatchObj.GetComponent<Outline>();
            if (swatchOutline != null)
            {
                swatchOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                swatchOutline.effectDistance = new Vector2(1f, -1f);
            }

            // Title Text
            var titleObj = transform.Find("Title");
            if (titleObj == null)
            {
                var tObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
                tObj.transform.SetParent(transform, false);
                titleObj = tObj.transform;
            }
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(0f, 0.5f);
            titleRect.pivot = new Vector2(0f, 0.5f);
            titleRect.anchoredPosition = new Vector2(50f, 11f);
            titleRect.sizeDelta = new Vector2(356f, 26f);

            _titleText = titleObj.GetComponent<Text>();
            _titleText.supportRichText = true;
            _titleText.fontSize = 18;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleLeft;
            if (customFont != null) _titleText.font = customFont;

            var titleOutline = titleObj.GetComponent<Outline>();
            if (titleOutline != null)
            {
                titleOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                titleOutline.effectDistance = new Vector2(1.2f, -1.2f);
            }

            // Subtitle Text
            var subObj = transform.Find("Subtitle");
            if (subObj == null)
            {
                var sObj = new GameObject("Subtitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
                sObj.transform.SetParent(transform, false);
                subObj = sObj.transform;
            }
            var subRect = subObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0f, 0.5f);
            subRect.anchorMax = new Vector2(0f, 0.5f);
            subRect.pivot = new Vector2(0f, 0.5f);
            subRect.anchoredPosition = new Vector2(50f, -12f);
            subRect.sizeDelta = new Vector2(356f, 24f);

            _subtitleText = subObj.GetComponent<Text>();
            _subtitleText.supportRichText = true;
            _subtitleText.fontSize = 15;
            _subtitleText.fontStyle = FontStyle.Normal;
            _subtitleText.alignment = TextAnchor.MiddleLeft;
            _subtitleText.color = new Color(0.90f, 0.94f, 0.96f, 1f);
            if (customFont != null) _subtitleText.font = customFont;

            var subShadow = subObj.GetComponent<Shadow>();
            if (subShadow != null)
            {
                subShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
                subShadow.effectDistance = new Vector2(1f, -1f);
            }
        }

        private void LateUpdate()
        {
            var item = DraggableItem.CurrentlyDraggedItem;
            if (_mainCamera == null) _mainCamera = Camera.main;

            if (_canvasRect == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null) _canvasRect = canvas.GetComponent<RectTransform>();
            }

            if (item != null && item.IsDragging && item.gameObject.activeInHierarchy && _mainCamera != null && _canvasRect != null)
            {
                if (_lastItem != item)
                {
                    _lastItem = item;
                    _isFirstFrame = true;
                    _scaleProgress = 0f;
                }

                if (_titleText != null) _titleText.text = item.GetDragTitle();
                if (_subtitleText != null) _subtitleText.text = item.GetDragSubtitle();

                Color dragColor = item.GetDragColor();
                if (_swatchImage != null) _swatchImage.color = dragColor;
                if (_bgOutline != null) _bgOutline.effectColor = new Color(dragColor.r, dragColor.g, dragColor.b, 0.85f);

                Vector3 worldPos = item.transform.position + Vector3.up * item.DragTooltipYOffset;
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_mainCamera, worldPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, null, out Vector2 localPos);

                Vector2 canvasSize = _canvasRect.rect.size;
                float halfW = _rectTransform.rect.width * 0.5f + 12f;
                float minX = -canvasSize.x * 0.5f + halfW;
                float maxX = canvasSize.x * 0.5f - halfW;
                float minY = -canvasSize.y * 0.5f + 20f;
                float maxY = canvasSize.y * 0.5f - _rectTransform.rect.height - 16f;

                localPos.x = Mathf.Clamp(localPos.x, minX, maxX);
                localPos.y = Mathf.Clamp(localPos.y, minY, maxY);

                if (_isFirstFrame)
                {
                    _rectTransform.anchoredPosition = localPos;
                    _isFirstFrame = false;
                }
                else
                {
                    _rectTransform.anchoredPosition = Vector2.Lerp(_rectTransform.anchoredPosition, localPos, Time.deltaTime * 26f);
                }

                _currentAlpha = Mathf.MoveTowards(_currentAlpha, 1f, Time.deltaTime * 9f);
                _scaleProgress = Mathf.Clamp01(_scaleProgress + Time.deltaTime * 7.5f);

                float scale = (_scaleProgress < 0.65f)
                    ? Mathf.Lerp(0.70f, 1.08f, _scaleProgress / 0.65f)
                    : Mathf.Lerp(1.08f, 1.0f, (_scaleProgress - 0.65f) / 0.35f);

                _rectTransform.localScale = Vector3.one * scale;
                if (_canvasGroup != null) _canvasGroup.alpha = _currentAlpha;
            }
            else
            {
                _lastItem = null;
                _isFirstFrame = true;

                if (_currentAlpha > 0f)
                {
                    _currentAlpha = Mathf.MoveTowards(_currentAlpha, 0f, Time.deltaTime * 10f);
                    if (_canvasGroup != null) _canvasGroup.alpha = _currentAlpha;
                    _rectTransform.localScale = Vector3.Lerp(_rectTransform.localScale, Vector3.one * 0.8f, Time.deltaTime * 10f);
                }
            }
        }
    }
}
