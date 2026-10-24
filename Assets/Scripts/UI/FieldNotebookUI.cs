using System;
using System.Collections.Generic;
using GardenMerger.Configs;
using GardenMerger.Core;
using GardenMerger.Tabletop;
using UnityEngine;
using UnityEngine.UI;

namespace GardenMerger.UI
{
    public class FieldNotebookUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _ordersListText;
        [SerializeField] private Text _recipesCounterText;
        [SerializeField] private Text _recipesListText;
        [SerializeField] private Button _closeButton;

        private ScrollRect _ordersScrollRect;
        private ScrollRect _researchScrollRect;
        private List<OrderConfigSO> _orders = new List<OrderConfigSO>();
        private List<BreedingRecipeSO> _recipes = new List<BreedingRecipeSO>();
        private readonly List<string> _observations = new List<string>();
        private GameManager _gameManager;
        private const int MaxObservations = 30;

        public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;
        public event Action OnClosed;

        private void Awake()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }
        }

        public void Setup(GameObject root, Text title, Text ordersList, Text recipesCounter, Text recipesList, Button closeBtn)
        {
            _panelRoot = root;
            _titleText = title;
            _ordersListText = ordersList;
            _recipesCounterText = recipesCounter;
            _recipesListText = recipesList;
            _closeButton = closeBtn;
            _ordersScrollRect = _ordersScrollRect != null
                ? _ordersScrollRect
                : CreateScrollableList(_ordersListText);
            _researchScrollRect = _researchScrollRect != null
                ? _researchScrollRect
                : CreateScrollableList(_recipesListText);
            ConfigureJournalText();

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(Close);
            }
        }

        public void SetJournalData(List<OrderConfigSO> orders, List<BreedingRecipeSO> recipes, GameManager gameManager)
        {
            _orders = orders ?? new List<OrderConfigSO>();
            _recipes = recipes;
            _gameManager = gameManager;
            RefreshUI();
        }

        public void Open()
        {
            if (_panelRoot != null) _panelRoot.SetActive(true);
            RefreshUI();
        }

        public void Close()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
            OnClosed?.Invoke();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void RefreshUI()
        {
            int total = 0;
            int discovered = 0;
            var recipeEntries = new System.Text.StringBuilder();
            if (_recipes != null)
            {
                foreach (var recipe in _recipes)
                {
                    if (recipe == null) continue;

                    total++;
                    if (recipe.IsDiscovered) discovered++;
                    AppendRecipeEntry(recipeEntries, recipe);
                }
            }

            if (_recipesCounterText != null) _recipesCounterText.text = $"Открыто формул: {discovered} из {total}";
            if (_recipesListText != null) _recipesListText.text = BuildResearchNotes(recipeEntries);

            RefreshOrders();
            UpdateScrollContent(_ordersListText, _ordersScrollRect);
            UpdateScrollContent(_recipesListText, _researchScrollRect);
        }

        public void RecordObservation(string observation)
        {
            if (string.IsNullOrWhiteSpace(observation))
            {
                return;
            }

            _observations.Insert(0, observation);
            if (_observations.Count > MaxObservations)
            {
                _observations.RemoveAt(_observations.Count - 1);
            }

            RefreshUI();
        }

        private string BuildResearchNotes(System.Text.StringBuilder recipeEntries)
        {
            var entries = new System.Text.StringBuilder();
            entries.Append("<b>ЛАБОРАТОРНЫЕ ЗАМЕТКИ</b>\n")
                .Append("<color=#315A40>Температура</color>\n")
                .Append("Высокая температура сдвигает оттенок пигмента в тёплую сторону, низкая — в холодную. В модели игры меняется цвет, а не форма.\n\n")
                .Append("<color=#315A40>Наследование формы</color>\n")
                .Append("При скрещивании форма семени наследуется от левого цветка (материнского растения). Цвет результата определяется отдельно по правилам смешения.\n\n")
                .Append("<color=#315A40>Влажность и развитие</color>\n")
                .Append("Порядок морфотипов: лилия (flowers6) → ромашка (flowers13) → цветы на веточке (flowers23) → несколько ромашек (flowers41) → колокольчик (flowers46). В этой лабораторной модели доступность воды служит сигналом развития: влажный режим сдвигает фенотип на одну ступень вперёд, сухой — на одну назад, нейтральный сохраняет форму. На крайних ступенях ряд не зацикливается. Это игровая модель изменения морфологии, а не универсальный закон ботаники.\n\n")
                .Append("<b>ХОД ИССЛЕДОВАНИЯ</b>\n");

            if (_observations.Count == 0)
            {
                entries.Append("Наблюдения появятся здесь по мере опытов и выполненных задач.\n\n");
            }
            else
            {
                foreach (string observation in _observations)
                {
                    entries.Append("• ").Append(observation).Append('\n');
                }

                entries.Append('\n');
            }

            entries.Append("<b>ОТКРЫТЫЕ СКРЕЩИВАНИЯ</b>\n")
                .Append(recipeEntries);
            return entries.ToString();
        }

        private void AppendRecipeEntry(System.Text.StringBuilder entries, BreedingRecipeSO recipe)
        {
            var parentA = ColorRegistry.GetColor(recipe.ParentAColorId);
            var parentB = ColorRegistry.GetColor(recipe.ParentBColorId);
            var result = ColorRegistry.GetColor(recipe.ResultColorId);

            if (recipe.IsDiscovered)
            {
                entries.Append("<color=#28583A>")
                    .Append(parentA.DisplayName).Append(" + ").Append(parentB.DisplayName)
                    .Append(" → ").Append(result.DisplayName)
                    .Append(" · ").Append(result.BotanicalName)
                    .Append("</color>\n");
            }
            else
            {
                entries.Append($"<color=#706A5E>??? + ??? → {result.DisplayName} ({result.BotanicalName})</color>\n");
            }
        }

        private void RefreshOrders()
        {
            if (_ordersListText == null) return;

            if (_orders == null || _orders.Count == 0)
            {
                _ordersListText.text = "Список заказов недоступен.";
                return;
            }

            int currentOrderIndex = _gameManager != null ? _gameManager.CurrentOrderIndex : 0;
            bool campaignWon = _gameManager != null && _gameManager.IsCampaignWon;
            int mergeCount = _gameManager != null ? _gameManager.OrderMergeCount : 0;
            var entries = new System.Text.StringBuilder();

            for (int i = 0; i < _orders.Count; i++)
            {
                var order = _orders[i];
                if (order == null) continue;

                string status;
                string statusColor;
                if (i < currentOrderIndex || (campaignWon && i == currentOrderIndex))
                {
                    status = "ВЫПОЛНЕН";
                    statusColor = "#3B6B43";
                }
                else if (i == currentOrderIndex)
                {
                    status = "В РАБОТЕ";
                    statusColor = "#94651C";
                }
                else
                {
                    status = "ОЖИДАЕТ";
                    statusColor = "#777064";
                }

                var target = ColorRegistry.GetColor(order.TargetColorId);
                entries.Append("<b>Заказ #").Append(order.OrderId).Append(": ")
                    .Append(order.ClientTitle).Append("</b>\n");
                entries.Append("<color=").Append(statusColor).Append(">")
                    .Append(status);
                if (i == currentOrderIndex && !campaignWon)
                {
                    entries.Append(" — скрещиваний: ").Append(mergeCount)
                        .Append(" / ").Append(order.ParMerges);
                }

                entries.Append("</color>\n")
                    .Append(order.ClientRole).Append('\n')
                    .Append("Нужен сорт: ").Append(target.DisplayName);
                if (order.RequiresFlowerForm)
                {
                    entries.Append(" · форма: ").Append(order.TargetFlowerForm.ToDisplayName());
                }

                entries.Append('\n').Append(order.PoeticHint).Append("\n\n");
            }

            _ordersListText.text = entries.ToString();
        }

        private void ConfigureJournalText()
        {
            ConfigureText(_titleText, 30, Color.black);
            ConfigureText(_ordersListText, 20, new Color(0.09f, 0.12f, 0.10f));
            ConfigureText(_recipesListText, 19, new Color(0.09f, 0.12f, 0.10f));
            ConfigureText(_recipesCounterText, 20, new Color(0.09f, 0.12f, 0.10f));
        }

        private static void ConfigureText(Text text, int fontSize, Color color)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = fontSize;
            text.color = color;
            text.lineSpacing = 1.15f;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
        }

        private static ScrollRect CreateScrollableList(Text text)
        {
            if (text == null)
            {
                return null;
            }

            RectTransform textRect = text.rectTransform;
            RectTransform originalParent = textRect.parent as RectTransform;
            if (originalParent == null)
            {
                Debug.LogError($"Cannot create a journal scroll view for '{text.name}': its parent has no RectTransform.", text);
                return null;
            }
            int siblingIndex = textRect.GetSiblingIndex();
            var scrollObject = new GameObject($"{text.name}ScrollView", typeof(RectTransform), typeof(ScrollRect));
            var scrollTransform = scrollObject.GetComponent<RectTransform>();
            var scrollRect = scrollObject.GetComponent<ScrollRect>();
            scrollTransform.SetParent(originalParent, false);
            scrollTransform.SetSiblingIndex(siblingIndex);
            scrollTransform.anchorMin = textRect.anchorMin;
            scrollTransform.anchorMax = textRect.anchorMax;
            scrollTransform.pivot = textRect.pivot;
            scrollTransform.anchoredPosition = textRect.anchoredPosition;
            scrollTransform.sizeDelta = textRect.sizeDelta;
            scrollTransform.localRotation = textRect.localRotation;
            scrollTransform.localScale = textRect.localScale;

            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            var viewport = viewportObject.GetComponent<RectTransform>();
            viewport.SetParent(scrollTransform, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = new Vector2(-18f, 0f);
            var viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

            var contentObject = new GameObject("Content", typeof(RectTransform));
            var content = contentObject.GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, viewport.rect.height);

            textRect.SetParent(content, false);
            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = new Vector2(0f, -8f);
            textRect.sizeDelta = new Vector2(-16f, 0f);
            text.raycastTarget = true;

            var scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
            var scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.SetParent(scrollTransform, false);
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = Vector2.one;
            scrollbarRect.anchoredPosition = Vector2.zero;
            scrollbarRect.sizeDelta = new Vector2(14f, 0f);
            var scrollbarImage = scrollbarObject.GetComponent<Image>();
            scrollbarImage.color = new Color(0.30f, 0.35f, 0.31f, 0.75f);

            var handleObject = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var handle = handleObject.GetComponent<RectTransform>();
            handle.SetParent(scrollbarRect, false);
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.offsetMin = new Vector2(2f, 2f);
            handle.offsetMax = new Vector2(-2f, -2f);
            handleObject.GetComponent<Image>().color = new Color(0.12f, 0.32f, 0.20f, 0.95f);

            var scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleObject.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return scrollRect;
        }

        private static void UpdateScrollContent(Text text, ScrollRect scrollRect)
        {
            if (text == null || scrollRect == null || scrollRect.content == null || scrollRect.viewport == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            float contentHeight = Mathf.Max(scrollRect.viewport.rect.height - 16f, text.preferredHeight + 16f);
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
        }
    }
}
