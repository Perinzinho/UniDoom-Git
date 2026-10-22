using UnityEngine;

/// <summary>Lives on the persistent player; a fresh player starts with an undiscovered archive.</summary>
[DisallowMultipleComponent]
public sealed class DirectorSecretMemory : MonoBehaviour
{
    public bool ArchiveOpened { get; private set; }
    public void RememberArchive() => ArchiveOpened = true;
}
