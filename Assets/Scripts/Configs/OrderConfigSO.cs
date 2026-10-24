using UnityEngine;

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
        [SerializeField] private int _parMerges = 1;

        public int OrderId => _orderId;
        public string ClientTitle => _clientTitle;
        public string ClientRole => _clientRole;
        public string PoeticHint => _poeticHint;
        public string TargetColorId => _targetColorId;
        public int ParMerges => _parMerges;

        public void Initialize(int id, string title, string role, string hint, string targetColor, int par)
        {
            _orderId = id;
            _clientTitle = title;
            _clientRole = role;
            _poeticHint = hint;
            _targetColorId = targetColor;
            _parMerges = par;
        }
    }
}
