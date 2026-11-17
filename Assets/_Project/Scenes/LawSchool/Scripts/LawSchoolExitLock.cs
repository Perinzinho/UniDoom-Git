using UnityEngine;
using UniDoom.Utils;

/// <summary>Keeps the entrance solid until the panel is off and the auditorium key is collected.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(SceneTransitionDoor),typeof(BoxCollider))]
public sealed class LawSchoolExitLock : MonoBehaviour, ISceneTransitionGuard
{
    [SerializeField] LawSchoolAlarmPrototype alarm;
    [SerializeField] BoxCollider solidBarrier;
    [SerializeField, Min(.5f)] float warningDistance = 3;
    [SerializeField, TextArea(2,3)] string warningText =
        "Saída bloqueada pelo alarme. Desligue-o no quadro da manutenção.";

    BoxCollider doorway;

    public bool IsTransitionBlocked
    {
        get
        {
            if (!alarm || !alarm.IsResolved) return true;
            var player = GameManager.SessionPlayer;
            var inventory = player ? player.GetComponent<PlayerCellKeys>() : null;
            return !inventory || !inventory.HasKey(CampusLibraryAccess.LawSchoolKeyId);
        }
    }
    public bool WarningVisible { get; private set; }
    public string WarningText => alarm && alarm.IsActive ? warningText :
        alarm && alarm.IsResolved ? "Ainda preciso pegar a chave da Biblioteca do Campus no auditório." :
        "Ainda preciso encontrar a chave da manutenção.";

    void Awake() => doorway = GetComponent<BoxCollider>();

    void OnEnable()
    {
        if (alarm) alarm.StateChanged += SyncBarrier;
        SyncBarrier();
    }

    void SyncBarrier()
    {
        if (!solidBarrier) return;
        solidBarrier.isTrigger = false;
        solidBarrier.enabled = IsTransitionBlocked;
    }

    // Independent of red/black phases, pause, and the portal's trigger collider.
    void LateUpdate() => SyncBarrier();

    void Update()
    {
        bool wasWarningVisible = WarningVisible;
        WarningVisible = false;
        if (!IsTransitionBlocked || Time.timeScale <= 0 || UniDoom.UI.GameMenus.BlocksGameplay) return;
        var player = GameManager.SessionPlayer;
        if (!player) return;
        var life = player.GetComponent<PlayerLife>();
        if (life && life.IsDead) return;
        var controller = player.GetComponent<CharacterController>();
        Vector3 position = controller ? controller.bounds.center : player.transform.position;
        WarningVisible = doorway && doorway.bounds.SqrDistance(position) <= warningDistance*warningDistance;
        if (WarningVisible && !wasWarningVisible)
            GameplayAudio.Instance?.NotifyLockedDoorContact(this);
    }

    void OnGUI()
    {
        if (WarningVisible && IsTransitionBlocked && !UniDoom.UI.GameplayNotice.IsShowing)
            UniDoom.UI.GameplayNotice.Draw(WarningText);
    }

    void OnDisable()
    {
        if (alarm) alarm.StateChanged -= SyncBarrier;
        WarningVisible = false;
        // Disabling the notice must not remove an active physical lock.
        SyncBarrier();
    }
}
