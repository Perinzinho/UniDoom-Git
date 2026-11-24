using UnityEngine;
using UniDoom.UI;
using TotalChaosMaps;

namespace UniDoom.CampusBlock4
{
    [DisallowMultipleComponent]
    public sealed class Block4PowerFluctuation : MonoBehaviour
    {
        [SerializeField] Light[] fixtures;
        [SerializeField] Renderer[] diffusers;
        [SerializeField] Vector2 interval = new Vector2(60, 90);
        [SerializeField] Vector2 firstEventDelay = new Vector2(30, 45);
        [SerializeField] Vector2 blackoutDuration = new Vector2(2, 4);
        [SerializeField, Range(0, 1)] float blackoutAmbientFraction = .07f;
        [SerializeField, Range(0, 1)] float blackoutReflection = .12f;
        [SerializeField, Range(0, 1)] float powerDownVolume = .85f;
        [SerializeField] AudioClip powerDownClip;
        float[] intensities;
        bool[] fixtureEnabled;
        MaterialPropertyBlock[] originalBlocks;
        MaterialPropertyBlock darkBlock;
        float remaining;
        int phase = -1;
        int pulses;
        TCRegionAtmosphere region;
        Color regionAmbient;
        float reflectionIntensity;
        AudioSource powerDownAudio;
        bool darkened;
        public bool IsBlackout => phase == 1;
        public event System.Action BlackoutStarted;

        void Awake()
        {
            intensities = new float[fixtures.Length];
            fixtureEnabled = new bool[fixtures.Length];
            for (int i=0;i<fixtures.Length;i++) if(fixtures[i])
            {
                intensities[i]=fixtures[i].intensity;
                fixtureEnabled[i]=fixtures[i].enabled;
            }
            originalBlocks = new MaterialPropertyBlock[diffusers.Length];
            for(int i=0;i<diffusers.Length;i++){originalBlocks[i]=new MaterialPropertyBlock();if(diffusers[i])diffusers[i].GetPropertyBlock(originalBlocks[i]);}
            darkBlock = new MaterialPropertyBlock();
            darkBlock.SetColor("_BaseColor",new Color(.025f,.028f,.024f));
            darkBlock.SetColor("_Color",new Color(.025f,.028f,.024f));
            darkBlock.SetColor("_EmissionColor",Color.black);
            region = FindAnyObjectByType<TCRegionAtmosphere>();
            regionAmbient = region ? region.ambient : RenderSettings.ambientLight;
            reflectionIntensity = RenderSettings.reflectionIntensity;
            powerDownAudio = CreatePowerDownAudio();
            remaining=Random.Range(firstEventDelay.x,firstEventDelay.y);
        }
        AudioSource CreatePowerDownAudio()
        {
            if(!powerDownClip)return null;
            var host=new GameObject("Block4 power-down audio");
            host.transform.SetParent(transform,false);
            var source=host.AddComponent<AudioSource>();
            source.playOnAwake=false;
            source.clip=powerDownClip;
            source.spatialBlend=0f;
            source.volume=powerDownVolume;
            return source;
        }
        void Update()
        {
            if(Time.timeScale<=0||GameMenus.BlocksGameplay||DeathScreen.IsVisible||DoorReloadScreen.IsReloading)return;
            remaining-=Time.deltaTime;if(remaining>0)return;
            if(phase<0){BeginFluctuation();return;}
            if(phase==0)
            {
                pulses++;SetPower(pulses%2==0);
                if(pulses>=6){phase=1;SetPower(false);remaining=Random.Range(blackoutDuration.x,blackoutDuration.y);EnterBlackout();BlackoutStarted?.Invoke();}
                else remaining=Random.Range(.07f,.16f);
                return;
            }
            RestorePower();phase=-1;remaining=Random.Range(interval.x,interval.y);
        }
        public void BeginFluctuation()
        {
            if(!isActiveAndEnabled||intensities==null)return;
            phase=0;pulses=0;SetPower(false);remaining=.1f;
        }
        void EnterBlackout()
        {
            if(darkened)return;
            darkened=true;
            if(region)region.ambient=regionAmbient*blackoutAmbientFraction;
            RenderSettings.reflectionIntensity=reflectionIntensity*blackoutReflection;
            if(powerDownAudio)powerDownAudio.PlayOneShot(powerDownClip);
        }
        void ExitBlackout()
        {
            if(!darkened)return;
            darkened=false;
            if(region)region.ambient=regionAmbient;
            RenderSettings.reflectionIntensity=reflectionIntensity;
        }
        void SetPower(bool on)
        {
            for(int i=0;i<fixtures.Length;i++)if(fixtures[i])
            {
                bool emergency = !on && i%12==0;
                fixtures[i].intensity = on ? intensities[i] : emergency ? intensities[i]*.035f : 0;
                fixtures[i].enabled = fixtureEnabled[i] && (on || emergency);
            }
            for(int i=0;i<diffusers.Length;i++)if(diffusers[i])diffusers[i].SetPropertyBlock(on?originalBlocks[i]:darkBlock);
        }
        void RestorePower(){ExitBlackout();if(intensities!=null)SetPower(true);}
        void OnDisable(){RestorePower();phase=-1;}
    }
}
