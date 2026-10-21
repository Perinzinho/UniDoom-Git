using UnityEngine;

/// <summary>Persistent imported sprites, camera-facing on Y; final death frame remains as a corpse.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class DirectorBossAnimator : MonoBehaviour
{
    [SerializeField] DirectorBoss boss;
    [SerializeField] Sprite[] walkFrames;
    [SerializeField] Sprite[] attackFrames;
    [SerializeField] Sprite[] paperCastFrames;
    [SerializeField] Sprite[] deathFrames;
    [SerializeField, Min(1)] float walkFramesPerSecond = 5;
    [SerializeField, Min(.1f)] float deathDuration = 2.8f;
    public bool IsWalking { get; set; }
    public float DeathDuration => deathDuration;
    public int DeathFrameCount => deathFrames == null ? 0 : deathFrames.Length;
    SpriteRenderer spriteRenderer;
    Camera viewCamera;
    float deathStarted = -1, flashUntil;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (boss == null) boss = GetComponentInParent<DirectorBoss>();
    }

    public void FlashHit() => flashUntil = Time.time + .13f;
    public void PlayDeath() { if (deathStarted < 0) deathStarted = Time.time; }

    void LateUpdate()
    {
        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera != null)
        {
            Vector3 forward = transform.position - viewCamera.transform.position;
            forward.y = 0;
            if (forward.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(forward);
        }
        if (boss == null) return;
        if (boss.IsDead)
        {
            if (deathStarted < 0) PlayDeath();
            float progress = Mathf.Clamp01((Time.time - deathStarted) / deathDuration);
            SetFrame(deathFrames, Mathf.FloorToInt(progress * DeathFrameCount));
            spriteRenderer.color = Color.white;
            if (progress >= 1) boss.FinishDeathAnimation();
        }
        else
        {
            if (boss.IsCastingPaper && paperCastFrames != null && paperCastFrames.Length > 0)
                SetFrame(paperCastFrames, Mathf.FloorToInt(boss.PaperCastProgress * paperCastFrames.Length));
            else if (boss.IsAttacking && attackFrames != null)
                SetFrame(attackFrames, Mathf.FloorToInt(boss.AttackProgress * attackFrames.Length));
            else SetFrame(walkFrames, IsWalking && walkFrames != null && walkFrames.Length > 0
                ? Mathf.FloorToInt(Time.time * walkFramesPerSecond) % walkFrames.Length : 0);
            spriteRenderer.color = Time.time < flashUntil ? new Color(1, .3f, .25f, 1) :
                new Color(1, 1, 1, Mathf.SmoothStep(0, 1, boss.AwakeningProgress));
        }
    }

    void SetFrame(Sprite[] frames, int index)
    {
        if (frames != null && frames.Length > 0) spriteRenderer.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1) % frames.Length];
    }
}
