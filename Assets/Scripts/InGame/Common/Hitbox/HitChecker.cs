using System;
using System.Collections.Generic;
using Fusion;
using InGame.Health;
using JetBrains.Annotations;
using UnityEngine;

public class HitChecker : MonoBehaviour
{
    [SerializeField] private List<Transform> _hitPoints = new();
    [SerializeField] private float _radius = 0.1f;
    
    [SerializeField] private List<Collider> _alreadyHit = new();
    private MeleeHitboxExecutor _executor;
    public bool IsActive = false;
    public List<Transform> HitPoint => _hitPoints;
    public event Action<Collider> OnHit;
    
    public void StartHitCheck()
    {
        _executor = new MeleeHitboxExecutor(_hitPoints, _radius)
        {
            OnHit = (col,pos) =>
            {
                if (!col) return;
                if (_alreadyHit.Contains(col)) return; // 既にヒット済みのColliderは無視
                _alreadyHit.Add(col); // ヒット済みとして登録
                OnHit?.Invoke(col); // ヒットイベントを発火
            }
        };
        IsActive = true;
    }
    
    private void Update()
    {
        _executor?.Tick(Time.deltaTime);
    }

    public void EndHitCheck()
    {
        if (_executor != null)
        {
            _executor.OnHit = null; // ヒットイベントを解除
            _executor = null; // Executorを解放
            IsActive = false; // ヒットチェックが終了したことを通知
            _alreadyHit.Clear(); // ヒット済みリストをクリア
        }
    }
}