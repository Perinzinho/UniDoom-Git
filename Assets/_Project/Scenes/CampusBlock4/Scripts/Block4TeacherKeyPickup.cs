using UniDoom.UI;
using UnityEngine;

namespace UniDoom.CampusBlock4
{
    [RequireComponent(typeof(SphereCollider))]
    public sealed class Block4TeacherKeyPickup : MonoBehaviour
    {
        [SerializeField] Block4Keypad keypad;
        [SerializeField] Transform visual;
        Vector3 restPosition;
        void Awake()
        {
            GetComponent<SphereCollider>().isTrigger = true;
            if (visual != null) restPosition = visual.localPosition;
            if (Block4PuzzleProgress.HasLibraryAccessKey) gameObject.SetActive(false);
        }
        void Update()
        {
            if (Block4PuzzleProgress.HasLibraryAccessKey) { gameObject.SetActive(false); return; }
            if (visual == null) return;
            visual.localPosition = restPosition + Vector3.up * (Mathf.Sin(Time.time * 2) * .035f);
        }
        void OnTriggerEnter(Collider other) => TryCollect(other);
        void OnTriggerStay(Collider other) => TryCollect(other);
        bool TryCollect(Collider other)
        {
            if (!Block4PuzzleProgress.DoorUnlocked || Block4PuzzleProgress.HasLibraryAccessKey || keypad == null ||
                !keypad.LockedDoor.IsFullyOpen || Time.timeScale <= 0 || GameMenus.BlocksGameplay ||
                DeathScreen.IsVisible || DoorReloadScreen.IsReloading) return false;
            var player = other.GetComponentInParent<PlayerMovement>();
            if (player == null || player != GameManager.SessionPlayer ||
                (player.TryGetComponent<PlayerLife>(out var life) && life.IsDead)) return false;
            if (!Block4PuzzleProgress.CollectLibraryAccessKey()) return false;
            var cellKeys = player.GetComponent<PlayerCellKeys>();
            if (!cellKeys) cellKeys = player.gameObject.AddComponent<PlayerCellKeys>();
            cellKeys.TryAddKey(Block4PuzzleProgress.LibraryAccessKeyId);
            SFXManager.Instance?.PlayKeyPickupSound();
            keypad.ShowKeyCollected();
            FindAnyObjectByType<Block4PowerFluctuation>()?.BeginKeyBlackout();
            gameObject.SetActive(false);
            return true;
        }
    }
}
