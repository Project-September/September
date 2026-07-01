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
                foreach (var data in _radiusArray)
                {
                    _radiusDict[data.ExhibitType] = data.Radius;
                }
            }

            return _radiusDict.TryGetValue(type, out var radius) ? radius : _defaultRadius;
        }
    }
}
