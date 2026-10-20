using System.Collections;
using UnityEngine;

/// <summary>Reveals the holding cells after the Director's death animation finishes.</summary>
public sealed class BasementPrisonPassage : MonoBehaviour
{
    [SerializeField] DirectorBossEncounter encounter;
    [SerializeField] Transform concealedPanel;
    [SerializeField] Vector3 openingOffset = new Vector3(0, 4.2f, 0);
    [SerializeField, Min(.1f)] float openingDuration = 3.5f;
    [SerializeField] AudioSource mechanism;

    public bool Revealed { get; private set; }
    Vector3 closedPosition;
    Coroutine opening;

    void Awake()
    {
        if (concealedPanel != null) closedPosition = concealedPanel.localPosition;
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
        if (concealedPanel != null) concealedPanel.localPosition = closedPosition + openingOffset;
    }

    public void Reveal()
    {
        if (Revealed || concealedPanel == null || !isActiveAndEnabled) return;
        Revealed = true;
        if (mechanism != null) mechanism.Play();
        opening = StartCoroutine(Open());
    }

    IEnumerator Open()
    {
        float elapsed = 0;
        while (elapsed < openingDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / openingDuration));
            concealedPanel.localPosition = closedPosition + openingOffset * progress;
            yield return null;
        }
        concealedPanel.localPosition = closedPosition + openingOffset;
        opening = null;
    }
}
