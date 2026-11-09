using UnityEngine;
using UniDoom.UI;

namespace UniDoom.CampusBlock4
{
    [DisallowMultipleComponent]
    public sealed class Block4PowerFluctuation : MonoBehaviour
    {
        [SerializeField] Light[] fixtures;
        [SerializeField] Renderer[] diffusers;
        [SerializeField] Vector2 interval = new Vector2(45, 110);
        [SerializeField] Vector2 firstEventDelay = new Vector2(12, 24);
        [SerializeField] Vector2 blackoutDuration = new Vector2(2, 5);
        float[] intensities;
        MaterialPropertyBlock[] originalBlocks;
        MaterialPropertyBlock darkBlock;
        float remaining;
        int phase = -1;
        int pulses;
        public bool IsBlackout => phase == 1;
        public event System.Action BlackoutStarted;

        void Awake()
        {
            intensities = new float[fixtures.Length];
            for (int i=0;i<fixtures.Length;i++) if(fixtures[i]) intensities[i]=fixtures[i].intensity;
            originalBlocks = new MaterialPropertyBlock[diffusers.Length];
            for(int i=0;i<diffusers.Length;i++){originalBlocks[i]=new MaterialPropertyBlock();if(diffusers[i])diffusers[i].GetPropertyBlock(originalBlocks[i]);}
            darkBlock = new MaterialPropertyBlock();
            darkBlock.SetColor("_BaseColor",new Color(.025f,.028f,.024f));
            darkBlock.SetColor("_Color",new Color(.025f,.028f,.024f));
            darkBlock.SetColor("_EmissionColor",Color.black);
            remaining=Random.Range(firstEventDelay.x,firstEventDelay.y);
        }
        void Update()
        {
            if(Time.timeScale<=0||GameMenus.BlocksGameplay||DeathScreen.IsVisible||DoorReloadScreen.IsReloading)return;
            remaining-=Time.deltaTime;if(remaining>0)return;
            if(phase<0){BeginFluctuation();return;}
            if(phase==0)
            {
                pulses++;SetPower(pulses%2==0);
                if(pulses>=6){phase=1;SetPower(false);remaining=Random.Range(blackoutDuration.x,blackoutDuration.y);BlackoutStarted?.Invoke();}
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
        void SetPower(bool on)
        {
            for(int i=0;i<fixtures.Length;i++)if(fixtures[i])fixtures[i].intensity=on?intensities[i]:(i%12==0?intensities[i]*.035f:0);
            for(int i=0;i<diffusers.Length;i++)if(diffusers[i])diffusers[i].SetPropertyBlock(on?originalBlocks[i]:darkBlock);
        }
        void RestorePower(){if(intensities!=null)SetPower(true);}
        void OnDisable(){RestorePower();phase=-1;}
    }
}
