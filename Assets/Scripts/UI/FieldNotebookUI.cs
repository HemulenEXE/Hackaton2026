using System;
using System.Collections.Generic;
using GardenMerger.Configs;
using GardenMerger.Core;
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

        private List<OrderConfigSO> _orders = new List<OrderConfigSO>();
        private List<BreedingRecipeSO> _recipes = new List<BreedingRecipeSO>();
        private GameManager _gameManager;

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
            if (_recipesListText != null) _recipesListText.text = recipeEntries.ToString();

            RefreshOrders();
        }

        private void AppendRecipeEntry(System.Text.StringBuilder entries, BreedingRecipeSO recipe)
        {
            if (recipe.IsDiscovered)
            {
                var parentA = ColorRegistry.GetColor(recipe.ParentAColorId);
                var parentB = ColorRegistry.GetColor(recipe.ParentBColorId);
                var result = ColorRegistry.GetColor(recipe.ResultColorId);
                entries.Append("<color=#3B6B43>")
                    .Append(parentA.DisplayName).Append(" + ").Append(parentB.DisplayName)
                    .Append(" → ").Append(result.DisplayName)
                    .Append(" · ").Append(result.BotanicalName)
                    .Append("</color>\n");
            }
            else
            {
                entries.Append("<color=#877F70>??? + ??? → ???</color>\n");
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

                entries.Append("</color>\n");
                entries.Append("<size=12>")
                    .Append(order.ClientRole).Append(" · нужен сорт: ").Append(target.DisplayName)
                    .Append("</size>\n\n");
            }

            _ordersListText.text = entries.ToString();
        }
    }
}
