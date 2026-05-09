using UnityEngine;

namespace September.Common
{
    [CreateAssetMenu(fileName = "BuildDatas", menuName = "Build/BuildDatas")]
    public class BuildDatas : ScriptableObject
    {
        [SerializeField] BuildDataBase[] _builds;

        public BuildDataBase[] Builds => _builds;
    }
}
