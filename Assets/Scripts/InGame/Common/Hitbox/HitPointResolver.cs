using System.Collections.Generic;
using UnityEngine;

public class HitPointResolver : MonoBehaviour
{
    [SerializeField] private List<Transform> _hitPoints = new();
    [SerializeField] private float _radius = 0.1f;
    
    public List<Transform> GetPoints() => _hitPoints;
    public float GetRadius() => _radius;
}