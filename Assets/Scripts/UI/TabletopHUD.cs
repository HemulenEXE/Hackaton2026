using System;
using System.Collections;
using GardenMerger.Configs;
using GardenMerger.Core;
using GardenMerger.Tabletop;
using UnityEngine;
using UnityEngine.UI;

namespace GardenMerger.UI
{
    public class TabletopHUD : MonoBehaviour
    {
        [Header("Order Card")]
        [SerializeField] private Text _orderTitleText;
        [SerializeField] private Text _clientRoleText;
        [SerializeField] private Text _targetColorText;
        [SerializeField] private Text _poeticHintText;
        [SerializeField] private Text _mergeCounterText;
        [SerializeField] private Image _targetColorSwatch;

        [Header("Banner Notification")]
        [SerializeField] private GameObject _bannerRoot;
        [SerializeField] private Text _bannerTitleText;
        [SerializeField] private Text _bannerSubtext;

        [Header("Top Bar Buttons")]
        [SerializeField] private Button _notebookButton;
        [SerializeField] private Button _tunerButton;

        public event Action OnNotebookClicked;
        public event Action OnTunerClicked;

        private Coroutine _bannerCoroutine;

        private void Awake()
        {
            if (_notebookButton != null) _notebookButton.onClick.AddListener(() => OnNotebookClicked?.Invoke());
            if (_tunerButton != null) _tunerButton.onClick.AddListener(() => OnTunerClicked?.Invoke());
        }

        public void Setup(Text title, Text role, Text targetColor, Text hint, Text counter, Image swatch,
                          GameObject banner, Text bTitle, Text bSub, Button nbBtn, Button tunerBtn)
        {
            _orderTitleText = title;
            _clientRoleText = role;
            _targetColorText = targetColor;
            _poeticHintText = hint;
            _mergeCounterText = counter;
            _targetColorSwatch = swatch;
            _bannerRoot = banner;
            _bannerTitleText = bTitle;
            _bannerSubtext = bSub;
            _notebookButton = nbBtn;
            _tunerButton = tunerBtn;

            if (_notebookButton != null)
            {
                _notebookButton.onClick.RemoveAllListeners();
                _notebookButton.onClick.AddListener(() => OnNotebookClicked?.Invoke());
            }

            if (_tunerButton != null)
            {
                _tunerButton.onClick.RemoveAllListeners();
                _tunerButton.onClick.AddListener(() => OnTunerClicked?.Invoke());
            }
        }

        public void UpdateOrder(OrderConfigSO order, int currentMerges)
        {
            if (order == null) return;

            if (_orderTitleText != null) _orderTitleText.text = $"Заказ #{order.OrderId}: {order.ClientTitle}";
            if (_clientRoleText != null) _clientRoleText.text = order.ClientRole;
            if (_poeticHintText != null) _poeticHintText.text = order.PoeticHint;

            BotanicalColor targetColor = ColorRegistry.GetColor(order.TargetColorId);
            if (_targetColorText != null)
            {
                string targetForm = order.RequiresFlowerForm
                    ? $"\nФорма: {order.TargetFlowerForm.ToDisplayName()}"
                    : string.Empty;
                _targetColorText.text = $"Цель: {targetColor.DisplayName} — {targetColor.BotanicalName}{targetForm}";
            }

            if (_mergeCounterText != null)
            {
                string colorHex = currentMerges <= order.ParMerges ? "#238636" : "#B22222";
                _mergeCounterText.text = $"Скрещиваний: <color={colorHex}>{currentMerges}</color> / Пар: {order.ParMerges}";
            }

            if (_targetColorSwatch != null)
            {
                _targetColorSwatch.color = targetColor.Color;
            }
        }

        public void ShowBanner(string title, string subtext, float duration = 3.0f)
        {
            if (_bannerRoot == null) return;

            if (_bannerTitleText != null) _bannerTitleText.text = title;
            if (_bannerSubtext != null) _bannerSubtext.text = subtext;

            if (_bannerCoroutine != null) StopCoroutine(_bannerCoroutine);
            _bannerCoroutine = StartCoroutine(BannerRoutine(duration));
        }

        private IEnumerator BannerRoutine(float duration)
        {
            _bannerRoot.SetActive(true);
            yield return new WaitForSeconds(duration);
            _bannerRoot.SetActive(false);
        }
    }
}
