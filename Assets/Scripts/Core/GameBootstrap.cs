using System.Collections.Generic;
using GardenMerger.Configs;
using GardenMerger.Tabletop;
using GardenMerger.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GardenMerger.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Optional Overrides")]
        [SerializeField] private TabletopBalanceSO _balanceSO;
        [SerializeField] private List<OrderConfigSO> _ordersSO;
        [SerializeField] private List<BreedingRecipeSO> _recipesSO;

        private Camera _mainCamera;
        private Canvas _mainCanvas;
        private TabletopHUD _hud;
        private FieldNotebookUI _notebookUI;
        private VictoryModalUI _victoryModalUI;
        private IncubatorMachine _incubator;
        private CrossBreederMachine _crossBreeder;
        private WasteChute _wasteChute;
        private TableSlot _orderTray;

        private void Awake()
        {
            SetupCamera();
            InitializeConfigs();
            CreateDeskEnvironment();
            CreateApparatuses();
            CreateSeedPouches();
            CreateUI();
            WireGameEvents();
        }

        private void SetupCamera()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                var camGo = new GameObject("Main Camera");
                _mainCamera = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }

            _mainCamera.orthographic = true;
            _mainCamera.orthographicSize = 5.4f; // 1920x1080 framing
            _mainCamera.transform.position = new Vector3(0, 0, -10f);
            _mainCamera.backgroundColor = new Color(0.035f, 0.075f, 0.065f);
            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private void InitializeConfigs()
        {
            if (_balanceSO == null)
            {
                _balanceSO = ScriptableObject.CreateInstance<TabletopBalanceSO>();
            }

            if (_ordersSO == null || _ordersSO.Count == 0)
            {
                _ordersSO = new List<OrderConfigSO>
                {
                    CreateOrder(1, "Леди Элеонора", "Придворная дама",
                        "«Для бального платья мне нужен цветок с нежным румянцем — словно чистое белое молоко озарилось первым алым лучом зари...»", "Pink", 1),
                    CreateOrder(2, "Аптекарь Грим", "Мастер трав",
                        "«Для сонной настойки нужен лепесток цвета морозного рассвета — когда пушистый снег тает в бездонной синеве чистого неба.»", "SkyBlue", 1),
                    CreateOrder(3, "Профессор Вэнс", "Глава Академии",
                        "«Королевской кафедре требуется цветок глубоких сумерек — тот миг, когда пламенное солнце заката тонет в прохладном синем океане ночи.»", "Purple", 1),
                    CreateOrder(4, "Герцогиня Женевьева", "Покровительница искусств",
                        "«Я грежу о цветке безмятежности. Предание шепчет: его загадочный оттенок рождается от союза девичьего румянца и утренней лазури...»", "Lavender", 2),
                    CreateOrder(5, "Архиботаник Торн", "Главный алхимик",
                        "«Высшая тайна ордена: выведите пламенный имперский сорт. В нем величие бархатной сумеречной мантии должно слиться с первозданной алой страстью.»", "Magenta", 2)
                };
            }

            if (_recipesSO == null || _recipesSO.Count == 0)
            {
                _recipesSO = new List<BreedingRecipeSO>
                {
                    CreateRecipe("Red", "White", "Pink", 1),
                    CreateRecipe("Blue", "Red", "Purple", 1),
                    CreateRecipe("Blue", "White", "SkyBlue", 1),
                    CreateRecipe("Pink", "SkyBlue", "Lavender", 2),
                    CreateRecipe("Purple", "Red", "Magenta", 2),
                    CreateRecipe("Purple", "White", "Lilac", 2),
                    CreateRecipe("Pink", "Red", "Crimson", 2)
                };
            }

            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gm.Initialize(_ordersSO, _recipesSO);
        }

        private OrderConfigSO CreateOrder(int id, string title, string role, string hint, string targetColor, int par)
        {
            var o = ScriptableObject.CreateInstance<OrderConfigSO>();
            o.Initialize(id, title, role, hint, targetColor, par);
            return o;
        }

        private BreedingRecipeSO CreateRecipe(string pA, string pB, string res, int tier)
        {
            var r = ScriptableObject.CreateInstance<BreedingRecipeSO>();
            r.Initialize(pA, pB, res, tier, false);
            return r;
        }

        private void CreateDeskEnvironment()
        {
            CreateDecorativeQuad("GreenhouseBackdrop", Vector3.zero, new Vector3(20f, 12f, 1f),
                new Color(0.035f, 0.075f, 0.065f), -22);

            var tableGo = new GameObject("MahoganyTable");
            var sr = tableGo.AddComponent<SpriteRenderer>();
            sr.sprite = CreateQuadSprite(new Color(0.23f, 0.12f, 0.075f));
            sr.sortingOrder = -20;
            tableGo.transform.localScale = new Vector3(18.5f, 10.5f, 1f);
            tableGo.transform.position = Vector3.zero;

            CreateDecorativeQuad("TableInset", new Vector3(0f, 0f, 0.1f), new Vector3(18.05f, 10.05f, 1f),
                new Color(0.34f, 0.20f, 0.12f), -19);
            CreateDecorativeQuad("TableSurface", new Vector3(0f, 0f, 0.2f), new Vector3(17.8f, 9.8f, 1f),
                new Color(0.27f, 0.15f, 0.095f), -18);

            var grainColor = new Color(0.52f, 0.34f, 0.21f, 0.16f);
            for (int i = 0; i < 7; i++)
            {
                float y = -4.35f + i * 1.35f;
                CreateDecorativeQuad($"WoodGrain_{i}", new Vector3(0f, y, 0.3f), new Vector3(17.55f, 0.025f, 1f),
                    grainColor, -17);
            }

            var trimColor = new Color(0.66f, 0.46f, 0.25f);
            CreateDecorativeQuad("TableTrimTop", new Vector3(0f, 4.92f, 0.3f), new Vector3(17.9f, 0.07f, 1f), trimColor, -16);
            CreateDecorativeQuad("TableTrimBottom", new Vector3(0f, -4.92f, 0.3f), new Vector3(17.9f, 0.07f, 1f), trimColor, -16);
            CreateDecorativeQuad("TableTrimLeft", new Vector3(-8.92f, 0f, 0.3f), new Vector3(0.07f, 9.8f, 1f), trimColor, -16);
            CreateDecorativeQuad("TableTrimRight", new Vector3(8.92f, 0f, 0.3f), new Vector3(0.07f, 9.8f, 1f), trimColor, -16);

            CreateGreenhousePlant("FernLeft", new Vector3(-8.25f, -4.35f, 0.4f), -1f);
            CreateGreenhousePlant("FernRight", new Vector3(8.25f, -4.35f, 0.4f), 1f);
            CreateGreenhousePlant("VineLeft", new Vector3(-8.25f, 4.35f, 0.4f), -1f);
            CreateGreenhousePlant("VineRight", new Vector3(8.25f, 4.35f, 0.4f), 1f);

            CreateWorldLabel("INCUBATOR_LABEL", "ИНКУБАТОР", new Vector3(-5.5f, 2.15f, -0.2f));
            CreateWorldLabel("BREEDER_LABEL", "СЕЛЕКЦИЯ", new Vector3(1.8f, 1.5f, -0.2f));
            CreateWorldLabel("SEED_LABEL", "СЕМЕНА", new Vector3(-4.2f, -4.75f, -0.2f));
            CreateWorldLabel("ORDER_LABEL", "ЗАКАЗЫ", new Vector3(3.6f, 2.75f, -0.2f));
            CreateWorldLabel("WASTE_LABEL", "ОТХОДЫ", new Vector3(7.2f, 2.75f, -0.2f));
        }

        private void CreateDecorativeQuad(string name, Vector3 position, Vector3 scale, Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.localScale = scale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateQuadSprite(color);
            renderer.sortingOrder = sortingOrder;
        }

        private void CreateGreenhousePlant(string name, Vector3 position, float direction)
        {
            var plant = new GameObject(name);
            plant.transform.position = position;

            var pot = new GameObject("TerracottaPot");
            pot.transform.SetParent(plant.transform, false);
            pot.transform.localPosition = new Vector3(0f, -0.18f, 0f);
            pot.transform.localScale = new Vector3(0.7f, 0.34f, 1f);
            var potRenderer = pot.AddComponent<SpriteRenderer>();
            potRenderer.sprite = CreateCircleSprite(new Color(0.58f, 0.28f, 0.16f));
            potRenderer.sortingOrder = -15;

            for (int i = 0; i < 3; i++)
            {
                var leaf = new GameObject($"Leaf_{i}");
                leaf.transform.SetParent(plant.transform, false);
                leaf.transform.localPosition = new Vector3(direction * (0.16f + i * 0.12f), i * 0.12f, 0.1f);
                leaf.transform.localRotation = Quaternion.Euler(0f, 0f, direction * (25f + i * 15f));
                leaf.transform.localScale = new Vector3(0.24f, 0.58f - i * 0.08f, 1f);

                var leafRenderer = leaf.AddComponent<SpriteRenderer>();
                leafRenderer.sprite = CreateCircleSprite(i == 1
                    ? new Color(0.31f, 0.55f, 0.34f)
                    : new Color(0.20f, 0.40f, 0.28f));
                leafRenderer.sortingOrder = -14;
            }
        }

        private void CreateWorldLabel(string name, string content, Vector3 position)
        {
            var label = new GameObject(name);
            label.transform.position = position;

            var text = label.AddComponent<TextMesh>();
            text.text = content;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 32;
            text.characterSize = 0.08f;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(0.91f, 0.77f, 0.53f);

            var renderer = label.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 2;
        }

        private void CreateApparatuses()
        {
            // 1. Incubator
            var incGo = new GameObject("Incubator");
            incGo.transform.position = new Vector3(-5.5f, -0.2f, 0);
            var incubatorBody = new GameObject("IncubatorBody");
            incubatorBody.transform.SetParent(incGo.transform, false);
            var incSr = incubatorBody.AddComponent<SpriteRenderer>();
            incSr.sprite = CreateQuadSprite(new Color(0.45f, 0.30f, 0.15f));
            incubatorBody.transform.localScale = new Vector3(3.4f, 4.8f, 1f);

            var seedSlotGo = CreateSlotObject("SeedCrucible", incGo.transform, new Vector3(-0.65f, 0.75f, -0.1f), SlotType.IncubatorSeedInput, 0.9f);
            var outputTrayGo = CreateSlotObject("OutputTray", incGo.transform, new Vector3(-0.65f, -0.85f, -0.1f), SlotType.IncubatorFlowerOutput, 1.1f);

            var leverGo = new GameObject("LeverKnob");
            leverGo.transform.SetParent(incGo.transform);
            leverGo.transform.localPosition = new Vector3(0.9f, 0.75f, -0.2f);
            var leverCol = leverGo.AddComponent<CircleCollider2D>();
            leverCol.radius = 0.35f;
            var leverSr = leverGo.AddComponent<SpriteRenderer>();
            leverSr.sprite = CreateCircleSprite(new Color(0.75f, 0.12f, 0.15f));
            leverGo.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

            var incComp = incGo.AddComponent<IncubatorMachine>();
            incComp.Setup(seedSlotGo.GetComponent<TableSlot>(), outputTrayGo.GetComponent<TableSlot>(), leverGo.transform, null, _balanceSO.SproutDurationSec);
            _incubator = incComp;

            leverGo.AddComponent<ClickReceiver>().OnClick += () => _incubator.TryPullLever();

            // 2. Cross-Breeder Machine
            var cbGo = new GameObject("CrossBreeder");
            cbGo.transform.position = new Vector3(1.8f, -0.6f, 0);
            var breederBody = new GameObject("CrossBreederBody");
            breederBody.transform.SetParent(cbGo.transform, false);
            var cbSr = breederBody.AddComponent<SpriteRenderer>();
            cbSr.sprite = CreateQuadSprite(new Color(0.35f, 0.25f, 0.15f));
            breederBody.transform.localScale = new Vector3(5.2f, 4.2f, 1f);

            var slotAGo = CreateSlotObject("ParentSlotA", cbGo.transform, new Vector3(-1.25f, 0.25f, -0.1f), SlotType.CrossBreederParentA, 1.1f);
            var slotBGo = CreateSlotObject("ParentSlotB", cbGo.transform, new Vector3(1.25f, 0.25f, -0.1f), SlotType.CrossBreederParentB, 1.1f);
            var dishGo = CreateSlotObject("SeedDish", cbGo.transform, new Vector3(0, -1.05f, -0.1f), SlotType.CrossBreederSeedOutput, 0.9f);

            var buttonGo = new GameObject("SynthesisButton");
            buttonGo.transform.SetParent(cbGo.transform);
            buttonGo.transform.localPosition = new Vector3(0, 0.25f, -0.2f);
            var btnCol = buttonGo.AddComponent<CircleCollider2D>();
            btnCol.radius = 0.4f;
            var btnSr = buttonGo.AddComponent<SpriteRenderer>();
            btnSr.sprite = CreateCircleSprite(new Color(0.85f, 0.15f, 0.18f));
            buttonGo.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

            var cbComp = cbGo.AddComponent<CrossBreederMachine>();
            cbComp.Setup(slotAGo.GetComponent<TableSlot>(), slotBGo.GetComponent<TableSlot>(), dishGo.GetComponent<TableSlot>(), buttonGo.transform, null, _balanceSO.SynthesisDurationSec);
            _crossBreeder = cbComp;

            buttonGo.AddComponent<ClickReceiver>().OnClick += () =>
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

            // 3. Waste Chute
            var chuteGo = new GameObject("WasteChute");
            chuteGo.transform.position = new Vector3(7.2f, 3.8f, 0);
            var chuteSr = chuteGo.AddComponent<SpriteRenderer>();
            chuteSr.sprite = CreateQuadSprite(new Color(0.12f, 0.08f, 0.05f));
            chuteGo.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            var chuteCol = chuteGo.AddComponent<BoxCollider2D>();
            chuteCol.isTrigger = true;
            _wasteChute = chuteGo.AddComponent<WasteChute>();

            // 4. Order Station Tray
            var orderTrayGo = CreateSlotObject("OrderDeliveryTray", null, new Vector3(3.6f, 3.8f, 0), SlotType.OrderSubmissionTray, 1.2f);
            _orderTray = orderTrayGo.GetComponent<TableSlot>();
        }

        private GameObject CreateSlotObject(string name, Transform parent, Vector3 localPos, SlotType type, float snapRadius)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent);
                go.transform.localPosition = localPos;
            }
            else
            {
                go.transform.position = localPos;
            }

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(new Color(0.1f, 0.07f, 0.05f, 0.8f));
            go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            var slot = go.AddComponent<TableSlot>();
            slot.Initialize(type, snapRadius);
            return go;
        }

        private void CreateSeedPouches()
        {
            string[] pouchColors = { "Red", "Blue", "White" };
            float[] xOffsets = { -5.8f, -4.2f, -2.6f };

            for (int i = 0; i < pouchColors.Length; i++)
            {
                string colId = pouchColors[i];
                var pGo = new GameObject($"Pouch_{colId}");
                pGo.transform.position = new Vector3(xOffsets[i], -3.8f, 0);

                var sr = pGo.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite(new Color(0.55f, 0.40f, 0.25f));
                pGo.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

                var badgeGo = new GameObject("ColorBadge");
                badgeGo.transform.SetParent(pGo.transform);
                badgeGo.transform.localPosition = new Vector3(0, 0, -0.1f);
                var bSr = badgeGo.AddComponent<SpriteRenderer>();
                bSr.sprite = CreateCircleSprite(ColorRegistry.GetColor(colId).Color);
                badgeGo.transform.localScale = Vector3.one * 0.45f;

                var col = pGo.AddComponent<CircleCollider2D>();
                col.radius = 0.5f;

                var pouch = pGo.AddComponent<InfiniteSeedPouch>();
                pouch.Initialize(colId);
                pouch.OnSeedRequested += (color, pos) => SpawnSeed(color, pos);
            }
        }

        private void CreateUI()
        {
            var canvasGo = new GameObject("MainCanvas");
            _mainCanvas = canvasGo.AddComponent<Canvas>();
            _mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var canvasScaler = canvasGo.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                var esGo = new GameObject("EventSystem");
                eventSystem = esGo.AddComponent<EventSystem>();
            }

            var legacyInputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyInputModule != null)
            {
                legacyInputModule.enabled = false;
                Destroy(legacyInputModule);
            }

            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            if (inputModule.point == null || inputModule.leftClick == null)
            {
                inputModule.AssignDefaultActions();
            }

            // HUD Component
            var hudGo = new GameObject("TabletopHUD");
            hudGo.transform.SetParent(canvasGo.transform, false);
            _hud = hudGo.AddComponent<TabletopHUD>();

            // Setup basic Text elements for Order
            var orderPanel = CreateUIPanel("OrderPanel", canvasGo.transform, new Vector2(0, 420), new Vector2(600, 150));
            var titleText = CreateUIText("OrderTitle", orderPanel.transform, new Vector2(0, 40), "Заказ #1", 20, Color.black);
            var roleText = CreateUIText("ClientRole", orderPanel.transform, new Vector2(0, 18), "Придворная дама", 14, new Color(0.3f, 0.3f, 0.3f));
            var hintText = CreateUIText("PoeticHint", orderPanel.transform, new Vector2(0, -15), "...", 14, Color.black, new Vector2(540, 50));
            var counterText = CreateUIText("MergeCounter", orderPanel.transform, new Vector2(0, -50), "Скрещиваний: 0 / Пар: 1", 14, Color.black);

            // Banner Toast
            var bannerGo = CreateUIPanel("BannerToast", canvasGo.transform, new Vector2(0, 320), new Vector2(500, 70));
            var bTitle = CreateUIText("BTitle", bannerGo.transform, new Vector2(0, 15), "Уведомление", 18, Color.yellow);
            var bSub = CreateUIText("BSub", bannerGo.transform, new Vector2(0, -12), "Описание", 14, Color.white);
            bannerGo.SetActive(false);

            // Top Buttons
            var nbBtnGo = CreateUIButton("NotebookBtn", canvasGo.transform, new Vector2(-400, 480), "📖 Полевой Журнал");
            var nbBtn = nbBtnGo.GetComponent<Button>();

            _hud.Setup(titleText, roleText, hintText, counterText, null, bannerGo, bTitle, bSub, nbBtn, null);

            // Notebook Modal
            var nbModalGo = CreateUIPanel("NotebookModal", canvasGo.transform, Vector2.zero, new Vector2(1100, 700));
            var nbTitle = CreateUIText("NBTitle", nbModalGo.transform, new Vector2(0, 310), "Полевой журнал ботаника", 24, Color.black);
            CreateUIText("OrdersSectionTitle", nbModalGo.transform, new Vector2(-260, 265), "ЖУРНАЛ ЗАКАЗОВ", 17, new Color(0.34f, 0.25f, 0.15f));
            CreateUIText("RecipesSectionTitle", nbModalGo.transform, new Vector2(280, 265), "ЖУРНАЛ ОТКРЫТИЙ", 17, new Color(0.34f, 0.25f, 0.15f));
            var ordersList = CreateUIText("OrdersList", nbModalGo.transform, new Vector2(-260, 10), "", 14, new Color(0.16f, 0.15f, 0.13f), new Vector2(490, 490));
            ordersList.alignment = TextAnchor.UpperLeft;
            ordersList.verticalOverflow = VerticalWrapMode.Overflow;
            var t1Count = CreateUIText("T1Count", nbModalGo.transform, new Vector2(280, 220), "Открыто формул: 0 из 3", 14, Color.black);
            var t1List = CreateUIText("T1List", nbModalGo.transform, new Vector2(280, 130), "", 14, new Color(0.16f, 0.15f, 0.13f), new Vector2(490, 145));
            t1List.alignment = TextAnchor.UpperLeft;
            t1List.verticalOverflow = VerticalWrapMode.Overflow;
            var t2Count = CreateUIText("T2Count", nbModalGo.transform, new Vector2(280, -55), "Открыто формул: 0 из 4", 14, Color.black);
            var t2List = CreateUIText("T2List", nbModalGo.transform, new Vector2(280, -185), "", 14, new Color(0.16f, 0.15f, 0.13f), new Vector2(490, 220));
            t2List.alignment = TextAnchor.UpperLeft;
            t2List.verticalOverflow = VerticalWrapMode.Overflow;
            var closeBtnGo = CreateUIButton("CloseNBBtn", nbModalGo.transform, new Vector2(450, 310), "✕");

            _notebookUI = nbModalGo.AddComponent<FieldNotebookUI>();
            _notebookUI.Setup(nbModalGo, nbTitle, ordersList, t1Count, t1List, t2Count, t2List, closeBtnGo.GetComponent<Button>());
            _notebookUI.SetJournalData(_ordersSO, _recipesSO, GameManager.Instance);
            nbModalGo.SetActive(false);

            _hud.OnNotebookClicked += () => _notebookUI.Toggle();

            // Victory Modal
            var vicModalGo = CreateUIPanel("VictoryModal", canvasGo.transform, Vector2.zero, new Vector2(620, 340));
            var vicTitle = CreateUIText("VicTitle", vicModalGo.transform, new Vector2(0, 110), "Игра Пройдена!", 26, new Color(0.5f, 0.1f, 0.1f));
            var vicStats = CreateUIText("VicStats", vicModalGo.transform, new Vector2(0, 20), "Все 5 королевских заказов выполнены!", 16, Color.black, new Vector2(500, 80));
            var sandboxBtn = CreateUIButton("SandboxBtn", vicModalGo.transform, new Vector2(-120, -100), "Свободная лаборатория").GetComponent<Button>();
            var restartBtn = CreateUIButton("RestartBtn", vicModalGo.transform, new Vector2(120, -100), "Начать заново").GetComponent<Button>();

            _victoryModalUI = vicModalGo.AddComponent<VictoryModalUI>();
            _victoryModalUI.Setup(vicModalGo, vicTitle, vicStats, sandboxBtn, restartBtn);
            _victoryModalUI.OnRestartCampaign += () => GameManager.Instance.RestartCampaign();
            vicModalGo.SetActive(false);
        }

        private GameObject CreateUIPanel(string name, Transform parent, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.95f, 0.92f, 0.85f, 0.95f);
            return go;
        }

        private Text CreateUIText(string name, Transform parent, Vector2 pos, string content, int fontSize, Color color, Vector2? size = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size ?? new Vector2(300, 30);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private GameObject CreateUIButton(string name, Transform parent, Vector2 pos, string label)
        {
            var go = CreateUIPanel(name, parent, pos, new Vector2(180, 40));
            var btn = go.AddComponent<Button>();
            CreateUIText("Label", go.transform, Vector2.zero, label, 14, Color.black, new Vector2(170, 35));
            return go;
        }

        private void WireGameEvents()
        {
            var gm = GameManager.Instance;

            gm.OnOrderUpdated += (order, count) => _hud.UpdateOrder(order, count);
            gm.OnOrderUpdated += (order, count) => _notebookUI.RefreshUI();

            gm.OnOrderCompleted += (order, isFinal) =>
            {
                _notebookUI.RefreshUI();
                if (!isFinal) _hud.ShowBanner($"Заказ #{order.OrderId} выполнен!", $"{order.ClientTitle} в восторге!");
            };

            gm.OnCampaignCompleted += (ordersCount, discoveredCount, totalRecipes) =>
            {
                _notebookUI.RefreshUI();
                _hud.ShowBanner("ИГРА ПРОЙДЕНА!", "Вы разгадали все тайны ботаники!");
                _victoryModalUI.Show(ordersCount, discoveredCount, totalRecipes);
            };

            gm.OnRecipeDiscovered += (hybridColor, isNew) =>
            {
                if (isNew)
                {
                    var col = ColorRegistry.GetColor(hybridColor);
                    _hud.ShowBanner("✨ Тайна разгадана!", $"В журнал записан рецепт: {col.DisplayName} ({col.BotanicalName})!");
                    _notebookUI.RefreshUI();
                }
            };

            _incubator.OnFlowerSprouted += (colorId) =>
            {
                var pot = SpawnFlowerPot(colorId, _incubator.OutputTray.transform.position);
                _incubator.OutputTray.AssignItem(pot);
                _hud.ShowBanner("Росток распустился!", $"Получен цветок: {ColorRegistry.GetColor(colorId).DisplayName}");
            };

            _crossBreeder.OnBreedingCompleted += (pA, pB, hybridColor) =>
            {
                bool isNew = gm.CheckRecipeDiscovery(pA, pB, out _, out bool newlyDiscovered);
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
            var go = new GameObject($"Seed_{colorId}");
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(ColorRegistry.GetColor(colorId).Color);
            go.transform.localScale = new Vector3(0.5f, 0.7f, 1f);

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;

            var seed = go.AddComponent<SeedItem>();
            seed.SetRenderer(sr);
            seed.Initialize(colorId);
            seed.SetDeskBounds(_balanceSO.DeskBounds);

            seed.OnItemDropped += (item) =>
            {
                _wasteChute.CheckDispose(item);
            };

            return seed;
        }

        public FlowerPot SpawnFlowerPot(string colorId, Vector3 position)
        {
            var go = new GameObject($"FlowerPot_{colorId}");
            go.transform.position = position;

            // Terracotta pot base
            var potSr = go.AddComponent<SpriteRenderer>();
            potSr.sprite = CreateQuadSprite(new Color(0.75f, 0.35f, 0.18f));
            go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            // Petals
            var petalsGo = new GameObject("Petals");
            petalsGo.transform.SetParent(go.transform);
            petalsGo.transform.localPosition = new Vector3(0, 0.35f, -0.1f);
            var petalsSr = petalsGo.AddComponent<SpriteRenderer>();
            petalsSr.sprite = CreateCircleSprite(ColorRegistry.GetColor(colorId).Color);
            petalsGo.transform.localScale = Vector3.one * 0.7f;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 1.1f);

            var pot = go.AddComponent<FlowerPot>();
            pot.SetRenderers(potSr, null, petalsSr, null);
            pot.Initialize(colorId);
            pot.SetDeskBounds(_balanceSO.DeskBounds);

            pot.OnItemDropped += (item) =>
            {
                _wasteChute.CheckDispose(item);

                // Check submission tray
                if (_orderTray != null && Vector2.Distance(item.transform.position, _orderTray.transform.position) <= _orderTray.SnapRadius)
                {
                    bool accepted = GameManager.Instance.TrySubmitFlower(pot);
                    if (accepted)
                    {
                        Destroy(pot.gameObject);
                    }
                    else
                    {
                        _hud.ShowBanner("Не тот сорт!", "Заказчик просит другой оттенок цветка.");
                    }
                }
            };

            return pot;
        }

        private Sprite CreateCircleSprite(Color color)
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size / 2f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist <= radius)
                    {
                        tex.SetPixel(x, y, color);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateQuadSprite(Color color)
        {
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, color);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }

    public class ClickReceiver : MonoBehaviour
    {
        public System.Action OnClick;

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            var hitCollider = GetComponent<Collider2D>();
            var camera = Camera.main;
            if (hitCollider == null || camera == null)
            {
                return;
            }

            Vector3 screenPosition = mouse.position.ReadValue();
            screenPosition.z = -camera.transform.position.z;
            if (hitCollider.OverlapPoint(camera.ScreenToWorldPoint(screenPosition)))
            {
                OnClick?.Invoke();
            }
        }
    }
}
