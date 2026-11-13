using System.Collections;
using UnityEngine;
using UniDoom.UI;

/// <summary>Reveals the holding cells after the Director's death animation finishes.</summary>
public sealed class BasementPrisonPassage : MonoBehaviour
{
    [SerializeField] DirectorBossEncounter encounter;
    [SerializeField] Transform concealedPanel;
    [SerializeField] Vector3 openingOffset = new Vector3(0, 4.2f, 0);
    [SerializeField, Min(.1f)] float openingDuration = 3.5f;
    [SerializeField] AudioSource mechanism;
    [SerializeField] Light passageLight;
    [SerializeField, Min(0)] float revealedLightIntensity = 4f;

    public bool Revealed { get; private set; }
    Vector3 closedPosition;
    Coroutine opening;

    void Awake()
    {
        if (concealedPanel != null) closedPosition = concealedPanel.localPosition;
        if (passageLight != null) passageLight.intensity = 0;
    }

    void OnEnable()
    {
        if (encounter == null || concealedPanel == null)
        {
            Debug.LogError("Prison passage needs its Director encounter and concealed panel.", this);
            enabled = false;
            return;
        }
        encounter.onEncounterCompleted.AddListener(Reveal);
        if (encounter.Completed) Reveal();
    }

    void OnDisable()
    {
        if (encounter != null) encounter.onEncounterCompleted.RemoveListener(Reveal);
        if (opening == null) return;
        StopCoroutine(opening);
        opening = null;
        // A disabled/re-enabled passage must never leave a partial obstruction.
        FinishOpening();
    }

    public void Reveal()
    {
        if (Revealed || concealedPanel == null || !isActiveAndEnabled) return;
        Revealed = true;
        if (mechanism != null) mechanism.Play();
        if (passageLight != null) passageLight.enabled = true;
        opening = StartCoroutine(Open());
        // Restored encounters also reveal this passage; announce it only after a real boss defeat.
        if (encounter.SpawnedBoss != null && encounter.SpawnedBoss.DeathAnimationFinished)
            GameplayNotice.ShowWhenGameplayReady(this,
                "Uma porta se abriu na parede. Atravesse o portal para a UNIMAR e ative o gerador da biblioteca. Depois, volte e liberte os professores.",
                11f);
    }

    public void RevealImmediately()
    {
        if (concealedPanel == null) return;
        if (opening != null) StopCoroutine(opening);
        opening = null;
        Revealed = true;
        if (passageLight != null) passageLight.enabled = true;
        FinishOpening();
    }

    IEnumerator Open()
    {
        float elapsed = 0;
        while (elapsed < openingDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / openingDuration));
            concealedPanel.localPosition = closedPosition + openingOffset * progress;
            SetPassageLight(Mathf.Clamp01(elapsed / openingDuration * 2));
            yield return null;
        }
        FinishOpening();
        opening = null;
    }

    void FinishOpening()
    {
        if (concealedPanel != null)
        {
            concealedPanel.localPosition = closedPosition + openingOffset;
            // The panel is fully inside the wall pocket; neither its mesh nor collision
            // should remain over the opening after the animation finishes.
            concealedPanel.gameObject.SetActive(false);
        }
        SetPassageLight(1);
    }

    void SetPassageLight(float progress)
    {
        if (passageLight != null)
            passageLight.intensity = revealedLightIntensity * Mathf.SmoothStep(0, 1, progress);
    }
}
