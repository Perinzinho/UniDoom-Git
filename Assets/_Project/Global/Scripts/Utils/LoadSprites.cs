using UnityEngine;
using UnityEngine.UI;

public class LoadSprites : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private float tempoPorFrame = 0.1f;
    [SerializeField] private bool loop = true;

    private int indice;
    private float timer;

    void Start()
    {
        if (sprites != null && sprites.Length > 0)
            image.sprite = sprites[0];
    }

    void Update()
    {
        if (sprites == null || sprites.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= tempoPorFrame)
        {
            timer -= tempoPorFrame;
            indice++;

            if (indice >= sprites.Length)
            {
                if (!loop)
                {
                    indice = sprites.Length - 1;
                    return;
                }
                indice = 0;
            }

            image.sprite = sprites[indice];
        }
    }
}