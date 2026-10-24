using System;
using System.Collections.Generic;
using GardenMerger.Configs;
using UnityEngine;

namespace GardenMerger.Core
{
    /// <summary>
    /// Устаревший класс. Логика инициализации и управления игрой объединена в <see cref="GameManager"/>.
    /// Оставлен для обратной совместимости сериализованных данных на существующих сценах.
    /// </summary>
    [Obsolete("GameBootstrap функционал объединен в GameManager. Используйте GameManager.")]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private TabletopBalanceSO _balanceSO;
        [SerializeField] private List<OrderConfigSO> _ordersSO;
        [SerializeField] private List<BreedingRecipeSO> _recipesSO;

        public TabletopBalanceSO BalanceSO => _balanceSO;
        public List<OrderConfigSO> OrdersSO => _ordersSO;
        public List<BreedingRecipeSO> RecipesSO => _recipesSO;

        private void Awake()
        {
            // Если GameManager уже присутствует, этот вспомогательный объект не требуется
            if (FindAnyObjectByType<GameManager>() != null)
            {
                Destroy(gameObject);
            }
        }
    }
}
