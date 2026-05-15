using System;
using UnityEngine;

namespace September.InGame.Common.Hitbox.ShapeStructs
{
    [Serializable]
    public struct BoxHitbox
    {
        [SerializeField] private Vector3 _center;
        [SerializeField] private Vector3 _halfExtents;
        [SerializeField] private Vector3 _eulerRotation;
        
        public Vector3 Center => _center;
        public Vector3 HalfExtents => _halfExtents;
        public Quaternion Rotation => Quaternion.Euler(_eulerRotation);

        public Vector3 GetWorldCenter(Transform root)
        {
            return root.TransformPoint(_center);
        }

        public Vector3 GetWorldCenter(Vector3 basePosition, Quaternion baseRotation)
        {
            return basePosition + baseRotation * _center;
        }

        public Quaternion GetWorldRotation(Quaternion baseRotation)
        {
            return baseRotation * Rotation;
        }

        public Matrix4x4 GetMatrix()
        {
            return Matrix4x4.TRS(_center, Rotation, _halfExtents * 2);
        }
    }

    [Serializable]
    public struct HitboxTicks
    {
        [SerializeField] private int _startPredictionTick;
        [SerializeField] private int _predictionDurationTick;
        [SerializeField] private int _hitboxDurationTick;
        
        public int PredictionDurationTick => _predictionDurationTick;
        public int HitboxDurationTick => _hitboxDurationTick;
        
        public int GetStartPredictionTick(int startExecuteTick) => _startPredictionTick + startExecuteTick;
        public int GetStartHitboxTick(int startExecuteTick) => _predictionDurationTick + _startPredictionTick + startExecuteTick;
        public int GetEndTick(int startExecuteTick) => _hitboxDurationTick + _predictionDurationTick + _startPredictionTick + startExecuteTick;

        public bool IsStartPredictionTick(int currentTick, int startExecuteTick) => currentTick == _startPredictionTick + startExecuteTick;
        public bool IsInHitboxExecuteTick(int currentTick, int startExecuteTick) =>
            currentTick >= GetStartHitboxTick(startExecuteTick) &&
            currentTick <= GetEndTick(startExecuteTick);
        public bool IsEnded(int currentTick, int startExecuteTick) => currentTick >= GetEndTick(startExecuteTick);
    }
}