using UnityEngine;

namespace GardenMerger.Configs
{
    [CreateAssetMenu(fileName = "NewBreedingRecipe", menuName = "GardenMerger/Breeding Recipe")]
    public class BreedingRecipeSO : ScriptableObject
    {
        [SerializeField] private string _parentAColorId;
        [SerializeField] private string _parentBColorId;
        [SerializeField] private string _resultColorId;
        [SerializeField] private int _tier = 1;
        [SerializeField] private bool _isDiscovered;

        public string ParentAColorId => _parentAColorId;
        public string ParentBColorId => _parentBColorId;
        public string ResultColorId => _resultColorId;
        public int Tier => _tier;
        public bool IsDiscovered => _isDiscovered;

        public void SetDiscovered(bool discovered)
        {
            _isDiscovered = discovered;
        }

        public void Initialize(string parentA, string parentB, string result, int tier, bool discovered = false)
        {
            _parentAColorId = parentA;
            _parentBColorId = parentB;
            _resultColorId = result;
            _tier = tier;
            _isDiscovered = discovered;
        }

        public bool Matches(string colorA, string colorB)
        {
            return (_parentAColorId == colorA && _parentBColorId == colorB) ||
                   (_parentAColorId == colorB && _parentBColorId == colorA);
        }
    }
}
