using UnityEngine;
using UniDoom.Utils;

/// <summary>Keeps the static entrance solid and prevents scene transitions during the alarm.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(SceneTransitionDoor),typeof(BoxCollider))]
public sealed class LawSchoolExitLock : MonoBehaviour, ISceneTransitionGuard
{
    [SerializeField] LawSchoolAlarmPrototype alarm;
    [SerializeField] BoxCollider solidBarrier;
    [SerializeField, Min(.5f)] float warningDistance = 3;
    [SerializeField, TextArea(2,3)] string warningText =
        "SAÍDA BLOQUEADA PELO ALARME\nDesligue o alarme no quadro de energia da manutenção.";

    BoxCollider doorway;

    public bool IsTransitionBlocked => alarm && alarm.IsActive;
    public bool WarningVisible { get; private set; }
    public string WarningText => warningText;

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
            UniDoom.UI.GameplayNotice.Draw(UniDoom.UI.GameplayNotice.LockedDoorMessage + "\n" + warningText);
    }

    void OnDisable()
    {
        if (alarm) alarm.StateChanged -= SyncBarrier;
        WarningVisible = false;
        // Disabling the notice must not remove an active physical lock.
        SyncBarrier();
    }
}
