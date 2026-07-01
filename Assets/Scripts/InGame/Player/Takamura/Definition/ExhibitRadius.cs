using Result;
using System;
using UnityEngine;

namespace InGame.Player.Ability
{
    /// <summary>展示物ごとの擬態解除時の攻撃範囲を持つクラス</summary>
    [Serializable]
    public class ExhibitRadius
    {
        [Header("展示物の種類"), SerializeField] ExhibitType _exhibitType;
        [Header("擬態解除時の攻撃範囲"), SerializeField] float _radius;

        public ExhibitType ExhibitType => _exhibitType;
        public float Radius => _radius;
    }
}
