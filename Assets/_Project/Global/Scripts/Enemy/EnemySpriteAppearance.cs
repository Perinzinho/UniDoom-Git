using UnityEngine;

/// <summary>Optional presentation for the shared enemy; combat and spawning stay on Enemy.</summary>
[CreateAssetMenu(menuName = "UniDoom/Enemy Sprite Appearance")]
public sealed class EnemySpriteAppearance : ScriptableObject
{
    public Material material;
    public Sprite[] idleFrames;
    public Sprite[] sideWalkFrames;
    public Sprite[] leftWalkFrames;
    public Sprite[] rightWalkFrames;
    public Sprite[] backWalkFrames;
    public Sprite[] attackFrames;
    public Sprite[] painFrames;
    public Sprite[] deathFrames;
    [Min(.1f)] public float visualScale = 1;
    [Min(.1f)] public float colliderHeight = 1.55f;
    [Min(.1f)] public float colliderRadius = .5f;
    [Min(1)] public float idleFramesPerSecond = 4;
    [Min(1)] public float walkFramesPerSecond = 9;
    [Min(.1f)] public float attackDuration = .65f;
    [Min(.1f)] public float painDuration = .32f;
    [Min(.1f)] public float deathDuration = 1.5f;
}
