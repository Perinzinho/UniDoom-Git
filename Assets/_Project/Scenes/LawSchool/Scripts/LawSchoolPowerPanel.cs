using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Physical, aimed interaction with the maintenance cabinet, after the key alarm begins.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
public sealed class LawSchoolPowerPanel : MonoBehaviour
{
    [SerializeField] LawSchoolAlarmPrototype alarm;
    [SerializeField, Min(.5f)] float interactionDistance = 3;
    [SerializeField] Transform mainSwitch;
    Camera view;
    bool showPrompt;
    GUIStyle promptStyle;

    public bool CanUse(PlayerPickupKey player)
    {
        if (!isActiveAndEnabled || !alarm || !alarm.IsActive || !player || Time.timeScale <= 0 ||
            UniDoom.UI.GameMenus.BlocksGameplay) return false;
        var life = player.GetComponent<PlayerLife>();
        if (life && life.IsDead) return false;
        if (!view) view = Camera.main;
        return view && Physics.Raycast(view.transform.position,view.transform.forward,out var hit,
            interactionDistance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore) &&
            hit.collider.transform.IsChildOf(transform);
    }

    public bool TryUse(PlayerPickupKey player) => CanUse(player) && alarm.TryResolve(player);

    void Update()
    {
        if (mainSwitch)
        {
            var position = Quaternion.Euler(alarm && alarm.IsResolved ? 32 : -32, 0, 0);
            mainSwitch.localRotation = Quaternion.RotateTowards(mainSwitch.localRotation,position,360*Time.deltaTime);
        }
        var player = GameManager.SessionPlayer;
        var keys = player ? player.GetComponent<PlayerPickupKey>() : null;
        showPrompt = CanUse(keys);
        if (!showPrompt) return;
        if ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame))
        {
            if (TryUse(keys)) showPrompt = false;
        }
    }

    void OnGUI()
    {
        if (!showPrompt || !alarm || !alarm.IsActive) return;
        if (promptStyle == null) promptStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
        GUI.Box(new Rect((Screen.width-330)*.5f,Screen.height*.77f,330,38),"[E]  DESLIGAR ALARME",promptStyle);
    }
}
