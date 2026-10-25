using System;
using System.Collections;
using GardenMerger.Configs;
using GardenMerger.Core;
using GardenMerger.Tabletop;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GardenMerger.UI
{
    public enum BannerType
    {
        Auto,
        Success,
        Discovery,
        Growth,
        Warning,
        Info
    }

    public class TabletopHUD : MonoBehaviour
    {
        [Header("Order Card")]
        [SerializeField] private Text _orderTitleText;
        [SerializeField] private Text _clientRoleText;
        [SerializeField] private Text _targetColorText;
        [SerializeField] private Text _poeticHintText;
        [SerializeField] private Text _mergeCounterText;
        [SerializeField] private Image _targetColorSwatch;

        [Header("Banner Notification")]
        [SerializeField] private GameObject _bannerRoot;
        [SerializeField] private Text _bannerTitleText;
        [SerializeField] private Text _bannerSubtext;

        [Header("Top Bar Buttons")]
        [SerializeField] private Button _notebookButton;
        [SerializeField] private Button _tunerButton;

        public event Action OnNotebookClicked;
        public event Action OnTunerClicked;

        // Cached banner components
        private RectTransform _bannerRect;
        private CanvasGroup _bannerCanvasGroup;
        private Image _bannerImage;
        private Outline _bannerOutline;
        private Shadow _bannerShadow;
        private Image _accentBarImage;
        private Image _topLineImage;
        private Outline _titleOutline;
        private Shadow _titleShadow;
        private Shadow _subtextShadow;
        private Vector2 _baseBannerPosition = new Vector2(0f, 310f);

        private Coroutine _bannerCoroutine;
        private bool _isBannerActive;
        private float _bannerHoldTimer;
        private float _punchProgress = 1f;
        private RectTransform _orderPanelRect;
        private Vector2 _orderPanelExpandedPosition;
        private Button _orderPanelToggleButton;
        private Text _orderPanelToggleText;
        private Coroutine _orderPanelSlideRoutine;
        private bool _orderPanelPinnedOpen = true;
        private bool _orderPanelPointerInside;
        private bool _orderPanelExpanded = true;

        private struct BannerTheme
        {
            public Color BackdropColor;
            public Color AccentColor;
            public Color TitleColor;
            public Color SubtextColor;
            public Color OutlineColor;
            public string TagText;

            public string AccentHex => ColorUtility.ToHtmlStringRGB(AccentColor);
        }

        private void Awake()
        {
            if (_notebookButton != null) _notebookButton.onClick.AddListener(() => OnNotebookClicked?.Invoke());
            if (_tunerButton != null) _tunerButton.onClick.AddListener(() => OnTunerClicked?.Invoke());

            if (_bannerRoot != null)
            {
                EnsureBannerComponents();
                _bannerRoot.SetActive(false);
            }
        }

        public void Setup(Text title, Text role, Text targetColor, Text hint, Text counter, Image swatch,
                          GameObject banner, Text bTitle, Text bSub, Button nbBtn, Button tunerBtn)
        {
            _orderTitleText = title;
            _clientRoleText = role;
            _targetColorText = targetColor;
            _poeticHintText = hint;
            _mergeCounterText = counter;
            _targetColorSwatch = swatch;
            _bannerRoot = banner;
            _bannerTitleText = bTitle;
            _bannerSubtext = bSub;
            _notebookButton = nbBtn;
            _tunerButton = tunerBtn;

            if (_notebookButton != null)
            {
                _notebookButton.onClick.RemoveAllListeners();
                _notebookButton.onClick.AddListener(() => OnNotebookClicked?.Invoke());
            }

            if (_tunerButton != null)
            {
                _tunerButton.onClick.RemoveAllListeners();
                _tunerButton.onClick.AddListener(() => OnTunerClicked?.Invoke());
            }

            if (_bannerRoot != null)
            {
                EnsureBannerComponents();
                _bannerRoot.SetActive(false);
            }
        }

        public static string GetClearInstruction(OrderConfigSO order)
        {
            if (order == null) return string.Empty;

            switch (order.OrderId)
            {
                case 1:
                    return "Поместите белое семя в инкубатор. Оставьте нейтральный режим и нажмите кнопку создания ростка.";
                case 2:
                    return "Переключите тумблер температуры на «ТЕПЛО» для получения красного мака, затем вырастите росток.";
                case 3:
                    return "Переключите тумблер температуры на «ХОЛОДНО» для получения синего цвета, затем вырастите росток.";
                case 4:
                    return "Скрестите розовый и голубой цветки в селекторе на столе, чтобы получить лавандовое семя.";
                case 5:
                    return "Скрестите противоположные сорта в селекторе для получения пепельного сорняка.";
                case 6:
                    return "Переключите тумблер влажности на «ВЛАЖНО» — форма цветка сдвинется с лилии на ромашку.";
                case 7:
                    return "Используйте ромашку как материнское растение слева и проведите влажный цикл до цветов на веточке.";
                case 8:
                    return "Оставьте влажный режим инкубатора, чтобы сдвинуть форму еще на одну ступень вперед до нескольких ромашек.";
                case 9:
                    return "Доведите форму до финального колокольчика через влажные циклы и придайте цветку лавандовый окрас.";
                default:
                    return string.IsNullOrEmpty(order.PoeticHint) ? "Вырастите требуемый сорт цветка в инкубаторе." : order.PoeticHint;
            }
        }

        private void EnsureOrderPanelStyling()
        {
            if (_orderTitleText == null) return;

            var panelTransform = _orderTitleText.transform.parent;
            if (panelTransform == null) return;

            var panelRect = panelTransform.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.sizeDelta = new Vector2(640f, 210f);
                if (_orderPanelRect == null)
                {
                    _orderPanelRect = panelRect;
                    _orderPanelExpandedPosition = panelRect.anchoredPosition;
                    BuildOrderPanelToggle(panelTransform);
                    BuildOrderPanelHoverEvents(panelTransform.gameObject);
                }
            }

            var panelImage = panelTransform.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(0.05f, 0.10f, 0.08f, 0.95f);
            }

            var panelOutline = panelTransform.GetComponent<Outline>();
            if (panelOutline == null)
            {
                panelOutline = panelTransform.gameObject.AddComponent<Outline>();
            }
            panelOutline.effectColor = new Color(0.22f, 0.65f, 0.42f, 0.85f);
            panelOutline.effectDistance = new Vector2(2f, -2f);

            var panelShadow = panelTransform.GetComponent<Shadow>();
            if (panelShadow == null)
            {
                panelShadow = panelTransform.gameObject.AddComponent<Shadow>();
            }
            panelShadow.effectColor = new Color(0f, 0f, 0f, 0.70f);
            panelShadow.effectDistance = new Vector2(0f, -6f);

            if (_orderTitleText != null)
            {
                _orderTitleText.supportRichText = true;
                _orderTitleText.fontSize = 24;
                _orderTitleText.fontStyle = FontStyle.Bold;
                _orderTitleText.alignment = TextAnchor.MiddleLeft;
                _orderTitleText.color = Color.white;
                var rect = _orderTitleText.rectTransform;
                rect.anchoredPosition = new Vector2(0f, 48f);
                rect.sizeDelta = new Vector2(590f, 38f);
            }

            if (_clientRoleText != null)
            {
                _clientRoleText.gameObject.SetActive(false);
                _clientRoleText.text = string.Empty;
            }

            if (_targetColorText != null)
            {
                _targetColorText.supportRichText = true;
                _targetColorText.fontSize = 20;
                _targetColorText.fontStyle = FontStyle.Normal;
                _targetColorText.alignment = TextAnchor.MiddleLeft;
                _targetColorText.color = Color.white;
                var rect = _targetColorText.rectTransform;
                rect.anchoredPosition = new Vector2(0f, 2f);
                rect.sizeDelta = new Vector2(590f, 36f);
            }

            if (_poeticHintText != null)
            {
                _poeticHintText.supportRichText = true;
                _poeticHintText.fontSize = 17;
                _poeticHintText.fontStyle = FontStyle.Normal;
                _poeticHintText.alignment = TextAnchor.MiddleLeft;
                _poeticHintText.color = new Color(0.95f, 0.96f, 0.98f, 1f);
                _poeticHintText.lineSpacing = 1.25f;
                var rect = _poeticHintText.rectTransform;
                rect.anchoredPosition = new Vector2(0f, -48f);
                rect.sizeDelta = new Vector2(590f, 56f);
            }

            if (_mergeCounterText != null)
            {
                _mergeCounterText.gameObject.SetActive(false);
                _mergeCounterText.text = string.Empty;
            }
        }

        private void BuildOrderPanelToggle(Transform panelTransform)
        {
            var buttonObject = new GameObject("OrderPanelToggle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panelTransform, false);
            buttonObject.transform.SetAsLastSibling();

            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0.5f);
            buttonRect.anchorMax = new Vector2(0f, 0.5f);
            buttonRect.pivot = new Vector2(0f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0f, 0f);
            buttonRect.sizeDelta = new Vector2(42f, 58f);

            var buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color(0.08f, 0.32f, 0.23f, 1f);

            _orderPanelToggleButton = buttonObject.GetComponent<Button>();
            _orderPanelToggleButton.transition = Selectable.Transition.ColorTint;
            _orderPanelToggleButton.onClick.AddListener(ToggleOrderPanel);

            var textObject = new GameObject("Arrow", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _orderPanelToggleText = textObject.GetComponent<Text>();
            _orderPanelToggleText.font = GameTextStyle.ReadableFont;
            _orderPanelToggleText.fontSize = 28;
            _orderPanelToggleText.alignment = TextAnchor.MiddleCenter;
            _orderPanelToggleText.color = Color.white;
            _orderPanelToggleText.raycastTarget = false;
            UpdateOrderPanelToggleIcon();
        }

        private void BuildOrderPanelHoverEvents(GameObject panelObject)
        {
            var eventTrigger = panelObject.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = panelObject.AddComponent<EventTrigger>();
            }
            if (eventTrigger.triggers == null)
            {
                eventTrigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();
            }

            AddOrderPanelPointerEvent(eventTrigger, EventTriggerType.PointerEnter, OnOrderPanelPointerEnter);
            AddOrderPanelPointerEvent(eventTrigger, EventTriggerType.PointerExit, OnOrderPanelPointerExit);
        }

        private static void AddOrderPanelPointerEvent(EventTrigger eventTrigger, EventTriggerType eventType, System.Action callback)
        {
            var entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener(_ => callback());
            eventTrigger.triggers.Add(entry);
        }

        private void ToggleOrderPanel()
        {
            _orderPanelPinnedOpen = !_orderPanelPinnedOpen;
            SetOrderPanelExpanded(_orderPanelPinnedOpen || _orderPanelPointerInside);
        }

        private void OnOrderPanelPointerEnter()
        {
            _orderPanelPointerInside = true;
            if (!_orderPanelPinnedOpen)
            {
                SetOrderPanelExpanded(true);
            }
        }

        private void OnOrderPanelPointerExit()
        {
            _orderPanelPointerInside = false;
            if (!_orderPanelPinnedOpen)
            {
                SetOrderPanelExpanded(false);
            }
        }

        private void SetOrderPanelExpanded(bool expanded)
        {
            if (_orderPanelRect == null)
            {
                return;
            }

            _orderPanelExpanded = expanded;
            if (_orderPanelSlideRoutine != null)
            {
                StopCoroutine(_orderPanelSlideRoutine);
            }

            Vector2 target = _orderPanelExpandedPosition;
            if (!expanded)
            {
                float visibleHandleWidth = _orderPanelToggleButton != null ? _orderPanelToggleButton.GetComponent<RectTransform>().rect.width : 42f;
                target.x += Mathf.Max(0f, _orderPanelRect.rect.width - visibleHandleWidth);
            }

            UpdateOrderPanelToggleIcon();
            _orderPanelSlideRoutine = StartCoroutine(SlideOrderPanel(target));
        }

        private System.Collections.IEnumerator SlideOrderPanel(Vector2 targetPosition)
        {
            Vector2 startPosition = _orderPanelRect.anchoredPosition;
            const float duration = 0.22f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                _orderPanelRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, t);
                yield return null;
            }

            _orderPanelRect.anchoredPosition = targetPosition;
            _orderPanelSlideRoutine = null;
        }

        private void UpdateOrderPanelToggleIcon()
        {
            if (_orderPanelToggleText != null)
            {
                _orderPanelToggleText.text = _orderPanelExpanded ? "‹" : "›";
            }
        }

        public void UpdateOrder(OrderConfigSO order, int currentMerges)
        {
            if (order == null) return;

            EnsureOrderPanelStyling();

            if (_orderTitleText != null)
            {
                _orderTitleText.text = $"<b><color=#FBBF24>ИССЛЕДОВАНИЕ #{order.OrderId}:</color> {order.ClientTitle}</b>";
            }

            if (_clientRoleText != null)
            {
                _clientRoleText.gameObject.SetActive(false);
                _clientRoleText.text = string.Empty;
            }

            BotanicalColor targetColor = ColorRegistry.GetColor(order.TargetColorId);
            string colorHex = ColorUtility.ToHtmlStringRGB(targetColor.Color);
            string formStr = order.RequiresFlowerForm
                ? $"🌸 Форма: <b>{order.TargetFlowerForm.ToDisplayName()}</b>"
                : "🌸 Форма: <b>Любая</b>";

            if (_targetColorText != null)
            {
                _targetColorText.text = $"🎯 <b>Целевой вид:</b> <color=#{colorHex}><b>{targetColor.DisplayName}</b></color> <size=16><color=#94A3B8>({targetColor.BotanicalName})</color></size>   |   {formStr}";
            }

            if (_poeticHintText != null)
            {
                string instruction = GetClearInstruction(order);
                _poeticHintText.text = $"📋 <b>Методика:</b> {instruction}";
            }

            if (_mergeCounterText != null)
            {
                _mergeCounterText.gameObject.SetActive(false);
                _mergeCounterText.text = string.Empty;
            }

            if (_targetColorSwatch != null)
            {
                _targetColorSwatch.color = targetColor.Color;
            }
        }

        public void ShowBanner(string title, string subtext, float duration = 3.0f)
        {
            ShowBanner(title, subtext, duration, BannerType.Auto);
        }

        public void ShowBanner(string title, string subtext, float duration, BannerType type)
        {
            if (_bannerRoot == null) return;

            EnsureBannerComponents();

            if (type == BannerType.Auto)
            {
                type = ResolveBannerType(title, subtext);
            }

            BannerTheme theme = GetThemeForType(type);
            ApplyTheme(theme, title, subtext);

            if (_isBannerActive && _bannerCoroutine != null)
            {
                // Juice: Punch & Flash on new message arrival while one is already showing
                _punchProgress = 0f;
                _bannerHoldTimer = Mathf.Max(_bannerHoldTimer, duration - 0.5f);
            }
            else
            {
                if (_bannerCoroutine != null) StopCoroutine(_bannerCoroutine);
                _bannerCoroutine = StartCoroutine(BannerRoutine(duration, theme));
            }
        }

        private void EnsureBannerComponents()
        {
            if (_bannerRoot == null) return;

            _bannerRect = _bannerRoot.GetComponent<RectTransform>();
            if (_bannerRect != null)
            {
                _bannerRect.pivot = new Vector2(0.5f, 0.5f);
                _bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
                _bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
                _bannerRect.sizeDelta = new Vector2(640f, 92f);
                if (_bannerRect.anchoredPosition.y != 0f)
                {
                    _baseBannerPosition = _bannerRect.anchoredPosition;
                }
                else
                {
                    _baseBannerPosition = new Vector2(0f, 310f);
                    _bannerRect.anchoredPosition = _baseBannerPosition;
                }
            }

            _bannerCanvasGroup = _bannerRoot.GetComponent<CanvasGroup>();
            if (_bannerCanvasGroup == null)
            {
                _bannerCanvasGroup = _bannerRoot.AddComponent<CanvasGroup>();
            }
            _bannerCanvasGroup.blocksRaycasts = false;
            _bannerCanvasGroup.interactable = false;

            _bannerImage = _bannerRoot.GetComponent<Image>();
            if (_bannerImage == null)
            {
                _bannerImage = _bannerRoot.AddComponent<Image>();
            }

            _bannerOutline = _bannerRoot.GetComponent<Outline>();
            if (_bannerOutline == null)
            {
                _bannerOutline = _bannerRoot.AddComponent<Outline>();
            }
            _bannerOutline.effectDistance = new Vector2(2f, -2f);

            _bannerShadow = _bannerRoot.GetComponent<Shadow>();
            if (_bannerShadow == null)
            {
                _bannerShadow = _bannerRoot.AddComponent<Shadow>();
            }
            _bannerShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            _bannerShadow.effectDistance = new Vector2(0f, -8f);

            // Left glowing accent bar
            var accentBarTransform = _bannerRoot.transform.Find("AccentBar");
            if (accentBarTransform == null)
            {
                var barObj = new GameObject("AccentBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                barObj.transform.SetParent(_bannerRoot.transform, false);
                accentBarTransform = barObj.transform;
            }
            var barRect = accentBarTransform.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0.5f);
            barRect.anchorMax = new Vector2(0f, 0.5f);
            barRect.pivot = new Vector2(0f, 0.5f);
            barRect.anchoredPosition = new Vector2(14f, 0f);
            barRect.sizeDelta = new Vector2(6f, 68f);
            _accentBarImage = accentBarTransform.GetComponent<Image>();

            // Top subtle highlight line
            var topLineTransform = _bannerRoot.transform.Find("TopLine");
            if (topLineTransform == null)
            {
                var lineObj = new GameObject("TopLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                lineObj.transform.SetParent(_bannerRoot.transform, false);
                topLineTransform = lineObj.transform;
            }
            var lineRect = topLineTransform.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 1f);
            lineRect.anchorMax = new Vector2(0.5f, 1f);
            lineRect.pivot = new Vector2(0.5f, 1f);
            lineRect.anchoredPosition = new Vector2(0f, -2f);
            lineRect.sizeDelta = new Vector2(624f, 2f);
            _topLineImage = topLineTransform.GetComponent<Image>();

            // Title styling
            if (_bannerTitleText != null)
            {
                _bannerTitleText.supportRichText = true;
                _bannerTitleText.fontSize = 21;
                _bannerTitleText.fontStyle = FontStyle.Bold;
                _bannerTitleText.alignment = TextAnchor.MiddleLeft;

                var titleRect = _bannerTitleText.rectTransform;
                titleRect.anchorMin = new Vector2(0.5f, 0.5f);
                titleRect.anchorMax = new Vector2(0.5f, 0.5f);
                titleRect.pivot = new Vector2(0.5f, 0.5f);
                titleRect.anchoredPosition = new Vector2(36f, 16f);
                titleRect.sizeDelta = new Vector2(560f, 36f);

                _titleOutline = _bannerTitleText.GetComponent<Outline>();
                if (_titleOutline == null)
                {
                    _titleOutline = _bannerTitleText.gameObject.AddComponent<Outline>();
                }
                _titleOutline.effectColor = new Color(0f, 0f, 0f, 0.90f);
                _titleOutline.effectDistance = new Vector2(1.5f, -1.5f);

                _titleShadow = _bannerTitleText.GetComponent<Shadow>();
                if (_titleShadow == null)
                {
                    _titleShadow = _bannerTitleText.gameObject.AddComponent<Shadow>();
                }
                _titleShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
                _titleShadow.effectDistance = new Vector2(1f, -2f);
            }

            // Subtext styling
            if (_bannerSubtext != null)
            {
                _bannerSubtext.supportRichText = true;
                _bannerSubtext.fontSize = 15;
                _bannerSubtext.fontStyle = FontStyle.Normal;
                _bannerSubtext.alignment = TextAnchor.MiddleLeft;

                var subRect = _bannerSubtext.rectTransform;
                subRect.anchorMin = new Vector2(0.5f, 0.5f);
                subRect.anchorMax = new Vector2(0.5f, 0.5f);
                subRect.pivot = new Vector2(0.5f, 0.5f);
                subRect.anchoredPosition = new Vector2(36f, -18f);
                subRect.sizeDelta = new Vector2(560f, 34f);

                _subtextShadow = _bannerSubtext.GetComponent<Shadow>();
                if (_subtextShadow == null)
                {
                    _subtextShadow = _bannerSubtext.gameObject.AddComponent<Shadow>();
                }
                _subtextShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
                _subtextShadow.effectDistance = new Vector2(1f, -1.5f);
            }
        }

        private BannerType ResolveBannerType(string title, string subtext)
        {
            if (string.IsNullOrEmpty(title)) return BannerType.Info;

            string lower = title.ToLowerInvariant();
            if (lower.Contains("выполнен") || lower.Contains("пройдена") || lower.Contains("победа") || lower.Contains("триумф"))
            {
                return BannerType.Success;
            }
            if (lower.Contains("тайна") || lower.Contains("рецепт") || lower.Contains("открыт") || lower.Contains("журнал") || lower.Contains("разгадан"))
            {
                return BannerType.Discovery;
            }
            if (lower.Contains("распустился") || lower.Contains("селекция") || lower.Contains("семечк") || lower.Contains("росток") || lower.Contains("цветок"))
            {
                return BannerType.Growth;
            }
            if (lower.Contains("недостаточно") || lower.Contains("не тот") || lower.Contains("нужна другая") || lower.Contains("ошибка") || lower.Contains("внимание"))
            {
                return BannerType.Warning;
            }
            if (lower.Contains("температур") || lower.Contains("влажност") || lower.Contains("режим") || lower.Contains("настройк"))
            {
                return BannerType.Info;
            }

            return BannerType.Info;
        }

        private BannerTheme GetThemeForType(BannerType type)
        {
            switch (type)
            {
                case BannerType.Success:
                    return new BannerTheme
                    {
                        BackdropColor = new Color(0.04f, 0.11f, 0.08f, 0.96f),
                        AccentColor = new Color(0.98f, 0.75f, 0.14f, 1f),
                        TitleColor = new Color(0.99f, 0.88f, 0.28f, 1f),
                        SubtextColor = new Color(0.95f, 0.96f, 0.98f, 1f),
                        OutlineColor = new Color(0.85f, 0.52f, 0.06f, 0.90f),
                        TagText = "★ УСПЕХ ★"
                    };

                case BannerType.Discovery:
                    return new BannerTheme
                    {
                        BackdropColor = new Color(0.04f, 0.09f, 0.16f, 0.96f),
                        AccentColor = new Color(0.22f, 0.74f, 0.97f, 1f),
                        TitleColor = new Color(0.49f, 0.83f, 0.99f, 1f),
                        SubtextColor = new Color(0.94f, 0.98f, 1.00f, 1f),
                        OutlineColor = new Color(0.01f, 0.52f, 0.78f, 0.90f),
                        TagText = "✦ ТАЙНА ✦"
                    };

                case BannerType.Growth:
                    return new BannerTheme
                    {
                        BackdropColor = new Color(0.03f, 0.12f, 0.07f, 0.96f),
                        AccentColor = new Color(0.29f, 0.87f, 0.50f, 1f),
                        TitleColor = new Color(0.53f, 0.94f, 0.67f, 1f),
                        SubtextColor = new Color(0.94f, 0.99f, 0.96f, 1f),
                        OutlineColor = new Color(0.09f, 0.64f, 0.29f, 0.90f),
                        TagText = "✦ САД ✦"
                    };

                case BannerType.Warning:
                    return new BannerTheme
                    {
                        BackdropColor = new Color(0.15f, 0.05f, 0.07f, 0.96f),
                        AccentColor = new Color(0.98f, 0.44f, 0.52f, 1f),
                        TitleColor = new Color(0.99f, 0.64f, 0.69f, 1f),
                        SubtextColor = new Color(1.00f, 0.95f, 0.95f, 1f),
                        OutlineColor = new Color(0.88f, 0.11f, 0.28f, 0.90f),
                        TagText = "▲ ВНИМАНИЕ ▲"
                    };

                case BannerType.Info:
                    return new BannerTheme
                    {
                        BackdropColor = new Color(0.04f, 0.09f, 0.14f, 0.96f),
                        AccentColor = new Color(0.22f, 0.74f, 0.97f, 1f),
                        TitleColor = new Color(0.56f, 0.88f, 1.00f, 1f),
                        SubtextColor = new Color(0.94f, 0.98f, 1.00f, 1f),
                        OutlineColor = new Color(0.03f, 0.55f, 0.75f, 0.90f),
                        TagText = "⚙ РЕЖИМ ⚙"
                    };

                default:
                    return new BannerTheme
                    {
                        BackdropColor = new Color(0.07f, 0.10f, 0.08f, 0.96f),
                        AccentColor = new Color(0.96f, 0.62f, 0.04f, 1f),
                        TitleColor = new Color(0.99f, 0.83f, 0.30f, 1f),
                        SubtextColor = new Color(0.97f, 0.98f, 0.99f, 1f),
                        OutlineColor = new Color(0.71f, 0.33f, 0.04f, 0.90f),
                        TagText = "● ВЕСТНИК ●"
                    };
            }
        }

        private void ApplyTheme(BannerTheme theme, string title, string subtext)
        {
            if (_bannerTitleText != null)
            {
                _bannerTitleText.text = $"<size=12><color=#{theme.AccentHex}><b>{theme.TagText}</b></color></size>   <b>{title}</b>";
                _bannerTitleText.color = theme.TitleColor;
            }

            if (_bannerSubtext != null)
            {
                _bannerSubtext.text = subtext;
                _bannerSubtext.color = theme.SubtextColor;
            }

            if (_bannerImage != null)
            {
                _bannerImage.color = theme.BackdropColor;
            }

            if (_bannerOutline != null)
            {
                _bannerOutline.effectColor = theme.OutlineColor;
            }

            if (_accentBarImage != null)
            {
                _accentBarImage.color = theme.AccentColor;
            }

            if (_topLineImage != null)
            {
                _topLineImage.color = new Color(theme.AccentColor.r, theme.AccentColor.g, theme.AccentColor.b, 0.50f);
            }
        }

        private IEnumerator BannerRoutine(float duration, BannerTheme theme)
        {
            _isBannerActive = true;
            _bannerRoot.SetActive(true);

            Vector2 basePos = _baseBannerPosition;
            Vector2 startPos = basePos + new Vector2(0f, 60f);
            _bannerRect.anchoredPosition = startPos;
            _bannerRect.localScale = Vector3.one * 0.35f;
            _bannerCanvasGroup.alpha = 0f;

            // 1. POP & OVERSHOOT (0.32s)
            float enterDuration = 0.32f;
            float elapsed = 0f;
            while (elapsed < enterDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / enterDuration);

                // Cubic out slide
                float posT = 1f - Mathf.Pow(1f - t, 3f);
                _bannerRect.anchoredPosition = Vector2.Lerp(startPos, basePos, posT);

                // Elastic overshoot scale: 0.35 -> 1.16 -> 1.00
                float scale = (t < 0.65f)
                    ? Mathf.Lerp(0.35f, 1.16f, t / 0.65f)
                    : Mathf.Lerp(1.16f, 1.0f, (t - 0.65f) / 0.35f);
                _bannerRect.localScale = Vector3.one * scale;

                _bannerCanvasGroup.alpha = Mathf.Clamp01(t * 3.5f);
                yield return null;
            }

            _bannerRect.anchoredPosition = basePos;
            _bannerRect.localScale = Vector3.one;
            _bannerCanvasGroup.alpha = 1f;

            // 2. LIVING HOLD & GENTLE FLOAT
            float holdDuration = Mathf.Max(0.5f, duration - 0.55f);
            _bannerHoldTimer = holdDuration;
            float bobTime = 0f;

            while (_bannerHoldTimer > 0f)
            {
                _bannerHoldTimer -= Time.deltaTime;
                bobTime += Time.deltaTime;

                if (_punchProgress < 1f)
                {
                    _punchProgress += Time.deltaTime * 6f;
                    float punchT = Mathf.Clamp01(_punchProgress);
                    float punchScale = Mathf.Lerp(1.22f, 1.0f, punchT);
                    _bannerRect.localScale = Vector3.one * punchScale;
                }
                else
                {
                    float breathe = 1f + Mathf.Sin(bobTime * 3.5f) * 0.012f;
                    _bannerRect.localScale = Vector3.one * breathe;
                }

                float floatOffset = Mathf.Sin(bobTime * 4f) * 2.5f;
                _bannerRect.anchoredPosition = new Vector2(basePos.x, basePos.y + floatOffset);

                yield return null;
            }

            // 3. SNAPPY EXIT (0.22s)
            float exitDuration = 0.22f;
            elapsed = 0f;
            Vector2 exitStartPos = _bannerRect.anchoredPosition;
            Vector2 exitEndPos = basePos + new Vector2(0f, 35f);

            while (elapsed < exitDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / exitDuration);

                float scale = (t < 0.15f)
                    ? Mathf.Lerp(1.0f, 1.04f, t / 0.15f)
                    : Mathf.Lerp(1.04f, 0.65f, (t - 0.15f) / 0.85f);

                _bannerRect.localScale = Vector3.one * scale;
                _bannerRect.anchoredPosition = Vector2.Lerp(exitStartPos, exitEndPos, t);
                _bannerCanvasGroup.alpha = 1f - Mathf.Pow(t, 2f);

                yield return null;
            }

            _bannerCanvasGroup.alpha = 0f;
            _bannerRoot.SetActive(false);
            _isBannerActive = false;
            _bannerCoroutine = null;
        }
    }
}
