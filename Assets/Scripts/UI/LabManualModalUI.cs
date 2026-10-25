using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GardenMerger.UI
{
    public class LabManualModalUI : MonoBehaviour
    {
        private GameObject _backdropRoot;
        private RectTransform _cardRect;
        private CanvasGroup _canvasGroup;
        private Button _closeCrossButton;
        private Button _bottomConfirmButton;
        private Button _cornerHelpButton;
        private AudioClip _clickSound;
        private AudioSource _audioSource;
        private Coroutine _animRoutine;

        public bool IsOpen => _backdropRoot != null && _backdropRoot.activeSelf;
        public event Action OnOpened;
        public event Action OnClosed;

        public static LabManualModalUI EnsureInstance(Transform canvasTransform, AudioClip clickSound = null)
        {
            if (canvasTransform == null) return null;

            var existing = canvasTransform.GetComponentInChildren<LabManualModalUI>(true);
            if (existing != null)
            {
                existing._clickSound = clickSound ?? existing._clickSound;
                return existing;
            }

            var host = new GameObject("LabManualModalUI", typeof(RectTransform), typeof(LabManualModalUI));
            host.transform.SetParent(canvasTransform, false);
            var modal = host.GetComponent<LabManualModalUI>();
            modal._clickSound = clickSound;
            modal.BuildUI(canvasTransform);
            return modal;
        }

        private void BuildUI(Transform canvasTransform)
        {
            Font mainFont = null;
            var sampleText = canvasTransform.GetComponentInChildren<Text>(true);
            if (sampleText != null && sampleText.font != null)
            {
                mainFont = sampleText.font;
            }
            if (mainFont == null)
            {
                mainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }

            BuildCornerHelpButton(canvasTransform, mainFont);
            BuildModalWindow(canvasTransform, mainFont);
        }

        private void BuildCornerHelpButton(Transform canvasTransform, Font font)
        {
            var btnObj = new GameObject("CornerHelpButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Button));
            btnObj.transform.SetParent(canvasTransform, false);

            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(26f, -26f);
            rt.sizeDelta = new Vector2(56f, 56f);

            var img = btnObj.GetComponent<Image>();
            img.color = new Color(0.04f, 0.09f, 0.12f, 0.94f);

            var outline = btnObj.GetComponent<Outline>();
            outline.effectColor = new Color(0.12f, 0.72f, 0.52f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);

            var shadow = btnObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(0f, -4f);

            var textObj = new GameObject("Icon", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(btnObj.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var txt = textObj.GetComponent<Text>();
            txt.font = font;
            txt.text = "?";
            txt.fontSize = 34;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.99f, 0.82f, 0.32f, 1f);
            txt.raycastTarget = false;

            // Small badge text under the button: "СПРАВКА"
            var labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(btnObj.transform, false);
            var labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.5f, 0f);
            labelRt.anchorMax = new Vector2(0.5f, 0f);
            labelRt.pivot = new Vector2(0.5f, 1f);
            labelRt.anchoredPosition = new Vector2(0f, -3f);
            labelRt.sizeDelta = new Vector2(70f, 18f);

            var labelTxt = labelObj.GetComponent<Text>();
            labelTxt.font = font;
            labelTxt.text = "СПРАВКА";
            labelTxt.fontSize = 11;
            labelTxt.alignment = TextAnchor.MiddleCenter;
            labelTxt.color = new Color(0.70f, 0.82f, 0.88f, 0.90f);
            labelTxt.raycastTarget = false;

            _cornerHelpButton = btnObj.GetComponent<Button>();
            var colors = _cornerHelpButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            _cornerHelpButton.colors = colors;

            _cornerHelpButton.onClick.AddListener(() =>
            {
                PlaySound();
                Show();
            });
        }

        private void BuildModalWindow(Transform canvasTransform, Font font)
        {
            // Fullscreen backdrop blocker
            _backdropRoot = new GameObject("LabManualBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            _backdropRoot.transform.SetParent(canvasTransform, false);
            _backdropRoot.transform.SetAsLastSibling();

            var backdropRt = _backdropRoot.GetComponent<RectTransform>();
            backdropRt.anchorMin = Vector2.zero;
            backdropRt.anchorMax = Vector2.one;
            backdropRt.offsetMin = Vector2.zero;
            backdropRt.offsetMax = Vector2.zero;

            var backdropImg = _backdropRoot.GetComponent<Image>();
            backdropImg.color = new Color(0.02f, 0.04f, 0.07f, 0.84f);

            _canvasGroup = _backdropRoot.GetComponent<CanvasGroup>();

            // Modal Card Container (980 x 760)
            var cardObj = new GameObject("ModalCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Shadow));
            cardObj.transform.SetParent(_backdropRoot.transform, false);

            _cardRect = cardObj.GetComponent<RectTransform>();
            _cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            _cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            _cardRect.pivot = new Vector2(0.5f, 0.5f);
            _cardRect.anchoredPosition = Vector2.zero;
            _cardRect.sizeDelta = new Vector2(980f, 760f);

            var cardImg = cardObj.GetComponent<Image>();
            cardImg.color = new Color(0.05f, 0.09f, 0.12f, 0.98f);

            var cardOutline = cardObj.GetComponent<Outline>();
            cardOutline.effectColor = new Color(0.08f, 0.65f, 0.48f, 0.90f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            var cardShadow = cardObj.GetComponent<Shadow>();
            cardShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            cardShadow.effectDistance = new Vector2(0f, -10f);

            // Top accent bar
            var topBarObj = new GameObject("AccentBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            topBarObj.transform.SetParent(cardObj.transform, false);
            var topBarRt = topBarObj.GetComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0f, 1f);
            topBarRt.anchorMax = new Vector2(1f, 1f);
            topBarRt.pivot = new Vector2(0.5f, 1f);
            topBarRt.anchoredPosition = Vector2.zero;
            topBarRt.sizeDelta = new Vector2(0f, 5f);
            topBarObj.GetComponent<Image>().color = new Color(0.20f, 0.82f, 0.60f, 1f);

            // Close Cross Button '✕' in top-right corner
            var closeBtnObj = new GameObject("CloseCrossButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Button));
            closeBtnObj.transform.SetParent(cardObj.transform, false);
            var closeRt = closeBtnObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-20f, -18f);
            closeRt.sizeDelta = new Vector2(44f, 44f);

            var closeImg = closeBtnObj.GetComponent<Image>();
            closeImg.color = new Color(0.12f, 0.18f, 0.22f, 0.95f);
            var closeOutline = closeBtnObj.GetComponent<Outline>();
            closeOutline.effectColor = new Color(0.90f, 0.32f, 0.38f, 0.80f);
            closeOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var crossTextObj = new GameObject("CrossIcon", typeof(RectTransform), typeof(Text));
            crossTextObj.transform.SetParent(closeBtnObj.transform, false);
            var crossRt = crossTextObj.GetComponent<RectTransform>();
            crossRt.anchorMin = Vector2.zero;
            crossRt.anchorMax = Vector2.one;
            crossRt.offsetMin = Vector2.zero;
            crossRt.offsetMax = Vector2.zero;

            var crossTxt = crossTextObj.GetComponent<Text>();
            crossTxt.font = font;
            crossTxt.text = "✕";
            crossTxt.fontSize = 24;
            crossTxt.fontStyle = FontStyle.Bold;
            crossTxt.alignment = TextAnchor.MiddleCenter;
            crossTxt.color = new Color(0.99f, 0.55f, 0.60f, 1f);
            crossTxt.raycastTarget = false;

            _closeCrossButton = closeBtnObj.GetComponent<Button>();
            var closeColors = _closeCrossButton.colors;
            closeColors.highlightedColor = new Color(0.85f, 0.20f, 0.28f, 1f);
            closeColors.pressedColor = new Color(0.60f, 0.10f, 0.18f, 1f);
            _closeCrossButton.colors = closeColors;

            _closeCrossButton.onClick.AddListener(() =>
            {
                PlaySound();
                Hide();
            });

            // Header: Tag + Title + Greeting
            var headerObj = new GameObject("HeaderArea", typeof(RectTransform), typeof(Text));
            headerObj.transform.SetParent(cardObj.transform, false);
            var headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -22f);
            headerRt.sizeDelta = new Vector2(880f, 108f);

            var headerTxt = headerObj.GetComponent<Text>();
            headerTxt.font = font;
            headerTxt.fontSize = 17;
            headerTxt.lineSpacing = 1.15f;
            headerTxt.alignment = TextAnchor.UpperLeft;
            headerTxt.supportRichText = true;
            headerTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            headerTxt.verticalOverflow = VerticalWrapMode.Overflow;
            headerTxt.text =
                "<size=12><color=#34D399><b>● БОТАНИЧЕСКАЯ СТАНЦИЯ · ВВОДНЫЙ РЕГЛАМЕНТ ●</b></color></size>\n" +
                "<size=23><color=#FFFFFF><b>ВВОДНЫЙ ИНСТРУКТАЖ СТАЖЁРА-ЛАБОРАНТА</b></color></size>\n" +
                "<color=#E2E8F0><b>Поздравляем с приёмом на работу стажёром-лаборантом!</b> Вот ваше рабочее место ботанической станции. " +
                "Ниже базовый порядок работы с оборудованием на столе:</color>";

            // Instruction Body Scroll View
            var scrollObj = new GameObject("BodyScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.transform.SetParent(cardObj.transform, false);
            var scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.offsetMin = new Vector2(40f, 85f);
            scrollRt.offsetMax = new Vector2(-40f, -140f);

            var scrollRect = scrollObj.GetComponent<ScrollRect>();

            var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var viewportRt = viewportObj.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportObj.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

            var contentObj = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 500f);

            var csf = contentObj.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layout = contentObj.GetComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 10f;
            layout.padding = new RectOffset(0, 0, 4, 10);

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 35f;

            // 5 Instructional Section Cards with Large, Readable Typography
            AddInstructionCard(contentObj.transform, font,
                "🌾 1. МЕШОК С СЕМЕНАМИ (слева стола)",
                "#FBBF24",
                "Кликните по холщовому мешку с семенами, чтобы получить обычное белое семя лилии.\n" +
                "Новое семечко можно брать в любой момент без ограничений и сразу нести в инкубатор.");

            AddInstructionCard(contentObj.transform, font,
                "🧪 2. ИНКУБАТОР ДЛЯ ПРОРАЩИВАНИЯ (по центру)",
                "#38BDF8",
                "Перетащите семя в слот инкубатора и выберите условия созревания:\n" +
                "• <b>Тумблер «ТЕМПЕРАТУРА»(Красный)</b>: сдвигает оттенок цветка. <i>Холод</i> даёт синий цвет, <i>Тепло</i> — красный.\n" +
                "• <b>Тумблер «ВЛАЖНОСТЬ»(Синий)</b>: управляет морфологией. <i>Влажно</i> развивает форму на 1 ступень вперёд, <i>Сухо</i> — на 1 ступень назад.\n" +
                "• Нажмите <b>кнопку проращивания</b>, чтобы цветок распустился на лотке готовым образцом.");

            AddInstructionCard(contentObj.transform, font,
                "🧬 3. СЕЛЕКТОР СКРЕЩИВАНИЯ (справа стола)",
                "#34D399",
                "Установите два выращенных цветка в чаши скрещивателя и нажмите зелёную кнопку синтеза.\n" +
                "Вы получите гибридное семя нового окраса. <b>Форма будущего цветка наследуется от материнского (левого) растения!</b>");

            AddInstructionCard(contentObj.transform, font,
                "📖 4. ПОЛЕВОЙ ЖУРНАЛ БОТАНИКА (книга на столе)",
                "#A78BFA",
                "Кликните по журналу на столе, чтобы изучить задачи ботаника, список открытых формул скрещивания, а также зафиксированные в опытах фенотипы.");

            AddInstructionCard(contentObj.transform, font,
                "🎯 5. НАУЧНЫЕ ЗАДАНИЯ (панель справа вверху)",
                "#F472B6",
                "Выполняйте исследовательские этапы по очереди. Как только требуемый сорт цветка будет выращен, <b>этап сдаётся и заносится в дневник автоматически</b>!");

            // Bottom Confirm Button: "[ ПРИСТУПИТЬ К ИССЛЕДОВАНИЯМ ]"
            var bottomBtnObj = new GameObject("ConfirmButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Button));
            bottomBtnObj.transform.SetParent(cardObj.transform, false);
            var bottomRt = bottomBtnObj.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0.5f, 0f);
            bottomRt.anchorMax = new Vector2(0.5f, 0f);
            bottomRt.pivot = new Vector2(0.5f, 0f);
            bottomRt.anchoredPosition = new Vector2(0f, 20f);
            bottomRt.sizeDelta = new Vector2(380f, 50f);

            var bottomImg = bottomBtnObj.GetComponent<Image>();
            bottomImg.color = new Color(0.05f, 0.58f, 0.40f, 1f);

            var bottomOutline = bottomBtnObj.GetComponent<Outline>();
            bottomOutline.effectColor = new Color(0.20f, 0.88f, 0.65f, 0.90f);
            bottomOutline.effectDistance = new Vector2(1.5f, -1.5f);

            var bottomTextObj = new GameObject("ButtonText", typeof(RectTransform), typeof(Text));
            bottomTextObj.transform.SetParent(bottomBtnObj.transform, false);
            var bTextRt = bottomTextObj.GetComponent<RectTransform>();
            bTextRt.anchorMin = Vector2.zero;
            bTextRt.anchorMax = Vector2.one;
            bTextRt.offsetMin = Vector2.zero;
            bTextRt.offsetMax = Vector2.zero;

            var bTxt = bottomTextObj.GetComponent<Text>();
            bTxt.font = font;
            bTxt.text = "ПРИСТУПИТЬ К ИССЛЕДОВАНИЯМ";
            bTxt.fontSize = 17;
            bTxt.fontStyle = FontStyle.Bold;
            bTxt.alignment = TextAnchor.MiddleCenter;
            bTxt.color = Color.white;
            bTxt.raycastTarget = false;

            _bottomConfirmButton = bottomBtnObj.GetComponent<Button>();
            var bColors = _bottomConfirmButton.colors;
            bColors.highlightedColor = new Color(0.10f, 0.72f, 0.50f, 1f);
            bColors.pressedColor = new Color(0.04f, 0.45f, 0.30f, 1f);
            _bottomConfirmButton.colors = bColors;

            _bottomConfirmButton.onClick.AddListener(() =>
            {
                PlaySound();
                Hide();
            });

            _backdropRoot.SetActive(false);
        }

        private static void AddInstructionCard(Transform parent, Font font, string title, string accentHex, string description)
        {
            var card = new GameObject("Card_" + title, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            card.transform.SetParent(parent, false);

            var img = card.GetComponent<Image>();
            img.color = new Color(0.03f, 0.06f, 0.09f, 0.85f);

            var outline = card.GetComponent<Outline>();
            outline.effectColor = new Color(0.12f, 0.20f, 0.26f, 0.80f);
            outline.effectDistance = new Vector2(1f, -1f);

            var textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(card.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16f, 10f);
            textRt.offsetMax = new Vector2(-16f, -10f);

            var txt = textObj.GetComponent<Text>();
            txt.font = font;
            txt.fontSize = 16;
            txt.lineSpacing = 1.2f;
            txt.alignment = TextAnchor.UpperLeft;
            txt.supportRichText = true;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.color = new Color(0.92f, 0.95f, 0.98f, 1f);
            txt.text = $"<color={accentHex}><b>{title}</b></color>\n{description}";

            var fitter = card.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var le = card.AddComponent<LayoutElement>();
            le.minHeight = 72f;
        }

        public void Show()
        {
            if (_backdropRoot == null) return;

            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(ShowRoutine());
            OnOpened?.Invoke();
        }

        public void Hide()
        {
            if (_backdropRoot == null) return;

            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(HideRoutine());
            OnClosed?.Invoke();
        }

        public void Toggle()
        {
            if (IsOpen) Hide();
            else Show();
        }

        private IEnumerator ShowRoutine()
        {
            _backdropRoot.SetActive(true);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            if (_cardRect != null) _cardRect.localScale = Vector3.one * 0.85f;

            float duration = 0.22f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                if (_canvasGroup != null) _canvasGroup.alpha = t;
                if (_cardRect != null) _cardRect.localScale = Vector3.LerpUnclamped(Vector3.one * 0.85f, Vector3.one, ease);
                yield return null;
            }

            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            if (_cardRect != null) _cardRect.localScale = Vector3.one;
            _animRoutine = null;
        }

        private IEnumerator HideRoutine()
        {
            float duration = 0.16f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (_canvasGroup != null) _canvasGroup.alpha = 1f - t;
                if (_cardRect != null) _cardRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.90f, t);
                yield return null;
            }

            if (_backdropRoot != null) _backdropRoot.SetActive(false);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            _animRoutine = null;
        }

        private void PlaySound()
        {
            if (_clickSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_clickSound);
            }
        }
    }
}
