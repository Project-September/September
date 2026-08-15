using Result;
using System.Collections.Generic;
using UnityEngine;

namespace InGame.Player.Ability
{
    [CreateAssetMenu(fileName = "RevealAttackParams", menuName = "Scriptable Objects/Player/Takamura/RevealAttackParams")]
    public class RevealAttackParams : ScriptableObject
    {
        [Header("デフォルトの攻撃範囲"), SerializeField] float _defaultRadius = 1;
        [SerializeField] ExhibitRadius[] _radiusArray;
        Dictionary<ExhibitType, float> _radiusDict;

        private void OnEnable()
        {
            _radiusDict = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Inspectorで配列が変更されたら検索用Dictionaryを作り直す
            _radiusDict = null;
        }
#endif

        /// <summary>
        /// 展示物に応じた攻撃範囲を取得するメソッド
        /// </summary>
        /// <param name="type">展示物の種類</param>
        /// <returns>攻撃範囲</returns>
        public float GetRadius(ExhibitType type)
        {
            if (_radiusDict == null)
            {
                _radiusDict = new();
                if (_radiusArray == null) return _defaultRadius;

                foreach (var data in _radiusArray)
                {
                    if (data == null) continue;
                    _radiusDict[data.ExhibitType] = data.Radius;
                }
            }

            return _radiusDict.TryGetValue(type, out var radius) ? radius : _defaultRadius;
        }
    }
}
