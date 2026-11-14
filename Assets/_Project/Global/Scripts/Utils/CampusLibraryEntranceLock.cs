using UnityEngine;
using UniDoom.Utils;

/// <summary>Two independent library locks, backed by the existing persistent player inventory.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(SceneTransitionDoor))]
public sealed class CampusLibraryEntranceLock : MonoBehaviour, ISceneTransitionGuard
{
    [SerializeField] BoxCollider solidBarrier;
    [SerializeField] GameObject lawSchoolPadlock, block4Padlock;
    [SerializeField, Min(.5f)] float warningDistance = 2.5f;
    const string WarningText = "Porta trancada! Procure pela chave.";
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
        // O estado do acesso é comunicado pela mensagem compartilhada; marcadores físicos
        // vermelhos sobre a fachada não fazem parte da linguagem visual das demais portas.
        if (lawSchoolPadlock) lawSchoolPadlock.SetActive(false);
        if (block4Padlock) block4Padlock.SetActive(false);
    }

    void OnGUI()
    {
        if (WarningVisible && IsTransitionBlocked && !UniDoom.UI.GameplayNotice.IsShowing)
            UniDoom.UI.GameplayNotice.Draw(WarningText);
    }

    void OnDisable() { WarningVisible = false; Refresh(); }
}
