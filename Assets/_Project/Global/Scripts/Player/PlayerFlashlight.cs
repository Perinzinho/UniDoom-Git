using System.Collections;
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
    Coroutine flickerRoutine;
    int flickerToken;
    float nextFlickerAt = -1f;
    const float BeamIntensity = 65f;
    const float FlickerDimFactor = .15f;
    const float MinFlickerInterval = 1f;
    const float MaxFlickerInterval = 60f;
    LibraryLightingController campusLibrary;
    LibraryEncounterDirector oldLibrary;
    Block4PowerFluctuation block4;
    LawSchoolAlarmPrototype lawSchool;

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
            lawSchool = FindAnyObjectByType<LawSchoolAlarmPrototype>();
            wasDark = false;
            StopFlicker();
        }

        bool libraryUnpowered = campusLibrary && campusLibrary.IsUnpowered;
        bool dark = libraryUnpowered ||
                    (oldLibrary && oldLibrary.BlackoutActive) ||
                    (block4 && block4.IsBlackout) ||
                    (lawSchool && lawSchool.IsActive);
        if (dark && !wasDark) requested = true;
        wasDark = dark;

        bool gameplayActive = Time.timeScale > 0f && !GameMenus.BlocksGameplay &&
                              !DeathScreen.IsVisible && !DoorReloadScreen.IsReloading;
        if (!gameplayActive)
        {
            if (flickerRoutine != null || nextFlickerAt >= 0f) StopFlicker();
            return;
        }

        if ((Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame))
        {
            requested = !requested;
            StopFlicker();
        }

        if (requested && !beam) CreateBeam();

        if (beam)
        {
            beam.enabled = requested;
            if (requested && flickerRoutine == null)
            {
                if (nextFlickerAt < 0f)
                    nextFlickerAt = Time.time + Random.Range(MinFlickerInterval, MaxFlickerInterval);
                if (Time.time >= nextFlickerAt)
                    flickerRoutine = StartCoroutine(FlickerRoutine(++flickerToken));
            }
        }
    }

    bool CanFlicker(int token) => token == flickerToken && beam && requested &&
                                  Time.timeScale > 0f && !GameMenus.BlocksGameplay &&
                                  !DeathScreen.IsVisible && !DoorReloadScreen.IsReloading;

    IEnumerator FlickerRoutine(int token)
    {
        int blinks = Random.Range(2, 7);
        for (int i = 0; i < blinks; i++)
        {
            if (!CanFlicker(token)) break;
            beam.intensity = BeamIntensity * FlickerDimFactor;
            yield return new WaitForSeconds(Random.Range(.04f, .12f));
            if (!CanFlicker(token)) break;
            beam.intensity = BeamIntensity;
            yield return new WaitForSeconds(Random.Range(.07f, .2f));
        }
        if (token != flickerToken) yield break;
        beam.intensity = BeamIntensity;
        flickerRoutine = null;
        nextFlickerAt = Time.time + Random.Range(MinFlickerInterval, MaxFlickerInterval);
    }

    void StopFlicker()
    {
        flickerToken++;
        if (flickerRoutine != null)
        {
            StopCoroutine(flickerRoutine);
            flickerRoutine = null;
        }
        if (beam) beam.intensity = BeamIntensity;
        nextFlickerAt = -1f;
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
        beam.range = 30f;
        beam.spotAngle = 58f;
        beam.innerSpotAngle = 32f;
        beam.intensity = BeamIntensity;
        beam.shadows = LightShadows.None;
        beam.renderMode = LightRenderMode.ForcePixel;
        beam.cullingMask = view.cullingMask;
        var urp = beam.GetUniversalAdditionalLightData();
        urp.usePipelineSettings = false;
        urp.additionalLightsShadowResolutionTier = 1;
    }

    void OnDisable()
    {
        StopFlicker();
        if (current == this) current = null;
        if (beam) beam.enabled = false;
    }
    void OnDestroy()
    {
        StopFlicker();
        if (beam) Destroy(beam.gameObject);
    }
}
