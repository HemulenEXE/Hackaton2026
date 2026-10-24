using GardenMerger.Configs;
using GardenMerger.Tabletop;
using GardenMerger.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GardenMerger.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private TabletopBalanceSO _balanceSO;
        [SerializeField] private System.Collections.Generic.List<OrderConfigSO> _ordersSO;
        [SerializeField] private System.Collections.Generic.List<BreedingRecipeSO> _recipesSO;

        private TabletopHUD _hud;
        private FieldNotebookUI _notebookUI;
        private VictoryModalUI _victoryModalUI;
        private IncubatorMachine _incubator;
        private CrossBreederMachine _crossBreeder;
        private WasteChute _wasteChute;
        private TableSlot _orderTray;

        private void Awake()
        {
            if (!SetupCamera() || !InitializeGameManager() || !BindSceneObjects())
            {
                enabled = false;
                return;
            }

            WireGameEvents();
        }

        private bool SetupCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("SimpleScene is missing its Main Camera.", this);
                return false;
            }

            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.035f, 0.075f, 0.065f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            return true;
        }

        private bool InitializeGameManager()
        {
            if (_balanceSO == null || _ordersSO == null || _ordersSO.Count == 0 ||
                _recipesSO == null || _recipesSO.Count == 0)
            {
                Debug.LogError("Assign the balance, orders, and recipes assets on GameBootstrap in SimpleScene.", this);
                return false;
            }

            var gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("SimpleScene is missing GameManager.", this);
                return false;
            }

            gameManager.Initialize(_ordersSO, _recipesSO);
            return true;
        }

        private bool BindSceneObjects()
        {
            var incubatorObject = FindSceneObject("Incubator");
            var pollinatorObject = FindSceneObject("CrossBreeder");
            _incubator = incubatorObject != null ? incubatorObject.GetComponent<IncubatorMachine>() : null;
            _crossBreeder = pollinatorObject != null ? pollinatorObject.GetComponent<CrossBreederMachine>() : null;
            if (_incubator == null || _crossBreeder == null)
            {
                Debug.LogError("SimpleScene is missing the incubator or pollinator machine.", this);
                return false;
            }

            _incubator.Setup(
                FindSceneComponent<TableSlot>("SeedCrucible"),
                FindSceneComponent<TableSlot>("OutputTray"),
                FindSceneTransform("SproutLever"),
                null,
                _balanceSO.SproutDurationSec);
            _crossBreeder.Setup(
                FindSceneComponent<TableSlot>("ParentSlotA"),
                FindSceneComponent<TableSlot>("ParentSlotB"),
                FindSceneComponent<TableSlot>("SeedDish"),
                FindSceneTransform("SynthesisButton"),
                null,
                _balanceSO.SynthesisDurationSec);

            SetupTemperatureControl();
            SetupSproutLever();
            SetupSynthesisButton();

            var chuteObject = FindSceneObject("WasteChute");
            _wasteChute = chuteObject != null ? chuteObject.GetComponent<WasteChute>() : null;
            _orderTray = FindSceneComponent<TableSlot>("OrderDeliveryTray");
            if (_wasteChute == null || _orderTray == null)
            {
                Debug.LogError("SimpleScene is missing the waste chute or order tray.", this);
                return false;
            }

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
            if (_hud == null || bannerObject == null)
            {
                Debug.LogError("SimpleScene is missing the HUD or banner.", this);
                return false;
            }

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

            var notebookObject = FindSceneObject("NotebookModal");
            _notebookUI = notebookObject != null ? notebookObject.GetComponent<FieldNotebookUI>() : null;
            var victoryObject = FindSceneObject("VictoryModal");
            _victoryModalUI = victoryObject != null ? victoryObject.GetComponent<VictoryModalUI>() : null;
            if (_notebookUI == null || _victoryModalUI == null)
            {
                Debug.LogError("SimpleScene is missing the notebook or victory modal.", this);
                return false;
            }

            _notebookUI.Setup(
                notebookObject,
                FindSceneComponent<Text>("NBTitle"),
                FindSceneComponent<Text>("OrdersList"),
                FindSceneComponent<Text>("RecipesCounter"),
                FindSceneComponent<Text>("RecipesList"),
                FindSceneComponent<Button>("CloseNBBtn"));
            _notebookUI.SetJournalData(_ordersSO, _recipesSO, GameManager.Instance);
            _hud.OnNotebookClicked += _notebookUI.Toggle;

            _victoryModalUI.Setup(
                victoryObject,
                FindSceneComponent<Text>("VicTitle"),
                FindSceneComponent<Text>("VicStats"),
                FindSceneComponent<Button>("SandboxBtn"),
                FindSceneComponent<Button>("RestartBtn"));
            _victoryModalUI.OnRestartCampaign += () => GameManager.Instance.RestartCampaign();

            return true;
        }

        private void SetupTemperatureControl()
        {
            var control = FindSceneObject("TemperatureControl");
            var renderer = control != null ? control.GetComponent<SpriteRenderer>() : null;
            var label = FindSceneComponent<TextMesh>("TemperatureModeLabel");
            if (control == null || renderer == null || label == null)
            {
                Debug.LogError("SimpleScene is missing the temperature control or label.", this);
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
            if (lever == null)
            {
                Debug.LogError("SimpleScene is missing SproutLever.", this);
                return;
            }

            GetOrAddClickReceiver(lever).OnClick += () => _incubator.TryPullLever();
        }

        private void SetupSynthesisButton()
        {
            var button = FindSceneObject("SynthesisButton");
            if (button == null)
            {
                Debug.LogError("SimpleScene is missing SynthesisButton.", this);
                return;
            }

            GetOrAddClickReceiver(button).OnClick += () =>
            {
                if (_crossBreeder.CanSynthesize())
                {
                    _crossBreeder.TryTriggerSynthesis();
                }
                else
                {
                    _hud.ShowBanner("Недостаточно родителей!", "Поместите два цветка в отсеки А и Б.");
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
            var gameManager = GameManager.Instance;
            gameManager.OnOrderUpdated += (order, count) => _hud.UpdateOrder(order, count);
            gameManager.OnOrderUpdated += (order, count) => _notebookUI.RefreshUI();
            _hud.UpdateOrder(gameManager.CurrentOrder, gameManager.OrderMergeCount);

            gameManager.OnOrderCompleted += (order, isFinal) =>
            {
                _notebookUI.RefreshUI();
                if (!isFinal)
                {
                    _hud.ShowBanner($"Заказ #{order.OrderId} выполнен!", $"{order.ClientTitle} в восторге!");
                }
            };

            gameManager.OnCampaignCompleted += (ordersCount, discoveredCount, totalRecipes) =>
            {
                _notebookUI.RefreshUI();
                _hud.ShowBanner("ИГРА ПРОЙДЕНА!", "Вы разгадали все тайны ботаники!");
                _victoryModalUI.Show(ordersCount, discoveredCount, totalRecipes);
            };

            gameManager.OnRecipeDiscovered += (hybridColor, isNew) =>
            {
                if (!isNew) return;

                var color = ColorRegistry.GetColor(hybridColor);
                _hud.ShowBanner("Тайна разгадана!", $"В журнал записан рецепт: {color.DisplayName} ({color.BotanicalName})!");
                _notebookUI.RefreshUI();
            };

            _incubator.OnFlowerSprouted += (colorId) =>
            {
                var pot = SpawnFlowerPot(colorId, _incubator.OutputTray.transform.position);
                _incubator.OutputTray.AssignItem(pot);
                _hud.ShowBanner("Росток распустился!", $"Получен цветок: {ColorRegistry.GetColor(colorId).DisplayName}");
            };

            _crossBreeder.OnBreedingCompleted += (parentA, parentB, hybridColor) =>
            {
                gameManager.CheckRecipeDiscovery(parentA, parentB, out _, out bool newlyDiscovered);
                var seed = SpawnSeed(hybridColor, _crossBreeder.OutputDish.transform.position);
                _crossBreeder.OutputDish.AssignItem(seed);

                if (!newlyDiscovered)
                {
                    _hud.ShowBanner("Селекция завершена", $"Получено семечко: {ColorRegistry.GetColor(hybridColor).DisplayName}");
                }
            };
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
            seed.SetDeskBounds(_balanceSO.DeskBounds);
            seed.OnItemDropped += item => _wasteChute.CheckDispose(item);
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
            pot.SetDeskBounds(_balanceSO.DeskBounds);
            pot.OnItemDropped += item =>
            {
                _wasteChute.CheckDispose(item);

                if (_orderTray == null ||
                    Vector2.Distance(item.transform.position, _orderTray.transform.position) > _orderTray.SnapRadius)
                {
                    return;
                }

                if (GameManager.Instance.TrySubmitFlower(pot))
                {
                    Destroy(pot.gameObject);
                }
                else
                {
                    _hud.ShowBanner("Не тот сорт!", "Заказчик просит другой цветок.");
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
    }
}
