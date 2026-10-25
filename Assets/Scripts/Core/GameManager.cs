using System;
using System.Collections.Generic;
using System.Linq;
using GardenMerger.Configs;
using GardenMerger.Tabletop;
using GardenMerger.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GardenMerger.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private TabletopBalanceSO _balanceSO;
        [SerializeField] private List<OrderConfigSO> _orders = new List<OrderConfigSO>();
        [SerializeField] private List<BreedingRecipeSO> _recipes = new List<BreedingRecipeSO>();
        [Header("Temperature Control")]
        [SerializeField] private Sprite[] _temperatureLeverSprites;
        [Header("Flower Forms (6, 13, 23, 41, 46)")]
        [SerializeField] private List<Sprite> _flowerFormSprites = new List<Sprite>();
        [SerializeField] private List<Texture2D> _flowerColorMasks = new List<Texture2D>();
        [SerializeField] private Shader _flowerMaskShader;
        [SerializeField] private Sprite _potSprite;

        [Header("Prefabs")]
        [SerializeField] private SeedItem _seedPrefab;
        [SerializeField] private FlowerPot _flowerPotPrefab;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip _clickSound;
        [SerializeField] private AudioClip _itemMoveSound;
        [SerializeField] private AudioClip _notificationSound;
        [SerializeField] private AudioClip _failureSound;
        [SerializeField] private AudioClip _deleteSound;
        [SerializeField] private AudioClip _mergeSound;
        [SerializeField] private AudioClip _flowerCreatedSound;
        [SerializeField] private AudioClip _orderCompletedSound;

        private int _currentOrderIndex;
        private int _orderMergeCount;
        private bool _isCampaignWon;
        private Material _flowerMaskMaterial;
        private AudioSource _audioSource;

        // UI & Scene Bindings
        private TabletopHUD _hud;
        private FieldNotebookUI _notebookUI;
        private VictoryModalUI _victoryModalUI;
        private LabManualModalUI _manualModalUI;
        private IncubatorMachine _incubator;
        private CrossBreederMachine _crossBreeder;
        private WasteChute _wasteChute;
        private TableSlot _orderTray;

        public int CurrentOrderIndex => _currentOrderIndex;
        public int OrderMergeCount => _orderMergeCount;
        public bool IsCampaignWon => _isCampaignWon;
        public OrderConfigSO CurrentOrder => (_orders != null && _currentOrderIndex < _orders.Count) ? _orders[_currentOrderIndex] : null;
        public List<BreedingRecipeSO> Recipes => _recipes;
        public TabletopBalanceSO Balance => _balanceSO;
        public SeedItem SeedPrefab => _seedPrefab;
        public FlowerPot FlowerPotPrefab => _flowerPotPrefab;
        public TabletopHUD HUD => _hud;
        public IncubatorMachine Incubator => _incubator;
        public CrossBreederMachine CrossBreeder => _crossBreeder;
        public LabManualModalUI ManualModal => _manualModalUI;

        public event Action<OrderConfigSO, int> OnOrderUpdated;
        public event Action<string, bool> OnRecipeDiscovered;
        public event Action<OrderConfigSO, bool> OnOrderCompleted;
        public event Action<int, int, int> OnCampaignCompleted; // ordersCount, discoveredCount, totalRecipes

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;

            EnsureFlowerMaskMaterial();

            ResolveLegacyBootstrapData();

            GameTextStyle.ApplyToScene(gameObject.scene);
            SetupCamera();
            BindSceneObjects();
            WireGameEvents();

            _currentOrderIndex = 0;
            _orderMergeCount = 0;
            _isCampaignWon = false;

            NotifyOrderChanged();

            _manualModalUI?.StartTutorial(BuildStartupTutorialSteps());
        }

        private List<LabManualModalUI.TutorialStep> BuildStartupTutorialSteps()
        {
            var journalObject = FindJournalSceneObject();
            return new List<LabManualModalUI.TutorialStep>
            {
                new LabManualModalUI.TutorialStep(
                    FindSceneTransform("Pouch_White"),
                    "Мешок с семенами",
                    "Нажмите на мешок, чтобы получить обычное семечко. Семена можно брать без ограничений; перетащите одно в инкубатор."),
                new LabManualModalUI.TutorialStep(
                    _incubator != null ? _incubator.transform : null,
                    "Инкубатор",
                    "Положите семечко в слот. Холод дает синий, тепло — красный цветок. Влажность меняет форму: влажно — на ступень вперед, сухо — назад. Потяните рычаг, чтобы вырастить цветок."),
                new LabManualModalUI.TutorialStep(
                    _crossBreeder != null ? _crossBreeder.transform : null,
                    "Опылитель",
                    "Положите два цветка в чаши и нажмите зеленую кнопку синтеза. Появится гибридное семя; форма будущего цветка наследуется от левого растения."),
                new LabManualModalUI.TutorialStep(
                    journalObject != null ? journalObject.transform : null,
                    "Полевой журнал",
                    "Откройте книгу, чтобы посмотреть открытые формулы скрещивания, задачи и историю фенотипов."),
                new LabManualModalUI.TutorialStep(
                    FindSceneTransform("OrderPanel"),
                    "Научные задания",
                    "Следите за текущим заданием на панели. Вырастите требуемый сорт цветка, чтобы выполнить этап.")
            };
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            if (_flowerMaskMaterial != null)
            {
                Destroy(_flowerMaskMaterial);
            }
        }

        public void Initialize(List<OrderConfigSO> orders, List<BreedingRecipeSO> recipes)
        {
            _orders = orders ?? new List<OrderConfigSO>();
            _recipes = recipes ?? new List<BreedingRecipeSO>();
            _currentOrderIndex = 0;
            _orderMergeCount = 0;
            _isCampaignWon = false;

            if (_notebookUI != null)
            {
                _notebookUI.SetJournalData(_orders, _recipes, this);
            }

            NotifyOrderChanged();
        }

        private void ResolveLegacyBootstrapData()
        {
            var legacyBootstrap = FindAnyObjectByType<GameBootstrap>();
            if (legacyBootstrap == null) return;

            if (_balanceSO == null && legacyBootstrap.BalanceSO != null)
            {
                _balanceSO = legacyBootstrap.BalanceSO;
            }

            if ((_orders == null || _orders.Count == 0) && legacyBootstrap.OrdersSO != null && legacyBootstrap.OrdersSO.Count > 0)
            {
                _orders = new List<OrderConfigSO>(legacyBootstrap.OrdersSO);
            }

            if ((_recipes == null || _recipes.Count == 0) && legacyBootstrap.RecipesSO != null && legacyBootstrap.RecipesSO.Count > 0)
            {
                _recipes = new List<BreedingRecipeSO>(legacyBootstrap.RecipesSO);
            }
        }

        private bool SetupCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("SimpleScene is missing its Main Camera.", this);
                return false;
            }

            camera.orthographic = true;
            if (camera.GetComponent("PixelPerfectCamera") == null)
            {
                camera.orthographicSize = 5.4f;
            }
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.035f, 0.075f, 0.065f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            return true;
        }

        private bool BindSceneObjects()
        {
            var incubatorObject = FindSceneObject("Incubator");
            var pollinatorObject = FindSceneObject("CrossBreeder");
            _incubator = incubatorObject != null ? incubatorObject.GetComponent<IncubatorMachine>() : null;
            _crossBreeder = pollinatorObject != null ? pollinatorObject.GetComponent<CrossBreederMachine>() : null;

            float sproutDuration = _balanceSO != null ? _balanceSO.SproutDurationSec : 0.45f;
            float synthDuration = _balanceSO != null ? _balanceSO.SynthesisDurationSec : 0.60f;

            if (_incubator != null)
            {
                _incubator.Setup(
                    FindSceneComponent<TableSlot>("SeedCrucible"),
                    FindSceneComponent<TableSlot>("OutputTray"),
                    FindSceneTransform("SproutLever"),
                    null,
                    sproutDuration);

                SetupTemperatureControl();
                SetupSproutLever();
            }

            if (_crossBreeder != null)
            {
                _crossBreeder.Setup(
                    FindSceneComponent<TableSlot>("ParentSlotA"),
                    FindSceneComponent<TableSlot>("ParentSlotB"),
                    FindSceneComponent<TableSlot>("SeedDish"),
                    FindSceneTransform("SynthesisButton"),
                    null,
                    synthDuration);

                SetupSynthesisButton();
            }

            var chuteObject = FindSceneObject("WasteChute");
            _wasteChute = chuteObject != null ? chuteObject.GetComponent<WasteChute>() : null;
            if (_wasteChute != null)
            {
                _wasteChute.OnItemDisposed += _ => PlaySound(_deleteSound);
            }
            _orderTray = FindSceneComponent<TableSlot>("OrderDeliveryTray");

            foreach (var pouch in FindObjectsByType<InfiniteSeedPouch>(FindObjectsInactive.Exclude))
            {
                if (pouch.gameObject.scene == gameObject.scene)
                {
                    pouch.OnSeedRequested += (color, position) => SpawnSeed(color, position);
                }
            }

            var canvasObject = FindSceneObject("MainCanvas");
            var bannerObject = FindSceneObject("BannerToast");
            _hud = canvasObject != null ? canvasObject.GetComponentInChildren<TabletopHUD>(true) : null;
            if (_hud != null)
            {
                _hud.Setup(
                    FindSceneComponent<Text>("OrderTitle"),
                    FindSceneComponent<Text>("ClientRole"),
                    FindSceneComponent<Text>("TargetColor"),
                    FindSceneComponent<Text>("PoeticHint"),
                    FindSceneComponent<Text>("MergeCounter"),
                    null,
                    bannerObject,
                    FindSceneComponent<Text>("BTitle"),
                    FindSceneComponent<Text>("BSub"),
                    FindSceneComponent<Button>("NotebookBtn"),
                    null);
            }

            DragTooltipUI.EnsureInstance();

            var notebookObject = FindSceneObject("NotebookModal");
            _notebookUI = notebookObject != null ? notebookObject.GetComponent<FieldNotebookUI>() : null;
            if (_notebookUI != null)
            {
                _notebookUI.Setup(
                    notebookObject,
                    FindSceneComponent<Text>("NBTitle"),
                    FindSceneComponent<Text>("OrdersList"),
                    FindSceneComponent<Text>("RecipesCounter"),
                    FindSceneComponent<Text>("RecipesList"),
                    FindSceneComponent<Button>("CloseNBBtn"));
                _notebookUI.SetJournalData(_orders, _recipes, this);

                SetupJournal();

                if (_hud != null)
                {
                    _hud.OnNotebookClicked += () =>
                    {
                        PlaySound(_clickSound);
                        _notebookUI.Toggle();
                    };
                }

                _notebookUI.OnClosed += () => PlaySound(_clickSound);
            }

            var victoryObject = FindSceneObject("VictoryModal");
            _victoryModalUI = victoryObject != null ? victoryObject.GetComponent<VictoryModalUI>() : null;
            if (_victoryModalUI != null)
            {
                _victoryModalUI.Setup(
                    victoryObject,
                    FindSceneComponent<Text>("VicTitle"),
                    FindSceneComponent<Text>("VicStats"),
                    FindSceneComponent<Button>("SandboxBtn"),
                    FindSceneComponent<Button>("RestartBtn"));
                _victoryModalUI.OnRestartCampaign += RestartCampaign;
                _victoryModalUI.OnRestartCampaign += () => PlaySound(_clickSound);
                _victoryModalUI.OnContinueSandbox += () => PlaySound(_clickSound);
            }

            var canvasTransform = canvasObject != null ? canvasObject.transform : FindFirstObjectByType<Canvas>()?.transform;
            if (canvasTransform != null)
            {
                _manualModalUI = LabManualModalUI.EnsureInstance(canvasTransform, _clickSound);
            }

            return true;
        }

        private void SetupTemperatureControl()
        {
            if (_incubator == null) return;

            // Find lever object (the toggle switch with 3 states)
            var leverObj = FindSceneObject("TemperatureControl") ??
                           FindSceneObject("Тумблер_анимация_0") ??
                           FindSceneObject("Тумблер") ??
                           FindSceneObject("TemperatureLever");

            // Find indicator object (the lamp next to the lever that changes color)
            var indicatorObj = FindSceneObject("Indicator") ??
                               FindSceneObject("Индикатор") ??
                               FindSceneObject("TemperatureIndicator");

            // If leverObj currently has the round lamp sprite (SimpleSceneSprite20) and a separate tumbler exists, swap them:
            var tumblerObj = FindSceneObject("Тумблер_анимация_0") ?? FindSceneObject("Тумблер");
            if (tumblerObj != null && tumblerObj != leverObj)
            {
                if (indicatorObj == null)
                {
                    indicatorObj = leverObj;
                }
                leverObj = tumblerObj;
            }

            var leverRenderer = leverObj != null ? leverObj.GetComponent<SpriteRenderer>() : null;
            var indicatorRenderer = indicatorObj != null ? indicatorObj.GetComponent<SpriteRenderer>() : null;
            var label = FindSceneComponent<TextMesh>("TemperatureModeLabel");

            var sprites = GetTemperatureLeverSprites(leverRenderer);

            void UpdateVisuals(TemperatureMode mode)
            {
                int spriteIndex = 1;
                Color indColor = Color.white;
                string labelStr = "НЕЙТРАЛЬНО";

                switch (mode)
                {
                    case TemperatureMode.Warm:
                        spriteIndex = 2;
                        indColor = new Color(0.95f, 0.35f, 0.2f);
                        labelStr = "ТЕПЛО";
                        break;
                    case TemperatureMode.Cold:
                        spriteIndex = 0;
                        indColor = new Color(0.35f, 0.75f, 1.0f);
                        labelStr = "ХОЛОДНО";
                        break;
                    default:
                        spriteIndex = 1;
                        indColor = Color.white;
                        labelStr = "НЕЙТРАЛЬНО";
                        break;
                }

                if (label != null)
                {
                    label.text = labelStr;
                }

                if (leverRenderer != null)
                {
                    if (sprites != null && spriteIndex < sprites.Length && sprites[spriteIndex] != null)
                    {
                        leverRenderer.sprite = sprites[spriteIndex];
                    }
                    leverRenderer.color = Color.white;
                }

                if (indicatorRenderer != null)
                {
                    indicatorRenderer.color = indColor;
                }
            }

            if (leverObj != null)
            {
                var col = leverObj.GetComponent<Collider2D>();
                if (col == null)
                {
                    var box = leverObj.AddComponent<BoxCollider2D>();
                    box.size = new Vector2(1.2f, 1.2f);
                    box.isTrigger = true;
                }

                var receiver = GetOrAddClickReceiver(leverObj);
                receiver.OnClick = null;
                receiver.OnClick += () =>
                {
                    PlaySound(_clickSound);
                    _incubator.CycleTemperature();
                };
            }

            if (indicatorObj != null && indicatorObj != leverObj)
            {
                var indReceiver = GetOrAddClickReceiver(indicatorObj);
                indReceiver.OnClick = null;
                indReceiver.OnClick += () =>
                {
                    PlaySound(_clickSound);
                    _incubator.CycleTemperature();
                };
            }

            _incubator.OnTemperatureChanged += mode =>
            {
                UpdateVisuals(mode);

                string tempName = GetTemperatureModeDisplayName(mode);
                string humName = GetHumidityModeDisplayName(_incubator.CurrentHumidityMode);
                string effectDesc = mode switch
                {
                    TemperatureMode.Warm => "Сдвиг оттенка в тёплую сторону (красный)",
                    TemperatureMode.Cold => "Сдвиг оттенка в холодную сторону (синий)",
                    _ => "Базовый оттенок цветка (без термо-сдвига)"
                };

                _notebookUI?.RecordObservation($"Температурный режим переключён: {GetTemperatureModeName(mode)}; {effectDesc}.");

                _hud?.ShowBanner(
                    $"ТЕМПЕРАТУРА: {tempName.ToUpper()}",
                    $"{effectDesc}.  [Температура: <b>{tempName}</b> | Влажность: <b>{humName}</b>]",
                    2.5f,
                    BannerType.Info);
            };

            UpdateVisuals(_incubator.CurrentTemperatureMode);

            EnsureControlHeader(leverObj, "ТЕМП", new Color(1f, 0.95f, 0.90f, 0.95f));

            SetupHumidityControl(leverObj, indicatorObj, sprites);
        }

        private void SetupHumidityControl(GameObject tempLeverObj, GameObject tempIndicatorObj, Sprite[] sprites)
        {
            if (_incubator == null) return;

            // Find or dynamically clone humidity lever object next to temperature lever
            var humLeverObj = FindSceneObject("HumidityControl") ??
                              FindSceneObject("HumidityLever") ??
                              FindSceneObject("Тумблер_влажности");

            if (humLeverObj == null && tempLeverObj != null)
            {
                humLeverObj = Instantiate(tempLeverObj, tempLeverObj.transform.parent);
                humLeverObj.name = "HumidityControl";
                humLeverObj.transform.localPosition = tempLeverObj.transform.localPosition + new Vector3(-1.15f, 0f, 0f);
                humLeverObj.transform.localRotation = tempLeverObj.transform.localRotation;
                humLeverObj.transform.localScale = tempLeverObj.transform.localScale;
            }

            // Find or dynamically clone humidity indicator lamp
            var humIndicatorObj = FindSceneObject("HumidityIndicator") ??
                                  FindSceneObject("Индикатор_влажности");

            if (humIndicatorObj == null && tempIndicatorObj != null)
            {
                humIndicatorObj = Instantiate(tempIndicatorObj, tempIndicatorObj.transform.parent);
                humIndicatorObj.name = "HumidityIndicator";
                humIndicatorObj.transform.localPosition = tempIndicatorObj.transform.localPosition + new Vector3(-1.15f, 0f, 0f);
                humIndicatorObj.transform.localRotation = tempIndicatorObj.transform.localRotation;
                humIndicatorObj.transform.localScale = tempIndicatorObj.transform.localScale;
            }

            var humLeverRenderer = humLeverObj != null ? humLeverObj.GetComponent<SpriteRenderer>() : null;
            var humIndicatorRenderer = humIndicatorObj != null ? humIndicatorObj.GetComponent<SpriteRenderer>() : null;
            var humLabel = FindSceneComponent<TextMesh>("HumidityModeLabel");

            EnsureControlHeader(humLeverObj, "ВЛАЖН", new Color(0.85f, 0.95f, 1f, 0.95f));

            void UpdateVisuals(HumidityMode mode)
            {
                int spriteIndex = 1;
                Color indColor = Color.white;
                string labelStr = "НЕЙТРАЛЬНО";

                switch (mode)
                {
                    case HumidityMode.Humid:
                        spriteIndex = 2;
                        indColor = new Color(0.20f, 0.75f, 1.0f); // Водно-голубой
                        labelStr = "ВЛАЖНО";
                        break;
                    case HumidityMode.Dry:
                        spriteIndex = 0;
                        indColor = new Color(0.98f, 0.72f, 0.18f); // Тёплый янтарь
                        labelStr = "СУХО";
                        break;
                    default:
                        spriteIndex = 1;
                        indColor = new Color(0.92f, 0.96f, 0.98f); // Мягкий нейтральный
                        labelStr = "НЕЙТРАЛЬНО";
                        break;
                }

                if (humLabel != null)
                {
                    humLabel.text = labelStr;
                }

                if (humLeverRenderer != null)
                {
                    if (sprites != null && spriteIndex < sprites.Length && sprites[spriteIndex] != null)
                    {
                        humLeverRenderer.sprite = sprites[spriteIndex];
                    }
                    humLeverRenderer.color = Color.white;
                }

                if (humIndicatorRenderer != null)
                {
                    humIndicatorRenderer.color = indColor;
                }
            }

            if (humLeverObj != null)
            {
                var col = humLeverObj.GetComponent<Collider2D>();
                if (col == null)
                {
                    var box = humLeverObj.AddComponent<BoxCollider2D>();
                    box.size = new Vector2(1.2f, 1.2f);
                    box.isTrigger = true;
                }

                var receiver = GetOrAddClickReceiver(humLeverObj);
                receiver.OnClick = null;
                receiver.OnClick += () =>
                {
                    PlaySound(_clickSound);
                    _incubator.CycleHumidity();
                };
            }

            if (humIndicatorObj != null && humIndicatorObj != humLeverObj)
            {
                var indReceiver = GetOrAddClickReceiver(humIndicatorObj);
                indReceiver.OnClick = null;
                indReceiver.OnClick += () =>
                {
                    PlaySound(_clickSound);
                    _incubator.CycleHumidity();
                };
            }

            _incubator.OnHumidityChanged += mode =>
            {
                UpdateVisuals(mode);

                string humName = GetHumidityModeDisplayName(mode);
                string tempName = GetTemperatureModeDisplayName(_incubator.CurrentTemperatureMode);
                string effectDesc = mode switch
                {
                    HumidityMode.Humid => "Развитие формы: сдвиг на 1 ступень вперёд",
                    HumidityMode.Dry => "Регресс формы: сдвиг на 1 ступень назад",
                    _ => "Сохранение исходной формы цветка"
                };

                _notebookUI?.RecordObservation($"Режим влажности переключён: {GetHumidityModeName(mode)}; {effectDesc}.");

                _hud?.ShowBanner(
                    $"ВЛАЖНОСТЬ: {humName.ToUpper()}",
                    $"{effectDesc}.  [Влажность: <b>{humName}</b> | Температура: <b>{tempName}</b>]",
                    2.5f,
                    BannerType.Info);
            };

            UpdateVisuals(_incubator.CurrentHumidityMode);
        }

        private static void EnsureControlHeader(GameObject lever, string text, Color color)
        {
            if (lever == null) return;
            var existing = lever.transform.Find("HeaderLabel");
            if (existing != null) return;

            var labelObj = new GameObject("HeaderLabel");
            labelObj.transform.SetParent(lever.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0.75f, -0.05f);
            var tm = labelObj.AddComponent<TextMesh>();
            tm.font = GameTextStyle.ReadableFont;
            tm.text = text;
            tm.fontSize = 24;
            tm.characterSize = 0.042f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = labelObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 5;
        }

        private Sprite[] GetTemperatureLeverSprites(SpriteRenderer leverRenderer)
        {
            if (_temperatureLeverSprites != null && _temperatureLeverSprites.Length >= 3 && _temperatureLeverSprites[0] != null)
            {
                return _temperatureLeverSprites;
            }

#if UNITY_EDITOR
            var editorSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Тумблер_анимация.png")
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToArray();
            if (editorSprites.Length >= 3)
            {
                _temperatureLeverSprites = editorSprites;
                return _temperatureLeverSprites;
            }
#endif

            if (leverRenderer != null && leverRenderer.sprite != null && leverRenderer.sprite.texture != null)
            {
                var tex = leverRenderer.sprite.texture;
                _temperatureLeverSprites = new Sprite[3]
                {
                    Sprite.Create(tex, new Rect(2f, 0f, 11f, 11f), Vector2.zero, 16f),
                    Sprite.Create(tex, new Rect(18f, 2f, 11f, 9f), Vector2.zero, 16f),
                    Sprite.Create(tex, new Rect(34f, 2f, 11f, 11f), Vector2.zero, 16f)
                };
                return _temperatureLeverSprites;
            }

            return null;
        }

        private void SetupSproutLever()
        {
            var lever = FindSceneObject("SproutLever");
            if (lever == null || _incubator == null) return;

            GetOrAddClickReceiver(lever).OnClick += () =>
            {
                PlaySound(_clickSound);
                if (!_incubator.TryPullLever())
                {
                    PlaySound(_failureSound);
                }
            };
        }

        private void SetupSynthesisButton()
        {
            var button = FindSceneObject("SynthesisButton");
            if (button == null || _crossBreeder == null) return;

            GetOrAddClickReceiver(button).OnClick += () =>
            {
                PlaySound(_clickSound);
                if (_crossBreeder.CanSynthesize())
                {
                    _crossBreeder.TryTriggerSynthesis();
                }
                else
                {
                    PlaySound(_failureSound);
                    _hud?.ShowBanner("Недостаточно родителей!", "Поместите два цветка в отсеки А и Б.");
                }
            };
        }

        private GameObject FindJournalSceneObject()
        {
            var journalObject = FindSceneObject("Журнал") ?? FindSceneObject("Journal") ?? FindSceneObject("Книга");
            if (journalObject != null)
            {
                return journalObject;
            }

            foreach (var sceneTransform in FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (sceneTransform.gameObject.scene != gameObject.scene) continue;
                if (sceneTransform.GetComponent<RectTransform>() != null) continue;

                string lowerName = sceneTransform.name.ToLowerInvariant();
                if (lowerName.Contains("журнал") || lowerName.Contains("journal") || lowerName.Contains("книга"))
                {
                    return sceneTransform.gameObject;
                }
            }

            foreach (var renderer in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                if (renderer.gameObject.scene != gameObject.scene) continue;
                if (renderer.sprite != null)
                {
                    string spriteName = renderer.sprite.name.ToLowerInvariant();
                    if (spriteName.Contains("книга") || spriteName.Contains("журнал"))
                    {
                        return renderer.gameObject;
                    }
                }
            }

            return null;
        }

        private void SetupJournal()
        {
            var notebookBtn = FindSceneObject("NotebookBtn");
            if (notebookBtn != null)
            {
                var btn = notebookBtn.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        PlaySound(_clickSound);
                        _notebookUI?.Toggle();
                    });
                }
                notebookBtn.SetActive(false);
            }

            var journalObject = FindJournalSceneObject();
            if (journalObject != null)
            {
                var col = journalObject.GetComponent<BoxCollider2D>();
                if (col == null)
                {
                    col = journalObject.AddComponent<BoxCollider2D>();
                }
                col.offset = new Vector2(0.32f, 0.55f);
                col.size = new Vector2(1.5f, 2.0f);
                col.isTrigger = true;

                var receiver = GetOrAddClickReceiver(journalObject);
                receiver.OnClick = null;
                receiver.OnClick += () =>
                {
                    PlaySound(_clickSound);
                    _notebookUI?.Toggle();
                };
            }
            else
            {
                Debug.LogWarning("[GameManager] Journal object not found in scene!", this);
            }
        }

        private ClickReceiver GetOrAddClickReceiver(GameObject target)
        {
            var receiver = target.GetComponent<ClickReceiver>();
            return receiver != null ? receiver : target.AddComponent<ClickReceiver>();
        }

        private GameObject FindSceneObject(string objectName)
        {
            foreach (var sceneTransform in FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (sceneTransform.gameObject.scene == gameObject.scene &&
                    sceneTransform.name == objectName)
                {
                    return sceneTransform.gameObject;
                }
            }

            return null;
        }

        private T FindSceneComponent<T>(string objectName) where T : Component
        {
            var sceneObject = FindSceneObject(objectName);
            return sceneObject != null ? sceneObject.GetComponent<T>() : null;
        }

        private Transform FindSceneTransform(string objectName)
        {
            var sceneObject = FindSceneObject(objectName);
            return sceneObject != null ? sceneObject.transform : null;
        }

        private void WireGameEvents()
        {
            OnOrderUpdated += (order, count) =>
            {
                _hud?.UpdateOrder(order, count);
                _notebookUI?.RefreshUI();
            };

            OnOrderCompleted += (order, isFinal) =>
            {
                PlaySound(_orderCompletedSound);
                if (order != null)
                {
                    string form = order.RequiresFlowerForm
                        ? $" Форма «{order.TargetFlowerForm.ToDisplayName()}» подтверждена."
                        : string.Empty;
                    _notebookUI?.RecordObservation($"Изучен вид №{order.OrderId} «{order.ClientTitle}».{form}");
                }
                _notebookUI?.RefreshUI();
                if (!isFinal && order != null)
                {
                    _hud?.ShowBanner($"Исследование #{order.OrderId} завершено!", $"Новый вид успешно выведен и описан в дневнике!");
                }
            };

            OnCampaignCompleted += (ordersCount, discoveredCount, totalRecipes) =>
            {
                _notebookUI?.RefreshUI();
                _hud?.ShowBanner("ИГРА ПРОЙДЕНА!", "Вы разгадали все тайны ботаники!");
                _victoryModalUI?.Show(ordersCount, discoveredCount, totalRecipes);
            };

            OnRecipeDiscovered += (hybridColor, isNew) =>
            {
                if (!isNew) return;

                var color = ColorRegistry.GetColor(hybridColor);
                PlaySound(_notificationSound);
                _hud?.ShowBanner("Тайна разгадана!", $"В журнал записан рецепт: {color.DisplayName} ({color.BotanicalName})!");
                _notebookUI?.RefreshUI();
            };

            if (_incubator != null)
            {
                _incubator.OnFlowerSprouted += (colorId, form) =>
                {
                    var pot = SpawnFlowerPot(colorId, _incubator.OutputTray.transform.position, form);
                    _incubator.OutputTray.AssignItem(pot);
                    PlaySound(_flowerCreatedSound);
                    string temperature = GetTemperatureModeName(_incubator.CurrentTemperatureMode);
                    string humidity = GetHumidityModeName(_incubator.CurrentHumidityMode);
                    _notebookUI?.RecordObservation(
                        $"Выращен цветок: {ColorRegistry.GetColor(colorId).DisplayName}, форма «{form.ToDisplayName()}»; температура — {temperature}, влажность — {humidity}.");

                    // Автоматическая сдача заказа сразу при получении требуемого цветка
                    if (!TrySubmitFlower(pot))
                    {
                        _hud?.ShowBanner("Росток распустился!", $"Получен цветок: {ColorRegistry.GetColor(colorId).DisplayName}");
                    }
                };
            }

            if (_crossBreeder != null)
            {
                _crossBreeder.OnBreedingCompleted += (parentA, parentB, hybridColor, inheritedForm) =>
                {
                    PlaySound(_mergeSound);
                    CheckRecipeDiscovery(parentA, parentB, out _, out bool newlyDiscovered);
                    var seed = SpawnSeed(hybridColor, _crossBreeder.OutputDish.transform.position, inheritedForm);
                    _crossBreeder.OutputDish.AssignItem(seed);

                    if (!newlyDiscovered)
                    {
                        _hud?.ShowBanner("Селекция завершена", $"Получено семечко: {ColorRegistry.GetColor(hybridColor).DisplayName}");
                    }
                };
            }
        }

        public SeedItem SpawnSeed(string colorId, Vector3 position, FlowerForm form = FlowerForm.Flowers6)
        {
            SeedItem seed;
            if (_seedPrefab != null)
            {
                seed = Instantiate(_seedPrefab, position, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning("Seed prefab is not assigned in GameManager! Creating default instance.", this);
                var seedObject = new GameObject($"Seed_{colorId}");
                seedObject.transform.position = position;
                var renderer = seedObject.AddComponent<SpriteRenderer>();
                renderer.sprite = CreateCircleSprite(ColorRegistry.GetColor(colorId).Color);
                seedObject.transform.localScale = new Vector3(0.5f, 0.7f, 1f);
                var collider = seedObject.AddComponent<CircleCollider2D>();
                collider.radius = 0.35f;
                seed = seedObject.AddComponent<SeedItem>();
                seed.SetRenderer(renderer);
            }

            seed.Initialize(colorId, form);
            seed.name = $"Seed_{colorId}";
            seed.SetDeskBounds(_balanceSO != null ? _balanceSO.DeskBounds : new Vector2(8.5f, 4.5f));
            seed.OnItemPickedUp += _ => PlaySound(_itemMoveSound);
            seed.OnItemDropped += item =>
            {
                if (item.CurrentSlot != null)
                {
                    PlaySound(_itemMoveSound);
                }
            };
            seed.OnItemDropped += item =>
            {
                if (_wasteChute != null)
                {
                    _wasteChute.CheckDispose(item);
                }
            };
            return seed;
        }

        public FlowerPot SpawnFlowerPot(string colorId, Vector3 position, FlowerForm form = FlowerForm.Flowers6)
        {
            FlowerPot pot;
            if (_flowerPotPrefab != null)
            {
                pot = Instantiate(_flowerPotPrefab, position, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning("Flower pot prefab is not assigned in GameManager! Creating default instance.", this);
                pot = CreateDefaultFlowerPot(position);
            }

            EnsureFlowerMaskMaterial();
            int formIndex = Mathf.Clamp((int)form, (int)FlowerForm.Flowers6, (int)FlowerForm.Flowers46);
            Sprite flowerSprite = formIndex < _flowerFormSprites.Count ? _flowerFormSprites[formIndex] : null;
            Texture2D colorMask = formIndex < _flowerColorMasks.Count ? _flowerColorMasks[formIndex] : null;
            if (flowerSprite == null || colorMask == null || _flowerMaskMaterial == null)
            {
                Debug.LogError(
                    $"Missing flower visuals at form index {formIndex}. Assign the ordered flower sprites, masks, and mask material on GameManager.",
                    this);
            }
            pot.Initialize(colorId, (FlowerForm)formIndex, flowerSprite, colorMask, _flowerMaskMaterial);
            pot.name = $"FlowerPot_{colorId}";
            pot.SetDeskBounds(_balanceSO != null ? _balanceSO.DeskBounds : new Vector2(8.5f, 4.5f));
            pot.OnItemPickedUp += _ => PlaySound(_itemMoveSound);
            pot.OnItemDropped += item =>
            {
                if (item.CurrentSlot != null)
                {
                    PlaySound(_itemMoveSound);
                }
            };
            pot.OnItemDropped += item =>
            {
                if (_wasteChute != null)
                {
                    _wasteChute.CheckDispose(item);
                }

                if (_orderTray == null ||
                    Vector2.Distance(item.transform.position, _orderTray.transform.position) > _orderTray.SnapRadius)
                {
                    return;
                }

                if (TrySubmitFlower(pot))
                {
                    Destroy(pot.gameObject);
                }
                else
                {
                    PlaySound(_failureSound);
                    if (CurrentOrder != null &&
                        pot.ColorId == CurrentOrder.TargetColorId &&
                        CurrentOrder.RequiresFlowerForm)
                    {
                        _hud?.ShowBanner(
                            "Нужна другая форма!",
                            $"Для исследования требуется форма: {CurrentOrder.TargetFlowerForm.ToDisplayName()}.");
                    }
                    else
                    {
                        _hud?.ShowBanner("Не тот сорт!", "Для исследования требуется другой вид цветка.");
                    }
                }
            };

            return pot;
        }

        private FlowerPot CreateDefaultFlowerPot(Vector3 position)
        {
            var potObject = new GameObject("FlowerPot");
            potObject.transform.position = position;
            potObject.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var potRenderer = potObject.AddComponent<SpriteRenderer>();
            potRenderer.sprite = _potSprite;

            var petalsObject = new GameObject("Petals");
            petalsObject.transform.SetParent(potObject.transform, false);
            petalsObject.transform.localPosition = new Vector3(0f, 0.856f, -0.1f);
            petalsObject.transform.localScale = Vector3.one * 0.7f;
            var petalsRenderer = petalsObject.AddComponent<SpriteRenderer>();

            var collider = potObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.9f, 1.1f);

            var pot = potObject.AddComponent<FlowerPot>();
            pot.SetRenderers(potRenderer, null, petalsRenderer, null);
            return pot;
        }

        private bool EnsureFlowerMaskMaterial()
        {
            if (_flowerMaskMaterial != null)
            {
                return true;
            }

            if (_flowerMaskShader == null)
            {
                Debug.LogError("Flower mask shader is not assigned on GameManager.", this);
                return false;
            }

            _flowerMaskMaterial = new Material(_flowerMaskShader);
            return true;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        private Sprite CreateCircleSprite(Color color)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size / 2f;
            var center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), center) <= radius ? color : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateQuadSprite(Color color)
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        public void IncrementMergeCount()
        {
            _orderMergeCount++;
            NotifyOrderChanged();
        }

        public bool CheckRecipeDiscovery(string parentA, string parentB, out string hybridColor, out bool isNewDiscovery)
        {
            hybridColor = ColorRegistry.CombineColors(parentA, parentB);
            isNewDiscovery = false;

            if (_recipes != null)
            {
                foreach (var recipe in _recipes)
                {
                    bool isGreyDiscovery = hybridColor == FlowerColor.Grey.ToString() &&
                                           recipe.ResultColorId == FlowerColor.Grey.ToString();
                    if (recipe.Matches(parentA, parentB) || isGreyDiscovery)
                    {
                        if (!recipe.IsDiscovered)
                        {
                            recipe.SetDiscovered(true);
                            isNewDiscovery = true;
                            _notebookUI?.RecordObservation(
                                $"Открыто скрещивание цветов {ColorRegistry.GetColor(parentA).DisplayName} × {ColorRegistry.GetColor(parentB).DisplayName}: получен {ColorRegistry.GetColor(hybridColor).DisplayName}.");
                            OnRecipeDiscovered?.Invoke(hybridColor, true);
                        }
                        break;
                    }
                }
            }

            IncrementMergeCount();
            return isNewDiscovery;
        }

        public bool TrySubmitFlower(FlowerPot flower)
        {
            if (flower == null || CurrentOrder == null) return false;

            bool colorMatches = flower.ColorId == CurrentOrder.TargetColorId;
            bool formMatches = !CurrentOrder.RequiresFlowerForm || flower.Form == CurrentOrder.TargetFlowerForm;
            if (colorMatches && formMatches)
            {
                bool isFinalOrder = (_currentOrderIndex == _orders.Count - 1);
                OnOrderCompleted?.Invoke(CurrentOrder, isFinalOrder);

                if (isFinalOrder)
                {
                    _isCampaignWon = true;
                    int discoveredCount = GetDiscoveredRecipesCount();
                    OnCampaignCompleted?.Invoke(_orders.Count, discoveredCount, _recipes.Count);
                }
                else
                {
                    PlaySound(_notificationSound);
                    AdvanceOrder();
                }

                return true;
            }

            return false;
        }

        private static string GetTemperatureModeName(TemperatureMode mode)
        {
            switch (mode)
            {
                case TemperatureMode.Warm:
                    return "высокая";
                case TemperatureMode.Cold:
                    return "низкая";
                default:
                    return "нейтральная";
            }
        }

        private static string GetHumidityModeName(HumidityMode mode)
        {
            switch (mode)
            {
                case HumidityMode.Humid:
                    return "влажный";
                case HumidityMode.Dry:
                    return "сухой";
                default:
                    return "нейтральный";
            }
        }

        public static string GetTemperatureModeDisplayName(TemperatureMode mode)
        {
            switch (mode)
            {
                case TemperatureMode.Warm:
                    return "Тепло";
                case TemperatureMode.Cold:
                    return "Холодно";
                default:
                    return "Нейтрально";
            }
        }

        public static string GetHumidityModeDisplayName(HumidityMode mode)
        {
            switch (mode)
            {
                case HumidityMode.Humid:
                    return "Влажно";
                case HumidityMode.Dry:
                    return "Сухо";
                default:
                    return "Нейтрально";
            }
        }

        public void AdvanceOrder()
        {
            if (_orders == null || _orders.Count == 0) return;

            _currentOrderIndex = (_currentOrderIndex + 1) % _orders.Count;
            _orderMergeCount = 0;
            NotifyOrderChanged();
        }

        public void RestartCampaign()
        {
            _currentOrderIndex = 0;
            _orderMergeCount = 0;
            _isCampaignWon = false;

            if (_recipes != null)
            {
                foreach (var r in _recipes) r.SetDiscovered(false);
            }

            NotifyOrderChanged();
        }

        public int GetDiscoveredRecipesCount()
        {
            if (_recipes == null) return 0;
            int count = 0;
            foreach (var r in _recipes) if (r.IsDiscovered) count++;
            return count;
        }

        private void NotifyOrderChanged()
        {
            OnOrderUpdated?.Invoke(CurrentOrder, _orderMergeCount);
        }
    }
}
