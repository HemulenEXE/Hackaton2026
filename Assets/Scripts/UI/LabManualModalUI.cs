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
        private Button _bottomConfirmButton;
        private Button _cornerHelpButton;
        private ScrollRect _bodyScrollRect;
        private RectTransform _bodyContent;
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
            Font mainFont = GameTextStyle.ReadableFont;

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
            rt.anchoredPosition = new Vector2(30f, -30f);
            rt.sizeDelta = new Vector2(50f, 50f);

            var img = btnObj.GetComponent<Image>();
            img.color = new Color(0.96f, 0.95f, 0.92f, 1f);

            var outline = btnObj.GetComponent<Outline>();
            outline.effectColor = new Color(0.12f, 0.35f, 0.25f, 1f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var shadow = btnObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.40f);
            shadow.effectDistance = new Vector2(0f, -2f);

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
            txt.fontSize = 28;
            txt.fontStyle = FontStyle.Normal;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.08f, 0.30f, 0.20f, 1f);
            txt.raycastTarget = false;

            var labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(btnObj.transform, false);
            var labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.5f, 0f);
            labelRt.anchorMax = new Vector2(0.5f, 0f);
            labelRt.pivot = new Vector2(0.5f, 1f);
            labelRt.anchoredPosition = new Vector2(0f, -3f);
            labelRt.sizeDelta = new Vector2(80f, 18f);

            var labelTxt = labelObj.GetComponent<Text>();
            labelTxt.font = font;
            labelTxt.text = "СПРАВКА";
            labelTxt.fontSize = 11;
            labelTxt.fontStyle = FontStyle.Normal;
            labelTxt.alignment = TextAnchor.MiddleCenter;
            labelTxt.color = Color.white;
            var labelOutline = labelObj.AddComponent<Outline>();
            labelOutline.effectColor = Color.black;
            labelOutline.effectDistance = new Vector2(1f, -1f);
            labelTxt.raycastTarget = false;

            _cornerHelpButton = btnObj.GetComponent<Button>();
            var colors = _cornerHelpButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.90f, 1f, 0.92f, 1f);
            colors.pressedColor = new Color(0.80f, 0.90f, 0.85f, 1f);
            _cornerHelpButton.colors = colors;

            _cornerHelpButton.onClick.AddListener(() =>
            {
                PlaySound();
                Show();
            });
        }

        private void BuildModalWindow(Transform canvasTransform, Font font)
        {
            // Fullscreen backdrop (clicks outside card close modal)
            _backdropRoot = new GameObject("LabManualBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(Button));
            _backdropRoot.transform.SetParent(canvasTransform, false);
            _backdropRoot.transform.SetAsLastSibling();

            var backdropRt = _backdropRoot.GetComponent<RectTransform>();
            backdropRt.anchorMin = Vector2.zero;
            backdropRt.anchorMax = Vector2.one;
            backdropRt.offsetMin = Vector2.zero;
            backdropRt.offsetMax = Vector2.zero;

            var backdropImg = _backdropRoot.GetComponent<Image>();
            backdropImg.color = new Color(0f, 0f, 0f, 0.70f);

            _canvasGroup = _backdropRoot.GetComponent<CanvasGroup>();

            var backdropBtn = _backdropRoot.GetComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;
            backdropBtn.onClick.AddListener(() =>
            {
                PlaySound();
                Hide();
            });

            // Modal Card Container - 100% OPAQUE LIGHT BACKGROUND, FITS NICELY ON SCREEN
            var cardObj = new GameObject("ModalCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Shadow));
            cardObj.transform.SetParent(_backdropRoot.transform, false);

            _cardRect = cardObj.GetComponent<RectTransform>();
            _cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            _cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            _cardRect.pivot = new Vector2(0.5f, 0.5f);
            _cardRect.anchoredPosition = Vector2.zero;
            _cardRect.sizeDelta = new Vector2(920f, 620f);

            // Prevent clicks inside card from bubbling to backdrop
            var cardBlocker = cardObj.AddComponent<Button>();
            cardBlocker.transition = Selectable.Transition.None;

            var cardImg = cardObj.GetComponent<Image>();
            cardImg.color = new Color(0.97f, 0.96f, 0.94f, 1f); // 100% непрозрачный светлый фон

            var cardOutline = cardObj.GetComponent<Outline>();
            cardOutline.effectColor = new Color(0.12f, 0.28f, 0.20f, 1f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            var cardShadow = cardObj.GetComponent<Shadow>();
            cardShadow.effectColor = new Color(0f, 0f, 0f, 0.50f);
            cardShadow.effectDistance = new Vector2(0f, -8f);

            // Top accent bar
            var topBarObj = new GameObject("AccentBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            topBarObj.transform.SetParent(cardObj.transform, false);
            var topBarRt = topBarObj.GetComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0f, 1f);
            topBarRt.anchorMax = new Vector2(1f, 1f);
            topBarRt.offsetMin = new Vector2(0f, -5f);
            topBarRt.offsetMax = new Vector2(0f, 0f);
            topBarObj.GetComponent<Image>().color = new Color(0.10f, 0.52f, 0.36f, 1f);

            // Header Title strictly contained in card
            var headerObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(Text));
            headerObj.transform.SetParent(cardObj.transform, false);
            var headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -14f);
            headerRt.sizeDelta = new Vector2(-60f, 32f);

            var headerTxt = headerObj.GetComponent<Text>();
            headerTxt.font = font;
            headerTxt.fontSize = 20;
            headerTxt.fontStyle = FontStyle.Normal;
            headerTxt.alignment = TextAnchor.MiddleCenter;
            headerTxt.color = new Color(0.08f, 0.20f, 0.14f, 1f);
            headerTxt.text = "ВВОДНЫЙ ИНСТРУКТАЖ СТАЖЁРА-ЛАБОРАНТА";

            // Body ScrollView strictly contained in card
            var scrollObj = new GameObject("BodyScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.transform.SetParent(cardObj.transform, false);
            var scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(36f, 75f);
            scrollRt.offsetMax = new Vector2(-36f, -52f);

            _bodyScrollRect = scrollObj.GetComponent<ScrollRect>();

            var viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var viewportRt = viewportObj.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = new Vector2(-22f, 0f);
            viewportObj.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);

            var scrollbarTrackObj = new GameObject("ScrollbarTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            scrollbarTrackObj.transform.SetParent(scrollObj.transform, false);
            var scrollbarTrackRt = scrollbarTrackObj.GetComponent<RectTransform>();
            scrollbarTrackRt.anchorMin = new Vector2(1f, 0f);
            scrollbarTrackRt.anchorMax = Vector2.one;
            scrollbarTrackRt.pivot = Vector2.one;
            scrollbarTrackRt.anchoredPosition = new Vector2(-4f, 0f);
            scrollbarTrackRt.sizeDelta = new Vector2(12f, 0f);
            scrollbarTrackObj.GetComponent<Image>().color = new Color(0.12f, 0.20f, 0.23f, 0.95f);

            var scrollbarThumbObj = new GameObject("ScrollbarThumb", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            scrollbarThumbObj.transform.SetParent(scrollbarTrackObj.transform, false);
            var scrollbarThumbRt = scrollbarThumbObj.GetComponent<RectTransform>();
            scrollbarThumbRt.anchorMin = Vector2.zero;
            scrollbarThumbRt.anchorMax = Vector2.one;
            scrollbarThumbRt.offsetMin = new Vector2(2f, 2f);
            scrollbarThumbRt.offsetMax = new Vector2(-2f, -2f);
            var scrollbarThumbImage = scrollbarThumbObj.GetComponent<Image>();
            scrollbarThumbImage.color = new Color(0.20f, 0.82f, 0.60f, 1f);

            var scrollbar = scrollbarTrackObj.AddComponent<Scrollbar>();
            scrollbar.handleRect = scrollbarThumbRt;
            scrollbar.targetGraphic = scrollbarThumbImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };

            var contentObj = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRt = contentObj.GetComponent<RectTransform>();
            _bodyContent = contentRt;
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

            var introBlockObj = new GameObject("HeaderArea", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            introBlockObj.transform.SetParent(contentObj.transform, false);
            var bodyHeaderTxt = introBlockObj.GetComponent<Text>();
            bodyHeaderTxt.font = font;
            bodyHeaderTxt.fontSize = 18;
            bodyHeaderTxt.fontStyle = FontStyle.Normal;
            bodyHeaderTxt.lineSpacing = 1.2f;
            bodyHeaderTxt.alignment = TextAnchor.MiddleCenter;
            bodyHeaderTxt.supportRichText = true;
            bodyHeaderTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyHeaderTxt.verticalOverflow = VerticalWrapMode.Overflow;
            bodyHeaderTxt.text =
                "<size=15><color=#258A66>БОТАНИЧЕСКАЯ СТАНЦИЯ · ВВОДНЫЙ РЕГЛАМЕНТ</color></size>\n" +
                "<color=#25312D>Поздравляем с приёмом на работу стажёром-лаборантом! Вот ваше рабочее место. " +
                "Кликните на мешок с семенами, чтобы получить обычное семечко. Ниже — порядок работы с оборудованием.</color>";
            var headerLayout = introBlockObj.GetComponent<LayoutElement>();
            headerLayout.minHeight = 110f;
            headerLayout.preferredHeight = 110f;

            AddStepItem(contentObj.transform, font,
                "1. МЕШОК С СЕМЕНАМИ (слева на столе)",
                "Кликните на мешок, чтобы получить базовое белое семя. Брать семена можно в любой момент без ограничений и сразу нести в инкубатор.");

            AddStepItem(contentObj.transform, font,
                "2. ИНКУБАТОР ДЛЯ ПРОРАЩИВАНИЯ (по центру)",
                "Перетащите семя в слот инкубатора и настройте режим:\n" +
                "• Тумблер «ТЕМП»: Холодно — синий цветок, Тепло — красный мак.\n" +
                "• Тумблер «ВЛАЖН»: Влажно — развитие формы вперёд, Сухо — назад.\n" +
                "Потяните рычаг проращивания, чтобы семя распустилось в готовый цветок на лотке.");

            AddStepItem(contentObj.transform, font,
                "3. СЕЛЕКТОР СКРЕЩИВАНИЯ (справа на столе)",
                "Установите два цветка в чаши скрещивателя и нажмите зелёную кнопку синтеза.\n" +
                "Форма гибрида наследуется от материнского (левого) цветка!");

            AddStepItem(contentObj.transform, font,
                "4. ПОЛЕВОЙ ЖУРНАЛ БОТАНИКА (книга на столе)",
                "Кликните по книге на столе, чтобы изучить список открытых формул скрещивания, задачи и историю зафиксированных фенотипов.");

            _bodyScrollRect.viewport = viewportRt;
            _bodyScrollRect.content = contentRt;
            _bodyScrollRect.horizontal = false;
            _bodyScrollRect.vertical = true;
            _bodyScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _bodyScrollRect.scrollSensitivity = 35f;
            _bodyScrollRect.verticalScrollbar = scrollbar;
            _bodyScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            _bodyScrollRect.verticalScrollbarSpacing = 5f;


            AddStepItem(contentObj.transform, font,
                "5. НАУЧНЫЕ ЗАДАНИЯ (панель справа вверху)",
                "Выполняйте этапы по очереди. Как только требуемый сорт цветка будет получен, этап зачтётся и занесётся в дневник автоматически!");

            // Bottom Confirm Button: "[ ПРИСТУПИТЬ К РАБОТЕ ]"
            var bottomBtnObj = new GameObject("ConfirmButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Button));
            bottomBtnObj.transform.SetParent(cardObj.transform, false);
            var bottomRt = bottomBtnObj.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0.5f, 0f);
            bottomRt.anchorMax = new Vector2(0.5f, 0f);
            bottomRt.pivot = new Vector2(0.5f, 0f);
            bottomRt.anchoredPosition = new Vector2(0f, 16f);
            bottomRt.sizeDelta = new Vector2(340f, 48f);

            var bottomImg = bottomBtnObj.GetComponent<Image>();
            bottomImg.color = new Color(0.08f, 0.46f, 0.32f, 1f); // Чёткий тёмно-изумрудный

            var bottomOutline = bottomBtnObj.GetComponent<Outline>();
            bottomOutline.effectColor = new Color(0.15f, 0.65f, 0.45f, 1f);
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
            bTxt.text = "ПРИСТУПИТЬ К РАБОТЕ";
            bTxt.fontSize = 16;
            bTxt.fontStyle = FontStyle.Normal;
            bTxt.alignment = TextAnchor.MiddleCenter;
            bTxt.color = Color.white;
            bTxt.raycastTarget = false;

            _bottomConfirmButton = bottomBtnObj.GetComponent<Button>();
            var bColors = _bottomConfirmButton.colors;
            bColors.normalColor = Color.white;
            bColors.highlightedColor = new Color(0.12f, 0.58f, 0.40f, 1f);
            bColors.pressedColor = new Color(0.05f, 0.36f, 0.24f, 1f);
            _bottomConfirmButton.colors = bColors;

            _bottomConfirmButton.onClick.AddListener(() =>
            {
                PlaySound();
                Hide();
            });

            _backdropRoot.SetActive(false);
        }

        private static void AddStepItem(Transform parent, Font font, string title, string description)
        {
            var itemObj = new GameObject(
                "StepItem",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(ContentSizeFitter),
                typeof(VerticalLayoutGroup),
                typeof(LayoutElement),
                typeof(Outline));
            itemObj.transform.SetParent(parent, false);

            var img = itemObj.GetComponent<Image>();
            img.color = Color.white;

            var outline = itemObj.GetComponent<Outline>();
            outline.effectColor = new Color(0.80f, 0.83f, 0.82f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);

            var itemLayout = itemObj.GetComponent<VerticalLayoutGroup>();
            itemLayout.padding = new RectOffset(16, 16, 12, 12);
            itemLayout.childAlignment = TextAnchor.UpperLeft;
            itemLayout.childControlWidth = true;
            itemLayout.childControlHeight = true;
            itemLayout.childForceExpandWidth = true;
            itemLayout.childForceExpandHeight = false;

            itemObj.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            itemObj.GetComponent<LayoutElement>().minHeight = 60f;

            var textObj = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter), typeof(LayoutElement));
            textObj.transform.SetParent(itemObj.transform, false);

            var txt = textObj.GetComponent<Text>();
            txt.font = font;
            txt.fontSize = 17;
            txt.fontStyle = FontStyle.Normal;
            txt.lineSpacing = 1.2f;
            txt.alignment = TextAnchor.UpperLeft;
            txt.supportRichText = true;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            txt.text = string.IsNullOrWhiteSpace(title) ? description : $"<b>{title}</b>\n{description}";

            var textFitter = textObj.GetComponent<ContentSizeFitter>();
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var textLayout = textObj.GetComponent<LayoutElement>();
            textLayout.minHeight = txt.fontSize * txt.lineSpacing;
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
            RefreshBodyLayout();
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            if (_cardRect != null) _cardRect.localScale = Vector3.one * 0.90f;

            float duration = 0.20f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                if (_canvasGroup != null) _canvasGroup.alpha = t;
                if (_cardRect != null) _cardRect.localScale = Vector3.LerpUnclamped(Vector3.one * 0.90f, Vector3.one, ease);
                yield return null;
            }

            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            if (_cardRect != null) _cardRect.localScale = Vector3.one;
            _animRoutine = null;
        }

        private void RefreshBodyLayout()
        {
            if (_bodyContent == null || _bodyScrollRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            RectTransform[] elements = _bodyContent.GetComponentsInChildren<RectTransform>(true);
            for (int i = elements.Length - 1; i >= 0; i--)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(elements[i]);
            }

            Canvas.ForceUpdateCanvases();
            _bodyScrollRect.verticalNormalizedPosition = 1f;
        }

        private IEnumerator HideRoutine()
        {
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (_canvasGroup != null) _canvasGroup.alpha = 1f - t;
                if (_cardRect != null) _cardRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.93f, t);
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
