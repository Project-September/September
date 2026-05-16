using System.Collections.Generic;
using UnityEngine;

namespace September.Common
{
    [CreateAssetMenu(fileName = "BuildDatas", menuName = "Build/BuildDatas")]
    public class BuildDatas : ScriptableObject
    {
        [SerializeField] BuildDataBase[] _builds;

        public IReadOnlyList<BuildDataBase> Builds => _builds;
    }
}
