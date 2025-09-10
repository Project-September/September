using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class HitboxFrameData
{
    public int frame;
    public int startFrame;
    public int endFrame;
    public bool active;
    public Vector3 hitboxPos;
    public Vector3 hitboxSize;
    public int damage;
    public Vector3 rootOffset;
    
}

[CreateAssetMenu(fileName = "NewHitboxData", menuName = "Hitbox Data")]
public class HitboxData : ScriptableObject
{
    public HitboxFrameData[] frames;
}
