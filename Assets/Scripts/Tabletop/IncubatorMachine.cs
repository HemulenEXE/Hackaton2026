using System;
using System.Collections;
using GardenMerger.Core;
using UnityEngine;

namespace GardenMerger.Tabletop
{
    public enum TemperatureMode
    {
        Neutral,
        Warm,
        Cold
    }

    public class IncubatorMachine : MonoBehaviour
    {
        [SerializeField] private TableSlot _seedSlot;
        [SerializeField] private TableSlot _outputTray;
        [SerializeField] private Transform _leverTransform;
        [SerializeField] private ParticleSystem _steamParticles;
        [SerializeField] private float _sproutDuration = 0.45f;

        private bool _isProcessing;
        private Vector3 _leverInitialRot;
        private TemperatureMode _temperatureMode;

        public TableSlot SeedSlot => _seedSlot;
        public TableSlot OutputTray => _outputTray;
        public bool IsProcessing => _isProcessing;
        public TemperatureMode CurrentTemperatureMode => _temperatureMode;

        public event Action<string> OnFlowerSprouted;
        public event Action<TemperatureMode> OnTemperatureChanged;

        private void Awake()
        {
            if (_leverTransform != null)
            {
                _leverInitialRot = _leverTransform.localEulerAngles;
            }
        }

        public void Setup(TableSlot seedSlot, TableSlot outputTray, Transform lever, ParticleSystem steam, float duration)
        {
            _seedSlot = seedSlot;
            _outputTray = outputTray;
            _leverTransform = lever;
            _steamParticles = steam;
            _sproutDuration = duration;
            if (_leverTransform != null) _leverInitialRot = _leverTransform.localEulerAngles;
        }

        public bool TryPullLever()
        {
            if (_isProcessing) return false;

            StartCoroutine(AnimateLever());

            if (!_seedSlot.IsOccupied)
            {
                // Dry click
                return false;
            }

            if (_outputTray.IsOccupied)
            {
                // Output tray is already blocked
                return false;
            }

            StartCoroutine(ProcessSprout(_temperatureMode));
            return true;
        }

        public TemperatureMode CycleTemperature()
        {
            switch (_temperatureMode)
            {
                case TemperatureMode.Neutral:
                    _temperatureMode = TemperatureMode.Warm;
                    break;
                case TemperatureMode.Warm:
                    _temperatureMode = TemperatureMode.Cold;
                    break;
                default:
                    _temperatureMode = TemperatureMode.Neutral;
                    break;
            }

            OnTemperatureChanged?.Invoke(_temperatureMode);
            return _temperatureMode;
        }

        private IEnumerator AnimateLever()
        {
            if (_leverTransform == null) yield break;

            float elapsed = 0f;
            float duration = 0.2f;

            // Pull down
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _leverTransform.localEulerAngles = _leverInitialRot + new Vector3(0, 0, Mathf.Lerp(0, -35f, t));
                yield return null;
            }

            // Return up
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _leverTransform.localEulerAngles = _leverInitialRot + new Vector3(0, 0, Mathf.Lerp(-35f, 0, t));
                yield return null;
            }

            _leverTransform.localEulerAngles = _leverInitialRot;
        }

        private IEnumerator ProcessSprout(TemperatureMode temperatureMode)
        {
            _isProcessing = true;

            if (_steamParticles != null)
            {
                _steamParticles.Play();
            }

            yield return new WaitForSeconds(_sproutDuration);

            if (_seedSlot.CurrentItem is SeedItem seed)
            {
                string colorId = seed.ColorId;
                if (ColorRegistry.TryParseColor(colorId, out FlowerColor seedColor))
                {
                    switch (temperatureMode)
                    {
                        case TemperatureMode.Warm:
                            colorId = ColorRegistry.Warm(seedColor).ToString();
                            break;
                        case TemperatureMode.Cold:
                            colorId = ColorRegistry.Cool(seedColor).ToString();
                            break;
                    }
                }
                else
                {
                    Debug.LogError($"Unknown seed color '{colorId}' in incubator.", seed);
                }

                // Remove seed from slot and destroy
                _seedSlot.ClearItem();
                Destroy(seed.gameObject);

                // Spawn new flower pot on output tray
                OnFlowerSprouted?.Invoke(colorId);
            }

            _isProcessing = false;
        }
    }
}
