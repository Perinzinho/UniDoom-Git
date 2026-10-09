using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SoundBank
{
    public AudioClip[] clips;
    [Range(0f, 1f)]     public float volume = 1f;
    [Range(0f, 0.5f)]   public float volumeVariation = 0.1f;
    [Range(0.5f, 1.5f)] public float basePitch = 1f;
    [Range(0f, 0.3f)]   public float pitchVariation = 0.08f;

    [NonSerialized] private List<int> order;
    [NonSerialized] private int position;
    [NonSerialized] private int lastIndex = -1;

    public bool IsEmpty => clips == null || clips.Length == 0;

    public float RandomVolume() =>
        volume * (1f - UnityEngine.Random.Range(0f, volumeVariation));

    public float RandomPitch() =>
        basePitch + UnityEngine.Random.Range(-pitchVariation, pitchVariation);

    public AudioClip Next()
    {
        if (IsEmpty) return null;
        if (clips.Length == 1) return clips[0];

        if (order == null || position >= order.Count)
            Refill();

        lastIndex = order[position++];
        return clips[lastIndex];
    }

    private void Refill()
    {
        order ??= new List<int>();
        order.Clear();
        position = 0;

        for (int i = 0; i < clips.Length; i++) order.Add(i);

        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        if (order.Count > 1 && order[0] == lastIndex)
        {
            int j = UnityEngine.Random.Range(1, order.Count);
            (order[0], order[j]) = (order[j], order[0]);
        }
    }
}