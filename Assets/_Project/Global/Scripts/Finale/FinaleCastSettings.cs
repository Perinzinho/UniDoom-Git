using UnityEngine;

namespace UniDoom.Finale
{
    public sealed class FinaleCastSettings : ScriptableObject
    {
        [SerializeField] Sprite[] teacherSprites;
        [SerializeField] bool[] useBlackBackgroundMaterial;
        [SerializeField] Material blackBackgroundMaterial;

        public Sprite[] TeacherSprites => teacherSprites;
        public Material MaterialFor(int index) => blackBackgroundMaterial != null &&
            useBlackBackgroundMaterial != null && index >= 0 && index < useBlackBackgroundMaterial.Length &&
            useBlackBackgroundMaterial[index] ? blackBackgroundMaterial : null;
    }
}
