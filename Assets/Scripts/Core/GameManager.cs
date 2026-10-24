using System;
using System.Collections.Generic;
using GardenMerger.Configs;
using GardenMerger.Tabletop;
using UnityEngine;

namespace GardenMerger.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private List<OrderConfigSO> _orders = new List<OrderConfigSO>();
        [SerializeField] private List<BreedingRecipeSO> _recipes = new List<BreedingRecipeSO>();

        private int _currentOrderIndex;
        private int _orderMergeCount;
        private bool _isCampaignWon;

        public int CurrentOrderIndex => _currentOrderIndex;
        public int OrderMergeCount => _orderMergeCount;
        public bool IsCampaignWon => _isCampaignWon;
        public OrderConfigSO CurrentOrder => (_orders != null && _currentOrderIndex < _orders.Count) ? _orders[_currentOrderIndex] : null;
        public List<BreedingRecipeSO> Recipes => _recipes;

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
        }

        public void Initialize(List<OrderConfigSO> orders, List<BreedingRecipeSO> recipes)
        {
            _orders = orders;
            _recipes = recipes;
            _currentOrderIndex = 0;
            _orderMergeCount = 0;
            _isCampaignWon = false;

            NotifyOrderChanged();
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
                    if (recipe.Matches(parentA, parentB))
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
                // Match!
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
