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
        [SerializeField] private Text _tier1CounterText;
        [SerializeField] private Text _tier1ListText;
        [SerializeField] private Text _tier2CounterText;
        [SerializeField] private Text _tier2ListText;
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

        public void Setup(GameObject root, Text title, Text ordersList, Text t1Counter, Text t1List,
            Text t2Counter, Text t2List, Button closeBtn)
        {
            _panelRoot = root;
            _titleText = title;
            _ordersListText = ordersList;
            _tier1CounterText = t1Counter;
            _tier1ListText = t1List;
            _tier2CounterText = t2Counter;
            _tier2ListText = t2List;
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
            int t1Total = 0, t1Found = 0;
            int t2Total = 0, t2Found = 0;

            var tier1Entries = new System.Text.StringBuilder();
            var tier2Entries = new System.Text.StringBuilder();
            if (_recipes != null)
            {
                foreach (var recipe in _recipes)
                {
                    if (recipe == null) continue;

                    if (recipe.Tier == 1)
                    {
                        t1Total++;
                        if (recipe.IsDiscovered) t1Found++;
                        AppendRecipeEntry(tier1Entries, recipe);
                    }
                    else if (recipe.Tier == 2)
                    {
                        t2Total++;
                        if (recipe.IsDiscovered) t2Found++;
                        AppendRecipeEntry(tier2Entries, recipe);
                    }
                }
            }

            if (_tier1CounterText != null) _tier1CounterText.text = $"Открыто формул: {t1Found} из {t1Total}";
            if (_tier2CounterText != null) _tier2CounterText.text = $"Открыто формул: {t2Found} из {t2Total}";
            if (_tier1ListText != null) _tier1ListText.text = tier1Entries.ToString();
            if (_tier2ListText != null) _tier2ListText.text = tier2Entries.ToString();

            RefreshOrders();
        }

        private void AppendRecipeEntry(System.Text.StringBuilder entries, BreedingRecipeSO recipe)
        {
            if (recipe.IsDiscovered)
            {
                var parentA = ColorRegistry.GetColor(recipe.ParentAColorId);
                var parentB = ColorRegistry.GetColor(recipe.ParentBColorId);
                var result = ColorRegistry.GetColor(recipe.ResultColorId);
                entries.Append("<color=#3B6B43>");
                entries.Append(parentA.DisplayName).Append(" + ").Append(parentB.DisplayName);
                entries.Append("  >  ").Append(result.DisplayName);
                entries.Append("</color>\n");
                entries.Append("<size=12><i>")
                    .Append(parentA.BotanicalName).Append(" + ").Append(parentB.BotanicalName)
                    .Append("  >  ").Append(result.BotanicalName)
                    .Append("</i></size>\n\n");
            }
            else
            {
                entries.Append("<color=#877F70>??? + ???  >  ???</color>\n\n");
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
