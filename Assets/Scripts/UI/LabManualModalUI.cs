using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GardenMerger.UI
{
    public class LabManualModalUI : MonoBehaviour
    {
        public sealed class TutorialStep
        {
            public Transform Target { get; }
            public string Title { get; }
            public string Description { get; }

            public TutorialStep(Transform target, string title, string description)
            {
                Target = target;
                Title = title;
                Description = description;
            }
        }

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
        private GameObject _tutorialRoot;
        private RectTransform _tutorialCard;
        private RectTransform _tutorialCanvasRect;
        private Canvas _canvas;
        private CanvasGroup _tutorialCanvasGroup;
        private Text _tutorialTitle;
        private Text _tutorialDescription;
        private Text _tutorialButtonText;
        private Button _tutorialNextButton;
        private List<TutorialStep> _tutorialSteps;
        private int _tutorialStepIndex;
        private Coroutine _tutorialRoutine;

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
            BuildTutorialWindow(canvasTransform, mainFont);
        }

        private void BuildTutorialWindow(Transform canvasTransform, Font font)
        {
            _canvas = canvasTransform.GetComponentInParent<Canvas>();
            _tutorialCanvasRect = canvasTransform as RectTransform;
            if (_tutorialCanvasRect == null)
            {
                Debug.LogError("[LabManualModalUI] Tutorial requires a Canvas RectTransform.", this);
                return;
            }

            _tutorialRoot = new GameObject("TutorialPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            _tutorialRoot.transform.SetParent(canvasTransform, false);
            _tutorialRoot.transform.SetAsLastSibling();
            _tutorialCard = _tutorialRoot.GetComponent<RectTransform>();
            _tutorialCard.anchorMin = new Vector2(0.5f, 0.5f);
            _tutorialCard.anchorMax = new Vector2(0.5f, 0.5f);
            _tutorialCard.pivot = new Vector2(0.5f, 0.5f);
            _tutorialCard.sizeDelta = new Vector2(520f, 360f);
            _tutorialRoot.GetComponent<Image>().color = new Color(0.97f, 0.96f, 0.94f, 1f);
            _tutorialCanvasGroup = _tutorialRoot.GetComponent<CanvasGroup>();
            _tutorialCanvasGroup.alpha = 0f;

            var outline = _tutorialRoot.AddComponent<Outline>();
            outline.effectColor = new Color(0.12f, 0.28f, 0.20f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);
            var shadow = _tutorialRoot.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -5f);

            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(_tutorialRoot.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(20f, -52f);
            titleRect.offsetMax = new Vector2(-20f, -12f);
            _tutorialTitle = titleObj.GetComponent<Text>();
            _tutorialTitle.font = font;
            _tutorialTitle.fontSize = 19;
            _tutorialTitle.fontStyle = FontStyle.Bold;
            _tutorialTitle.alignment = TextAnchor.MiddleCenter;
            _tutorialTitle.color = new Color(0.08f, 0.30f, 0.20f, 1f);
            _tutorialTitle.raycastTarget = false;

            var descriptionObj = new GameObject("Description", typeof(RectTransform), typeof(Text));
            descriptionObj.transform.SetParent(_tutorialRoot.transform, false);
            var descriptionRect = descriptionObj.GetComponent<RectTransform>();
            descriptionRect.anchorMin = new Vector2(0f, 0f);
            descriptionRect.anchorMax = new Vector2(1f, 1f);
            descriptionRect.offsetMin = new Vector2(24f, 68f);
            descriptionRect.offsetMax = new Vector2(-24f, -60f);
            _tutorialDescription = descriptionObj.GetComponent<Text>();
            _tutorialDescription.font = font;
            _tutorialDescription.fontSize = 17;
            _tutorialDescription.lineSpacing = 1.2f;
            _tutorialDescription.alignment = TextAnchor.MiddleCenter;
            _tutorialDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tutorialDescription.verticalOverflow = VerticalWrapMode.Overflow;
            _tutorialDescription.color = new Color(0.10f, 0.12f, 0.11f, 1f);
            _tutorialDescription.raycastTarget = false;

            var buttonObj = new GameObject("NextButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(_tutorialRoot.transform, false);
            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 12f);
            buttonRect.sizeDelta = new Vector2(190f, 38f);
            buttonObj.GetComponent<Image>().color = new Color(0.08f, 0.46f, 0.32f, 1f);
            _tutorialNextButton = buttonObj.GetComponent<Button>();
            _tutorialNextButton.onClick.AddListener(AdvanceTutorial);

            var buttonTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            var buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
            _tutorialButtonText = buttonTextObj.GetComponent<Text>();
            _tutorialButtonText.font = font;
            _tutorialButtonText.fontSize = 15;
            _tutorialButtonText.alignment = TextAnchor.MiddleCenter;
            _tutorialButtonText.color = Color.white;
            _tutorialButtonText.raycastTarget = false;

            _tutorialRoot.SetActive(false);
        }

        public void StartTutorial(IList<TutorialStep> steps)
        {
            if (_tutorialRoot == null || steps == null || steps.Count == 0)
            {
                return;
            }

            _tutorialSteps = new List<TutorialStep>();
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] == null || steps[i].Target == null)
                {
                    Debug.LogWarning($"[LabManualModalUI] Skipping tutorial step {i + 1}: target is missing.", this);
                    continue;
                }
                _tutorialSteps.Add(steps[i]);
            }

            if (_tutorialSteps.Count == 0)
            {
                return;
            }

            if (_tutorialRoutine != null) StopCoroutine(_tutorialRoutine);
            _tutorialStepIndex = 0;
            ShowTutorialStep();
            _tutorialRoot.SetActive(true);
            PositionTutorialPopup();
            _tutorialRoutine = StartCoroutine(FadeTutorial(0f, 1f));
        }

        private void AdvanceTutorial()
        {
            PlaySound();
            if (_tutorialRoutine != null) StopCoroutine(_tutorialRoutine);
            _tutorialRoutine = StartCoroutine(AdvanceTutorialRoutine());
        }

        private IEnumerator AdvanceTutorialRoutine()
        {
            yield return FadeTutorial(1f, 0f);
            _tutorialStepIndex++;
            if (_tutorialSteps == null || _tutorialStepIndex >= _tutorialSteps.Count)
            {
                _tutorialRoot.SetActive(false);
                _tutorialSteps = null;
                _tutorialRoutine = null;
                yield break;
            }

            ShowTutorialStep();
            PositionTutorialPopup();
            yield return FadeTutorial(0f, 1f);
            _tutorialRoutine = null;
        }

        private IEnumerator FadeTutorial(float from, float to)
        {
            const float duration = 0.12f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _tutorialCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            _tutorialCanvasGroup.alpha = to;
        }

        private void ShowTutorialStep()
        {
            TutorialStep step = _tutorialSteps[_tutorialStepIndex];
            _tutorialTitle.text = $"{_tutorialStepIndex + 1}. {step.Title}";
            _tutorialDescription.text = step.Description;
            _tutorialButtonText.text = _tutorialStepIndex == _tutorialSteps.Count - 1 ? "ЗАВЕРШИТЬ" : "ДАЛЕЕ";
        }

        private void LateUpdate()
        {
            if (_tutorialRoot != null && _tutorialRoot.activeSelf)
            {
                PositionTutorialPopup();
            }
        }

        private void PositionTutorialPopup()
        {
            if (_tutorialSteps == null || _tutorialStepIndex >= _tutorialSteps.Count)
            {
                return;
            }

            Transform target = _tutorialSteps[_tutorialStepIndex].Target;
            if (target == null)
            {
                return;
            }

            Camera camera = target is RectTransform
                ? (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null)
                : Camera.main;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, target.position);
            Camera eventCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_tutorialCanvasRect, screenPoint, eventCamera, out Vector2 targetLocal))
            {
                return;
            }

            Vector2 popupSize = _tutorialCard.sizeDelta;
            Vector2 offset = new Vector2(targetLocal.x > 0f ? -popupSize.x * 0.62f : popupSize.x * 0.62f,
                                         targetLocal.y > 0f ? -popupSize.y * 0.72f : popupSize.y * 0.72f);
            Vector2 position = targetLocal + offset;
            Rect canvasBounds = _tutorialCanvasRect.rect;
            position.x = Mathf.Clamp(position.x, canvasBounds.xMin + popupSize.x * 0.5f + 12f, canvasBounds.xMax - popupSize.x * 0.5f - 12f);
            position.y = Mathf.Clamp(position.y, canvasBounds.yMin + popupSize.y * 0.5f + 12f, canvasBounds.yMax - popupSize.y * 0.5f - 12f);
            _tutorialCard.anchoredPosition = position;
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
            headerTxt.text = "БОТАНИЧЕСКАЯ СТАНЦИЯ · ВВОДНЫЙ РЕГЛАМЕНТ";

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
                "<size=25><color=#25312D>ВВОДНЫЙ ИНСТРУКТАЖ СТАЖЁРА-ЛАБОРАНТА</color></size>\n" +
                "<color=#25312D>Поздравляем с приёмом на работу! Ниже — порядок работы с оборудованием ботанической станции.</color>";
            var headerLayout = introBlockObj.GetComponent<LayoutElement>();
            headerLayout.minHeight = 100f;
            headerLayout.preferredHeight = 100f;

            AddStepItem(contentObj.transform, font,
                "1. МЕШОК С СЕМЕНАМИ (слева стола)",
                "Кликните по холщовому мешку с семенами, чтобы получить обычное белое семя лилии.\n" +
                "Новое семечко можно брать в любой момент без ограничений и сразу нести в инкубатор.");

            AddStepItem(contentObj.transform, font,
                "2. ИНКУБАТОР ДЛЯ ПРОРАЩИВАНИЯ (по центру)",
                "Перетащите семя в слот инкубатора и выберите условия созревания:\n" +
                "• <b>Тумблер «ТЕМПЕРАТУРА»(Красный)</b>: сдвигает оттенок цветка. <i>Холод</i> даёт синий цвет, <i>Тепло</i> — красный.\n" +
                "• <b>Тумблер «ВЛАЖНОСТЬ»(Зеленый)</b>: управляет морфологией. <i>Влажно</i> развивает форму на 1 ступень вперёд (тумблер вверх), <i>Сухо</i> — на 1 ступень назад (тумблер вниз).\n" +
                "• Нажмите <b>кнопку проращивания</b>, чтобы цветок распустился на лотке готовым образцом.");

            AddStepItem(contentObj.transform, font,
                "3. СЕЛЕКТОР СКРЕЩИВАНИЯ (справа стола)",
                "Установите два выращенных цветка в чаши скрещивателя и нажмите зелёную кнопку синтеза.\n" +
                "Вы получите гибридное семя нового окраса. <b>Форма будущего цветка наследуется от материнского (левого) растения!</b>");

            AddStepItem(contentObj.transform, font,
                "",
                "Кликните по журналу на столе, чтобы изучить задачи ботаника, список открытых формул скрещивания, а также зафиксированные в опытах фенотипы.");

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
                "Выполняйте исследовательские этапы по очереди. Как только требуемый сорт цветка будет выращен, <b>этап сдаётся и заносится в дневник автоматически</b>!");

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
            bTxt.text = "ПРИСТУПИТЬ К ИССЛЕДОВАНИЯМ";
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

            if (_tutorialRoutine != null) StopCoroutine(_tutorialRoutine);
            _tutorialRoutine = null;
            _tutorialSteps = null;
            if (_tutorialRoot != null) _tutorialRoot.SetActive(false);

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
