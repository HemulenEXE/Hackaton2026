using System;
using System.Collections.Generic;
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

        private int _currentOrderIndex;
        private int _orderMergeCount;
        private bool _isCampaignWon;

        // UI & Scene Bindings
        private TabletopHUD _hud;
        private FieldNotebookUI _notebookUI;
        private VictoryModalUI _victoryModalUI;
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
        public TabletopHUD HUD => _hud;
        public IncubatorMachine Incubator => _incubator;
        public CrossBreederMachine CrossBreeder => _crossBreeder;

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

            ResolveLegacyBootstrapData();

            SetupCamera();
            BindSceneObjects();
            WireGameEvents();

            _currentOrderIndex = 0;
            _orderMergeCount = 0;
            _isCampaignWon = false;

            NotifyOrderChanged();
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

                if (_hud != null)
                {
                    _hud.OnNotebookClicked += _notebookUI.Toggle;
                }
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
            }

            return true;
        }

        private void SetupTemperatureControl()
        {
            var control = FindSceneObject("TemperatureControl");
            var renderer = control != null ? control.GetComponent<SpriteRenderer>() : null;
            var label = FindSceneComponent<TextMesh>("TemperatureModeLabel");
            if (control == null || renderer == null || label == null || _incubator == null)
            {
                return;
            }

            GetOrAddClickReceiver(control).OnClick += () => _incubator.CycleTemperature();
            _incubator.OnTemperatureChanged += mode =>
            {
                switch (mode)
                {
                    case TemperatureMode.Warm:
                        label.text = "ТЕПЛО";
                        renderer.color = new Color(0.9f, 0.3f, 0.2f);
                        break;
                    case TemperatureMode.Cold:
                        label.text = "ХОЛОДНО";
                        renderer.color = new Color(0.35f, 0.7f, 0.95f);
                        break;
                    default:
                        label.text = "НЕЙТРАЛЬНО";
                        renderer.color = Color.white;
                        break;
                }
            };
        }

        private void SetupSproutLever()
        {
            var lever = FindSceneObject("SproutLever");
            if (lever == null || _incubator == null) return;

            GetOrAddClickReceiver(lever).OnClick += () => _incubator.TryPullLever();
        }

        private void SetupSynthesisButton()
        {
            var button = FindSceneObject("SynthesisButton");
            if (button == null || _crossBreeder == null) return;

            GetOrAddClickReceiver(button).OnClick += () =>
            {
                if (_crossBreeder.CanSynthesize())
                {
                    _crossBreeder.TryTriggerSynthesis();
                }
                else
                {
                    _hud?.ShowBanner("Недостаточно родителей!", "Поместите два цветка в отсеки А и Б.");
                }
            };
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
                _notebookUI?.RefreshUI();
                if (!isFinal && order != null)
                {
                    _hud?.ShowBanner($"Заказ #{order.OrderId} выполнен!", $"{order.ClientTitle} в восторге!");
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
                _hud?.ShowBanner("Тайна разгадана!", $"В журнал записан рецепт: {color.DisplayName} ({color.BotanicalName})!");
                _notebookUI?.RefreshUI();
            };

            if (_incubator != null)
            {
                _incubator.OnFlowerSprouted += (colorId) =>
                {
                    var pot = SpawnFlowerPot(colorId, _incubator.OutputTray.transform.position);
                    _incubator.OutputTray.AssignItem(pot);
                    _hud?.ShowBanner("Росток распустился!", $"Получен цветок: {ColorRegistry.GetColor(colorId).DisplayName}");
                };
            }

            if (_crossBreeder != null)
            {
                _crossBreeder.OnBreedingCompleted += (parentA, parentB, hybridColor) =>
                {
                    CheckRecipeDiscovery(parentA, parentB, out _, out bool newlyDiscovered);
                    var seed = SpawnSeed(hybridColor, _crossBreeder.OutputDish.transform.position);
                    _crossBreeder.OutputDish.AssignItem(seed);

                    if (!newlyDiscovered)
                    {
                        _hud?.ShowBanner("Селекция завершена", $"Получено семечко: {ColorRegistry.GetColor(hybridColor).DisplayName}");
                    }
                };
            }
        }

        public SeedItem SpawnSeed(string colorId, Vector3 position)
        {
            var seedObject = new GameObject($"Seed_{colorId}");
            seedObject.transform.position = position;

            var renderer = seedObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateCircleSprite(ColorRegistry.GetColor(colorId).Color);
            seedObject.transform.localScale = new Vector3(0.5f, 0.7f, 1f);

            var collider = seedObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.35f;

            var seed = seedObject.AddComponent<SeedItem>();
            seed.SetRenderer(renderer);
            seed.Initialize(colorId);
            seed.SetDeskBounds(_balanceSO != null ? _balanceSO.DeskBounds : new Vector2(8.5f, 4.5f));
            seed.OnItemDropped += item =>
            {
                if (_wasteChute != null)
                {
                    _wasteChute.CheckDispose(item);
                }
            };
            return seed;
        }

        public FlowerPot SpawnFlowerPot(string colorId, Vector3 position)
        {
            var potObject = new GameObject($"FlowerPot_{colorId}");
            potObject.transform.position = position;

            var potRenderer = potObject.AddComponent<SpriteRenderer>();
            potRenderer.sprite = CreateQuadSprite(new Color(0.75f, 0.35f, 0.18f));
            potObject.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var petalsObject = new GameObject("Petals");
            petalsObject.transform.SetParent(potObject.transform);
            petalsObject.transform.localPosition = new Vector3(0f, 0.35f, -0.1f);
            var petalsRenderer = petalsObject.AddComponent<SpriteRenderer>();
            petalsRenderer.sprite = CreateCircleSprite(ColorRegistry.GetColor(colorId).Color);
            petalsObject.transform.localScale = Vector3.one * 0.7f;

            var collider = potObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.9f, 1.1f);

            var pot = potObject.AddComponent<FlowerPot>();
            pot.SetRenderers(potRenderer, null, petalsRenderer, null);
            pot.Initialize(colorId);
            pot.SetDeskBounds(_balanceSO != null ? _balanceSO.DeskBounds : new Vector2(8.5f, 4.5f));
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
                    _hud?.ShowBanner("Не тот сорт!", "Заказчик просит другой цветок.");
                }
            };

            return pot;
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

            if (flower.ColorId == CurrentOrder.TargetColorId)
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
                    AdvanceOrder();
                }

                return true;
            }

            return false;
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
