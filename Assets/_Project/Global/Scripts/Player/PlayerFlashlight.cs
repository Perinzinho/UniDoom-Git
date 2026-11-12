using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UniDoom.CampusBlock4;
using UniDoom.CampusLibrary;
using UniDoom.UI;

/// <summary>A camera-aimed light for scenes whose normal lighting fails.</summary>
[DisallowMultipleComponent]
public sealed class PlayerFlashlight : MonoBehaviour
{
    static PlayerFlashlight current;
    public static bool HandlesCurrentDarkness => current && current.isActiveAndEnabled;
    Camera view;
    Light beam;
    Scene boundScene;
    bool sceneBound;
    bool wasDark;
    bool requested;
    LibraryLightingController campusLibrary;
    LibraryEncounterDirector oldLibrary;
    Block4PowerFluctuation block4;

    void OnEnable() { current = this; }

    void Update()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!sceneBound || activeScene != boundScene)
        {
            boundScene = activeScene;
            sceneBound = true;
            campusLibrary = FindAnyObjectByType<LibraryLightingController>();
            oldLibrary = FindAnyObjectByType<LibraryEncounterDirector>();
            block4 = FindAnyObjectByType<Block4PowerFluctuation>();
            wasDark = false;
        }

        bool dark = (campusLibrary && campusLibrary.IsUnpowered) ||
                    (oldLibrary && oldLibrary.BlackoutActive) ||
                    (block4 && block4.IsBlackout);
        if (dark && !wasDark) requested = true;
        wasDark = dark;

        if (Time.timeScale > 0 && !GameMenus.BlocksGameplay &&
            !DeathScreen.IsVisible && !DoorReloadScreen.IsReloading &&
            ((Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) ||
             (Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame)))
            requested = !requested;

        if (requested && !beam) CreateBeam();
        if (beam) beam.enabled = requested;
    }

    void CreateBeam()
    {
        view = Camera.main;
        if (!view) view = GetComponentInChildren<Camera>(true);
        if (!view) return;

        var go = new GameObject("Player camera flashlight");
        go.transform.SetParent(view.transform, false);
        go.transform.localPosition = new Vector3(.06f, -.08f, .12f);
        // Offset the lamp slightly from the eye, while centering its cone on the crosshair.
        go.transform.localRotation = Quaternion.LookRotation(new Vector3(-.06f, .08f, 20f), Vector3.up);
        beam = go.AddComponent<Light>();
        beam.type = LightType.Spot;
        beam.color = new Color(1f, .94f, .82f);
        beam.range = 20f;
        beam.spotAngle = 58f;
        beam.innerSpotAngle = 32f;
        beam.intensity = 80f;
        beam.shadows = LightShadows.None;
        beam.renderMode = LightRenderMode.ForcePixel;
        beam.cullingMask = view.cullingMask;
        var urp = beam.GetUniversalAdditionalLightData();
        urp.usePipelineSettings = false;
        urp.additionalLightsShadowResolutionTier = 1;
    }

    void OnDisable()
    {
        if (current == this) current = null;
        if (beam) beam.enabled = false;
    }
    void OnDestroy() { if (beam) Destroy(beam.gameObject); }
}
