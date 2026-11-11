using UnityEngine;

/// <summary>The moving half of a cell gate; its existing solid collider turns with the bars.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class PrisonCellGate : MonoBehaviour
{
    [SerializeField, Range(1, PrisonProgress.CellCount)] int cellId = 1;
    [SerializeField] Transform teacher;
    [SerializeField] float openingAngle = -100;
    [SerializeField, Min(.1f)] float openingDuration = 1.4f;
    Quaternion closedRotation;
    float progress;

    public int CellId => cellId;
    public bool IsOpen { get; private set; }
    public AudioSource Voice => teacher != null ? teacher.GetComponent<AudioSource>() : null;

    public void SetTeacherVisible(bool visible)
    {
        if (teacher != null) teacher.gameObject.SetActive(visible);
    }

    void Awake()
    {
        closedRotation = transform.localRotation;
        if (PrisonProgress.IsCellOpen(cellId))
        {
            IsOpen = true;
            progress = 1;
            ApplyPose();
        }
    }

    public bool TryOpen(PlayerPickupKey player)
    {
        if (!isActiveAndEnabled || IsOpen || player == null ||
            !PrisonProgress.CanOpenCell(cellId, player.GetComponent<PlayerCellKeys>()) ||
            UniDoom.UI.GameMenus.BlocksGameplay || Time.timeScale <= 0) return false;
        var life = player.GetComponent<PlayerLife>();
        if (life != null && life.IsDead) return false;
        if (!PrisonProgress.OpenCell(cellId)) return false;
        IsOpen = true;
        GameplayAudio.Instance?.PlayDoorOpening();
        return true;
    }

    void Update()
    {
        if (!IsOpen || progress >= 1) return;
        progress = Mathf.MoveTowards(progress, 1, Time.deltaTime / Mathf.Max(.1f, openingDuration));
        ApplyPose();
    }

    void ApplyPose() => transform.localRotation = closedRotation *
        Quaternion.Euler(0, openingAngle * Mathf.SmoothStep(0, 1, progress), 0);
}
