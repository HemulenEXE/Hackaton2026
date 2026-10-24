using UnityEngine;

namespace GardenMerger.Configs
{
    [CreateAssetMenu(fileName = "NewTabletopBalance", menuName = "GardenMerger/Tabletop Balance")]
    public class TabletopBalanceSO : ScriptableObject
    {
        [Header("Interaction & Snapping")]
        [SerializeField] private float _snapRadius = 1.0f;
        [SerializeField] private float _dragElevationScale = 1.15f;
        [SerializeField] private float _dragZOffset = -0.5f;

        [Header("Machine Durations")]
        [SerializeField] private float _sproutDurationSec = 0.45f;
        [SerializeField] private float _synthesisDurationSec = 0.60f;
        [SerializeField] private float _wasteChuteFadeDurationSec = 0.35f;

        [Header("Desk Space")]
        [SerializeField] private Vector2 _deskBounds = new Vector2(8.5f, 4.5f);

        public float SnapRadius => _snapRadius;
        public float DragElevationScale => _dragElevationScale;
        public float DragZOffset => _dragZOffset;
        public float SproutDurationSec => _sproutDurationSec;
        public float SynthesisDurationSec => _synthesisDurationSec;
        public float WasteChuteFadeDurationSec => _wasteChuteFadeDurationSec;
        public Vector2 DeskBounds => _deskBounds;
    }
}
