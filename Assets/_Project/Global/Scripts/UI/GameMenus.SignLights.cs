using UnityEngine;

namespace UniDoom.UI
{
    public sealed partial class GameMenus
    {
        struct SignLight
        {
            public float current;
            public float target;
            public float nextChange;
            public int burstSteps;
        }

        static readonly int LeftLightId = Shader.PropertyToID("_LeftLight");
        static readonly int RightLightId = Shader.PropertyToID("_RightLight");
        static readonly int PostLightsId = Shader.PropertyToID("_PostLights");
        static readonly int FifthPostLightId = Shader.PropertyToID("_FifthPostLight");

        Material signLightsMaterial;
        SignLight leftSignLight, rightSignLight;
        readonly SignLight[] postLights = new SignLight[5];

        void BuildSignLights()
        {
            // Resources keeps the shader available in player builds; this instance belongs only to this menu.
            var shader = Resources.Load<Shader>("Menus/MenuSignFlicker");
            if (shader == null)
            {
                Debug.LogWarning("Menu sign flicker shader is missing; the campus photo will remain unchanged.");
                return;
            }

            signLightsMaterial = new Material(shader)
            {
                name = "Menu sign lights (runtime)",
                hideFlags = HideFlags.DontSave
            };
            backdrop.material = signLightsMaterial;
            signLightsMaterial.SetFloat(LeftLightId, 1f);
            signLightsMaterial.SetFloat(RightLightId, 1f);
            signLightsMaterial.SetVector(PostLightsId, Vector4.one);
            signLightsMaterial.SetFloat(FifthPostLightId, 1f);
        }

        void SetSignLightsVisible(bool visible)
        {
            if (signLightsMaterial == null) return;

            // Options shares the photograph, but the requested effect belongs to the main screen.
            leftSignLight = NewSignLight(Time.unscaledTime);
            rightSignLight = NewSignLight(Time.unscaledTime);
            for (int i = 0; i < postLights.Length; i++)
            {
                postLights[i] = NewSignLight(Time.unscaledTime);
                postLights[i].nextChange += i * .65f;
            }
            signLightsMaterial.SetFloat(LeftLightId, 1f);
            signLightsMaterial.SetFloat(RightLightId, 1f);
            signLightsMaterial.SetVector(PostLightsId, Vector4.one);
            signLightsMaterial.SetFloat(FifthPostLightId, 1f);
            if (!visible) return;
            rightSignLight.nextChange += 1.7f;
        }

        static SignLight NewSignLight(float now)
        {
            return new SignLight
            {
                current = 1f,
                target = 1f,
                nextChange = now + Random.Range(1.8f, 4.8f)
            };
        }

        void UpdateSignLights()
        {
            if (CurrentScreen != Screen.Main || signLightsMaterial == null) return;

            float now = Time.unscaledTime;
            AdvanceSignLight(ref leftSignLight, now, false);
            AdvanceSignLight(ref rightSignLight, now, false);
            for (int i = 0; i < postLights.Length; i++)
                AdvanceSignLight(ref postLights[i], now, true);
            signLightsMaterial.SetFloat(LeftLightId, leftSignLight.current);
            signLightsMaterial.SetFloat(RightLightId, rightSignLight.current);
            signLightsMaterial.SetVector(PostLightsId, new Vector4(postLights[0].current,
                postLights[1].current, postLights[2].current, postLights[3].current));
            signLightsMaterial.SetFloat(FifthPostLightId, postLights[4].current);
        }

        static void AdvanceSignLight(ref SignLight light, float now, bool isPost)
        {
            if (now >= light.nextChange)
            {
                if (light.burstSteps == 0) light.burstSteps = Random.Range(isPost ? 1 : 2, isPost ? 3 : 4) * 2;

                bool dimming = light.target > .9f;
                light.target = dimming ? Random.Range(.06f, .35f) : 1f;
                light.burstSteps--;
                light.nextChange = now + (light.burstSteps == 0
                    ? Random.Range(isPost ? 7f : 3.5f, isPost ? 14f : 8f)
                    : dimming ? Random.Range(.07f, .16f) : Random.Range(.1f, .28f));
            }

            light.current = Mathf.MoveTowards(light.current, light.target, Time.unscaledDeltaTime * 18f);
        }
    }
}
