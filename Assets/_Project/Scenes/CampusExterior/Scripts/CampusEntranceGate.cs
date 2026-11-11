using UnityEngine;

/// <summary>The campus gate opens as the player leaves the prison access forecourt.</summary>
public sealed class CampusEntranceGate : MonoBehaviour
{
    [SerializeField] Transform leftLeaf, rightLeaf;
    [SerializeField] float travel = 5.6f;
    [SerializeField] float openingSeconds = 2.8f;
    [SerializeField] float approachDistance = 15f;
    Vector3 leftClosed, rightClosed;
    float progress;
    bool opening;

    void Awake()
    {
        if (leftLeaf != null) leftClosed = leftLeaf.localPosition;
        if (rightLeaf != null) rightClosed = rightLeaf.localPosition;
    }

    void Update()
    {
        if (!opening)
        {
            var player = GameManager.SessionPlayer;
            if (player != null && Vector3.Distance(player.transform.position, transform.position) < approachDistance)
            {
                opening = true;
                GameplayAudio.Instance?.PlayDoorOpening();
            }
        }
        if (!opening || progress >= 1) return;
        progress = Mathf.Clamp01(progress + Time.deltaTime / Mathf.Max(.1f, openingSeconds));
        float offset = Mathf.SmoothStep(0, travel, progress);
        if (leftLeaf != null) leftLeaf.localPosition = leftClosed + Vector3.left * offset;
        if (rightLeaf != null) rightLeaf.localPosition = rightClosed + Vector3.right * offset;
    }
}
