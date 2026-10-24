using System;
using System.Collections;
using GardenMerger.Core;
using UnityEngine;

namespace GardenMerger.Tabletop
{
    public class CrossBreederMachine : MonoBehaviour
    {
        [SerializeField] private TableSlot _slotA;
        [SerializeField] private TableSlot _slotB;
        [SerializeField] private TableSlot _outputDish;
        [SerializeField] private Transform _buttonTransform;
        [SerializeField] private ParticleSystem _synthesisParticles;
        [SerializeField] private float _synthesisDuration = 0.60f;

        private bool _isProcessing;

        public TableSlot SlotA => _slotA;
        public TableSlot SlotB => _slotB;
        public TableSlot OutputDish => _outputDish;
        public bool IsProcessing => _isProcessing;

        public event Action<string, string, string> OnBreedingCompleted;

        public void Setup(TableSlot slotA, TableSlot slotB, TableSlot outputDish, Transform button, ParticleSystem particles, float duration)
        {
            _slotA = slotA;
            _slotB = slotB;
            _outputDish = outputDish;
            _buttonTransform = button;
            _synthesisParticles = particles;
            _synthesisDuration = duration;
        }

        public bool CanSynthesize()
        {
            return !_isProcessing &&
                   _slotA != null && _slotA.IsOccupied && _slotA.CurrentItem is FlowerPot &&
                   _slotB != null && _slotB.IsOccupied && _slotB.CurrentItem is FlowerPot &&
                   _outputDish != null && !_outputDish.IsOccupied;
        }

        public bool TryTriggerSynthesis()
        {
            if (!CanSynthesize()) return false;

            StartCoroutine(ProcessSynthesis());
            return true;
        }

        private IEnumerator ProcessSynthesis()
        {
            _isProcessing = true;

            // Button press anim
            if (_buttonTransform != null)
            {
                _buttonTransform.localScale = Vector3.one * 0.9f;
            }

            if (_synthesisParticles != null)
            {
                _synthesisParticles.Play();
            }

            yield return new WaitForSeconds(_synthesisDuration);

            if (_buttonTransform != null)
            {
                _buttonTransform.localScale = Vector3.one;
            }

            FlowerPot potA = _slotA.CurrentItem as FlowerPot;
            FlowerPot potB = _slotB.CurrentItem as FlowerPot;

            if (potA != null && potB != null)
            {
                string parentAColor = potA.ColorId;
                string parentBColor = potB.ColorId;
                string hybridColor = ColorRegistry.CombineColors(parentAColor, parentBColor);

                // Parents remain intact in their slots!
                OnBreedingCompleted?.Invoke(parentAColor, parentBColor, hybridColor);
            }

            _isProcessing = false;
        }
    }
}
