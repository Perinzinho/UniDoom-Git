using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Both the overhead bar and top HUD follow the same damage source.</summary>
public sealed class DirectorBossHealthBar : MonoBehaviour
{
    [SerializeField] DirectorBoss boss;
    [SerializeField] Image fill;
    [SerializeField] Image damageTrail;
    [SerializeField] TMP_Text valueLabel;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] bool faceCamera;
    Camera viewCamera;
    float targetFill = 1, visibleFill = 1, trailFill = 1, lastHit, diedAt = -1;
    public float DisplayedFraction => visibleFill;

    void OnEnable()
    {
        if (boss == null) boss = GetComponentInParent<DirectorBoss>();
        if (boss == null) return;
        boss.HealthChanged += UpdateHealth;
        boss.Defeated += HandleDeath;
        UpdateHealth(boss.CurrentHealth, boss.MaxHealth);
        if (boss.IsDead) HandleDeath();
    }
    void OnDisable()
    {
        if (boss == null) return;
        boss.HealthChanged -= UpdateHealth;
        boss.Defeated -= HandleDeath;
    }
    void HandleDeath() => diedAt = Time.time;
    void UpdateHealth(float health, float max)
    {
        targetFill = Mathf.Clamp01(health / Mathf.Max(1, max));
        lastHit = Time.time;
        if (valueLabel != null) valueLabel.text = $"{Mathf.CeilToInt(health)} / {Mathf.CeilToInt(max)}";
    }
    void LateUpdate()
    {
        visibleFill = Mathf.MoveTowards(visibleFill, targetFill, Time.deltaTime * 2.8f);
        if (Time.time - lastHit > .4f) trailFill = Mathf.MoveTowards(trailFill, targetFill, Time.deltaTime * .55f);
        if (fill != null) fill.rectTransform.localScale = new Vector3(visibleFill, 1, 1);
        if (damageTrail != null) damageTrail.rectTransform.localScale = new Vector3(Mathf.Max(visibleFill, trailFill), 1, 1);
        if (canvasGroup != null)
            canvasGroup.alpha = diedAt < 0 ? 1 : 1 - Mathf.Clamp01((Time.time - diedAt - 2.8f) / .8f);
        if (faceCamera)
        {
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera != null) transform.rotation = viewCamera.transform.rotation;
        }
    }
}
