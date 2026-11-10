using UnityEngine;
using UniDoom.Utils;

/// <summary>Two independent library locks, backed by the existing persistent player inventory.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(SceneTransitionDoor))]
public sealed class CampusLibraryEntranceLock : MonoBehaviour, ISceneTransitionGuard
{
    [SerializeField] BoxCollider solidBarrier;
    [SerializeField] GameObject lawSchoolPadlock, block4Padlock;
    [SerializeField, Min(.5f)] float warningDistance = 2.5f;
    const string WarningText = "Porta trancada, você precisa de uma chave.";
    GUIStyle warningStyle;
    public bool WarningVisible { get; private set; }

    public bool IsTransitionBlocked => !CampusLibraryAccess.CanEnter(Inventory);
    static PlayerCellKeys Inventory => GameManager.SessionPlayer ?
        GameManager.SessionPlayer.GetComponent<PlayerCellKeys>() : null;

    void OnEnable() => Refresh();
    void Update()
    {
        Refresh();
        WarningVisible = false;
        if (!IsTransitionBlocked || Time.timeScale <= 0 || UniDoom.UI.GameMenus.BlocksGameplay ||
            UniDoom.UI.DoorReloadScreen.IsReloading) return;
        var player = GameManager.SessionPlayer;
        if (!player || gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene()) return;
        var life = player.GetComponent<PlayerLife>();
        if (life && life.IsDead) return;
        var controller = player.GetComponent<CharacterController>();
        Vector3 position = controller ? controller.bounds.center : player.transform.position;
        WarningVisible = solidBarrier && solidBarrier.bounds.SqrDistance(position) <= warningDistance * warningDistance;
    }

    void Refresh()
    {
        var inventory = Inventory;
        bool blocked = !CampusLibraryAccess.CanEnter(inventory);
        if (solidBarrier) solidBarrier.enabled = blocked;
        if (lawSchoolPadlock) lawSchoolPadlock.SetActive(!inventory || !inventory.HasKey(CampusLibraryAccess.LawSchoolKeyId));
        if (block4Padlock) block4Padlock.SetActive(!inventory || !inventory.HasKey(CampusLibraryAccess.Block4KeyId));
    }

    void OnGUI()
    {
        if (!WarningVisible || !IsTransitionBlocked) return;
        warningStyle ??= new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            fontSize = Mathf.RoundToInt(19 * Mathf.Clamp(Screen.height / 900f, .8f, 1.6f))
        };
        float width = Mathf.Min(680, Screen.width - 40);
        GUI.Box(new Rect((Screen.width - width) * .5f, Screen.height * .76f, width, 64), WarningText, warningStyle);
    }

    void OnDisable() { WarningVisible = false; Refresh(); }
}
