using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GardenMerger.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Transition")]
        [SerializeField] private string _gameSceneName = "SimpleScene";
        [SerializeField] private Button _playButton;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _clickSound;

        [Header("Button Micro-Animation")]
        [SerializeField] private RectTransform _buttonRect;
        [SerializeField] private float _hoverScale = 1.06f;

        private Vector3 _originalButtonScale;
        private Coroutine _scaleCoroutine;

        private void Awake()
        {
            if (_playButton == null)
            {
                _playButton = GetComponentInChildren<Button>();
            }

            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayButtonClicked);
                if (_buttonRect == null)
                {
                    _buttonRect = _playButton.GetComponent<RectTransform>();
                }
            }

            if (_buttonRect != null)
            {
                _originalButtonScale = _buttonRect.localScale;
            }

            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
        }

        public void OnPlayButtonClicked()
        {
            if (_audioSource != null && _clickSound != null)
            {
                _audioSource.PlayOneShot(_clickSound);
            }

            StartCoroutine(LoadSceneRoutine());
        }

        private IEnumerator LoadSceneRoutine()
        {
            // Небольшая задержка для проигрывания клика
            if (_clickSound != null)
            {
                yield return new WaitForSecondsRealtime(0.12f);
            }

            SceneManager.LoadScene(_gameSceneName);
        }

        public void OnButtonPointerEnter()
        {
            AnimateButtonScale(_originalButtonScale * _hoverScale);
        }

        public void OnButtonPointerExit()
        {
            AnimateButtonScale(_originalButtonScale);
        }

        private void AnimateButtonScale(Vector3 targetScale)
        {
            if (_buttonRect == null) return;
            if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(ScaleRoutine(targetScale));
        }

        private IEnumerator ScaleRoutine(Vector3 target)
        {
            float elapsed = 0f;
            float duration = 0.12f;
            Vector3 start = _buttonRect.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                _buttonRect.localScale = Vector3.Lerp(start, target, t);
                yield return null;
            }

            _buttonRect.localScale = target;
        }
    }
}
