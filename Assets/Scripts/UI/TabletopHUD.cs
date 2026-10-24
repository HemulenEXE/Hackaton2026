using System;
using System.Collections;
using GardenMerger.Configs;
using GardenMerger.Core;
using GardenMerger.Tabletop;
using UnityEngine;
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

        public void UpdateOrder(OrderConfigSO order, int currentMerges)
        {
            if (order == null) return;

            if (_orderTitleText != null) _orderTitleText.text = $"Заказ #{order.OrderId}: {order.ClientTitle}";
            if (_clientRoleText != null) _clientRoleText.text = order.ClientRole;
            if (_poeticHintText != null) _poeticHintText.text = order.PoeticHint;

            BotanicalColor targetColor = ColorRegistry.GetColor(order.TargetColorId);
            if (_targetColorText != null)
            {
                string targetForm = order.RequiresFlowerForm
                    ? $"\nФорма: {order.TargetFlowerForm.ToDisplayName()}"
                    : string.Empty;
                _targetColorText.text = $"Цель: {targetColor.DisplayName} — {targetColor.BotanicalName}{targetForm}";
            }

            if (_mergeCounterText != null)
            {
                string colorHex = currentMerges <= order.ParMerges ? "#238636" : "#B22222";
                _mergeCounterText.text = $"Скрещиваний: <color={colorHex}>{currentMerges}</color> / Пар: {order.ParMerges}";
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
