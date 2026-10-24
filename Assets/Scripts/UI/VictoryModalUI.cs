using System;
using UnityEngine;
using UnityEngine.UI;

namespace GardenMerger.UI
{
    public class VictoryModalUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _statsText;
        [SerializeField] private Button _sandboxButton;
        [SerializeField] private Button _restartButton;

        public event Action OnContinueSandbox;
        public event Action OnRestartCampaign;

        private void Awake()
        {
            if (_sandboxButton != null) _sandboxButton.onClick.AddListener(() => OnContinueSandbox?.Invoke());
            if (_restartButton != null) _restartButton.onClick.AddListener(() => OnRestartCampaign?.Invoke());
        }

        public void Setup(GameObject root, Text title, Text stats, Button sandboxBtn, Button restartBtn)
        {
            _panelRoot = root;
            _titleText = title;
            _statsText = stats;
            _sandboxButton = sandboxBtn;
            _restartButton = restartBtn;

            if (_sandboxButton != null)
            {
                _sandboxButton.onClick.RemoveAllListeners();
                _sandboxButton.onClick.AddListener(() =>
                {
                    Hide();
                    OnContinueSandbox?.Invoke();
                });
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveAllListeners();
                _restartButton.onClick.AddListener(() =>
                {
                    Hide();
                    OnRestartCampaign?.Invoke();
                });
            }
        }

        public void Show(int ordersCompleted, int recipesDiscovered, int totalRecipes)
        {
            if (_panelRoot != null) _panelRoot.SetActive(true);
            if (_statsText != null)
            {
                _statsText.text = $"Все {ordersCompleted} королевских заказов выполнены!\nОткрыто формул в журнале: {recipesDiscovered} из {totalRecipes}.";
            }
        }

        public void Hide()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }
    }
}
