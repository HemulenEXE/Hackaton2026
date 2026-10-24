using UnityEngine;
using GardenMerger.Tabletop;

namespace GardenMerger.Configs
{
    [CreateAssetMenu(fileName = "NewOrderConfig", menuName = "GardenMerger/Order Config")]
    public class OrderConfigSO : ScriptableObject
    {
        [SerializeField] private int _orderId;
        [SerializeField] private string _clientTitle;
        [SerializeField] private string _clientRole;
        [TextArea(2, 4)]
        [SerializeField] private string _poeticHint;
        [SerializeField] private string _targetColorId;
        [SerializeField] private bool _requiresFlowerForm;
        [SerializeField] private FlowerForm _targetFlowerForm;
        [SerializeField] private int _parMerges = 1;

        public int OrderId => _orderId;
        public string ClientTitle => _clientTitle;
        public string ClientRole => _clientRole;
        public string PoeticHint => _poeticHint;
        public string TargetColorId => _targetColorId;
        public bool RequiresFlowerForm => _requiresFlowerForm;
        public FlowerForm TargetFlowerForm => _targetFlowerForm;
        public int ParMerges => _parMerges;

        public void Initialize(
            int id,
            string title,
            string role,
            string hint,
            string targetColor,
            int par,
            bool requiresFlowerForm = false,
            FlowerForm targetFlowerForm = FlowerForm.Flowers6)
        {
            _orderId = id;
            _clientTitle = title;
            _clientRole = role;
            _poeticHint = hint;
            _targetColorId = targetColor;
            _parMerges = par;
            _requiresFlowerForm = requiresFlowerForm;
            _targetFlowerForm = targetFlowerForm;
        }
    }
}
