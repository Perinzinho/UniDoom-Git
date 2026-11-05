using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniDoom.UI;

// Opt-in temporary integration checks. Results go to Temp; edit-mode scenes are never saved.
[InitializeOnLoad]
public static class AxePlayModeChecks
{
    const string Active = "UniDoom.AxeChecks";
    const string Request = "Temp/run-axe-checks.txt";
    const string AudioRequest = "Temp/run-axe-audio-checks.txt";
    const string AudioOnly = "UniDoom.AxeAudioChecks";
    const string BalanceRequest = "Temp/run-axe-balance-checks.txt";
    const string BalanceOnly = "UniDoom.AxeBalanceChecks";
    static Enemy balanceEnemy;
    static PlayerLife balanceLife;
    static float enemyHealth;
    static int enemyHits;
    static AudioSource swingAudio;
    static readonly List<string> results = new();
    static int step;
    static double next, deadline;
    static bool background;
    static bool backgroundCaptured;
    static PlayerWeaponManager weapons;
    static Axe axe;
    static Gun pistol, shotgun;
    static WeaponPickup droppedPistol;
    static WeaponSpriteAnimator animator;
    static Image weaponImage;
    static AxeCheckTarget target;
    static GameObject wall;
    static Camera testCamera;
    static int ammo, reserve;
    static AsyncOperation travel;
    static double lastStatus;
    static float gameReadyAt;

    static AxePlayModeChecks() => EditorApplication.update += Tick;

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (File.Exists(BalanceRequest) && Application.isPlaying && !SessionState.GetBool(Active, false))
        {
            File.Move(BalanceRequest, "Temp/axe-balance-inplace-" + DateTime.UtcNow.Ticks + ".consumed");
            ValidateBalanceInPlace();
            return;
        }
        if (!SessionState.GetBool(Active, false))
        {
            string request = File.Exists(BalanceRequest) ? BalanceRequest : File.Exists(AudioRequest) ? AudioRequest : Request;
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Move(request, "Temp/axe-check-request-" + DateTime.UtcNow.Ticks + ".consumed");
            SessionState.SetBool(AudioOnly, request == AudioRequest);
            SessionState.SetBool(BalanceOnly, request == BalanceRequest);
            SessionState.SetBool(Active, true);
            EditorApplication.isPlaying = true;
            return;
        }
        if (!EditorApplication.isPlaying)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = true;
            return;
        }
        if (EditorApplication.timeSinceStartup - lastStatus > 1)
        {
            lastStatus = EditorApplication.timeSinceStartup;
            File.WriteAllText("Temp/axe-check-state.txt", "Play Mode active. Step=" + step);
        }
        EditorApplication.QueuePlayerLoopUpdate();
        if (next == 0) { next = EditorApplication.timeSinceStartup + 5; deadline = next + 240; }
        if (EditorApplication.timeSinceStartup < next || Time.time < gameReadyAt) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timeout at step " + step);
            Run();
        }
        catch (Exception e) { results.Add("FAIL: " + e); Finish(); }
    }

    static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        results.Add("PASS: " + message);
    }

    static void Advance(double wait = .15, bool gameTime = false)
    {
        step++;
        next = EditorApplication.timeSinceStartup + (gameTime ? .01 : wait);
        gameReadyAt = gameTime ? Time.time + (float)wait : 0;
        File.WriteAllLines("Temp/axe-check-progress.txt", results.Concat(new[] { "Step=" + step }));
    }

    static void Run()
    {
        if (SessionState.GetBool(BalanceOnly, false)) { RunBalance(); return; }
        if (SessionState.GetBool(AudioOnly, false)) { RunAudio(); return; }
        // Editor rendering can lag wall time; await actual swing recovery before the next scenario.
        if (axe != null && axe.IsAttacking && ((step >= 4 && step <= 8) || step == 10)) return;
        if (step >= 13 && step <= 20)
        {
            var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/_Project/Global/Prefabs/Weapons/AxeDefinition.asset");
            weaponImage.sprite = definition.ShootFrames[step - 13];
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/axe-pose-" + (step - 13) + ".png"));
            Advance(.3);
            return;
        }
        if (step == 21) { animator.enabled = true; Finish(); return; }
        switch (step)
        {
            case 0:
                if (!backgroundCaptured)
                {
                    background = Application.runInBackground;
                    backgroundCaptured = true;
                }
                Application.runInBackground = true;
                if (GameMenus.Instance == null) return;
                GameMenus.Instance.StartNewGame();
                Advance(3);
                break;
            case 1:
                if (GameManager.SessionPlayer == null || PlayerWeaponManager.InputBlocked) return;
                var player = GameManager.SessionPlayer;
                player.MovementEnabled = false;
                foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>()) enemy.enabled = false;
                weapons = player.GetComponent<PlayerWeaponManager>();
                axe = weapons.Axe;
                pistol = weapons.CurrentGun;
                Check(axe != null && pistol is Pistol, "Player starts with permanent axe and pistol");
                Check(weapons.GetComponentsInChildren<Gun>(true).Length == 1, "Exactly one carried firearm");
                animator = UnityEngine.Object.FindAnyObjectByType<WeaponSpriteAnimator>();
                weaponImage = (Image)typeof(WeaponSpriteAnimator).GetField("weaponImage", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(animator);
                ammo = pistol.CurrentAmmo;
                reserve = pistol.ReserveAmmo;
                Check(weapons.EquipAxe() && !pistol.gameObject.activeSelf, "Slot 1 equips axe and holsters pistol");
                Check(weaponImage.sprite.name == "Axe_00", "Axe idle sprite is bound to HUD");
                Canvas.ForceUpdateCanvases();
                var parentRect = (RectTransform)weaponImage.rectTransform.parent;
                Check(weaponImage.rectTransform.rect.height / parentRect.rect.height > .60f,
                    "Axe animation canvas occupies over 60 percent of screen height");
                CheckAxeFrames();
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/axe-large-idle.png"));
                Advance(1);
                break;
            case 2:
                player = GameManager.SessionPlayer;
                Check(weapons.CurrentGun == pistol, "Ammo systems still address holstered firearm");
                pistol.TryShoot();
                Check(pistol.CurrentAmmo == ammo, "Holstered firearm cannot shoot");
                var ammoObject = new GameObject("Axe check ammo", typeof(BoxCollider), typeof(AmmoPickup));
                ammoObject.transform.position = Vector3.one * 8000;
                Check(ammoObject.GetComponent<AmmoPickup>().TryCollect(player.GetComponent<CharacterController>()), "Ammo is collected with axe equipped");
                Check(pistol.ReserveAmmo == reserve + 10, "Collected ammo goes to holstered pistol");
                Check(weapons.EquipFirearm(), "Slot 2 restores pistol");
                Check(pistol.CurrentAmmo == ammo && pistol.ReserveAmmo == reserve + 10, "Switching preserves magazine and reserve");
                pistol.TryShoot();
                Check(pistol.CurrentAmmo == ammo - 1, "Pistol still shoots after switching");
                pistol.TryReload();
                Check(pistol.GetComponent<Reloading>().IsReloading, "Pistol starts reload");
                Check(weapons.EquipAxe() && !pistol.GetComponent<Reloading>().IsReloading, "Switching to axe cancels reload");
                Time.timeScale = 0;
                Check(!weapons.EquipFirearm() && !axe.TryAttack(), "Pause blocks switching and melee");
                Time.timeScale = 1;
                var pickupPrefab = AssetDatabase.LoadAssetAtPath<WeaponPickup>("Assets/_Project/Global/Prefabs/Weapons/ShotgunPickup.prefab");
                var pickup = UnityEngine.Object.Instantiate(pickupPrefab, Vector3.one * 8000, Quaternion.identity);
                Check(weapons.TryPickup(pickup), "Shotgun pickup replaces the carried pistol while axe is equipped");
                shotgun = weapons.CurrentGun;
                Check(shotgun is Shotgun && weapons.IsAxeEquipped && weapons.Axe == axe, "Axe stays equipped and permanent after pickup");
                Check(weapons.GetComponentsInChildren<Gun>(true).Length == 1, "Pickup leaves exactly one firearm in inventory");
                droppedPistol = UnityEngine.Object.FindObjectsByType<WeaponPickup>().Single(x =>
                    x.GetComponentsInChildren<Gun>(true).Any(g => g == pistol));
                Check(pistol.CurrentAmmo == ammo - 1 && pistol.ReserveAmmo == reserve + 10, "Dropped pistol retains ammunition");
                Check(!weapons.TryPickup(droppedPistol), "Dropped weapon blocks immediate recollection by owner");
                // An isolated main camera keeps melee physics away from scene geometry and Cinemachine.
                foreach (Camera camera in Camera.allCameras)
                {
                    camera.tag = "Untagged";
                    camera.enabled = false;
                }
                testCamera = new GameObject("Axe physics camera", typeof(Camera)).GetComponent<Camera>();
                testCamera.tag = "MainCamera";
                testCamera.transform.position = new Vector3(9000, 9000, 9000);
                testCamera.transform.rotation = Quaternion.identity;
                axe.SetOwner(testCamera.transform); // Match the isolated combat origin.
                var targetObject = new GameObject("Axe damage target", typeof(BoxCollider), typeof(AxeCheckTarget));
                target = targetObject.GetComponent<AxeCheckTarget>();
                targetObject.GetComponent<BoxCollider>().size = Vector3.one * .6f;
                target.transform.position = MeleeRay().GetPoint(1f);
                Physics.SyncTransforms();
                Check(axe.TryAttack() && !axe.TryAttack(), "One swing starts; repeated clicks are blocked during cooldown");
                Check(!weapons.EquipFirearm(), "Cannot cancel a swing by switching weapons");
                Check(target.Hits == 0, "Swing has delayed impact");
                Advance(.35, true);
                break;
            case 3:
                Check(target.Hits == 1 && Mathf.Approximately(target.Damage, 55), "Axe deals one 55-damage melee hit");
                Check(weaponImage.sprite.name.StartsWith("Axe_") && weaponImage.sprite.name != "Axe_00", "Swing plays sprites from the imported sheet");
                Advance(.6, true);
                break;
            case 4:
                Check(!axe.IsAttacking && weaponImage.sprite.name == "Axe_00", "Swing finishes and HUD returns to idle");
                target.transform.position = CrosshairAim.CreateRay(testCamera).GetPoint(4);
                Physics.SyncTransforms();
                Check(axe.TryAttack(), "Next swing is available after recovery");
                Advance(.9, true);
                break;
            case 5:
                Check(target.Hits == 1, "Axe cannot hit a target outside melee range");
                target.transform.position = MeleeRay().GetPoint(1f);
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "Axe obstruction check";
                wall.transform.position = MeleeRay().GetPoint(.45f);
                wall.transform.localScale = new Vector3(2, 2, .2f);
                Physics.SyncTransforms();
                Check(axe.TryAttack(), "Obstructed swing starts");
                Advance(.9, true);
                break;
            case 6:
                Check(target.Hits == 1, "Wall blocks melee damage");
                UnityEngine.Object.Destroy(wall);
                target.IsDead = true;
                Advance();
                break;
            case 7:
                Check(axe.TryAttack(), "Swing at dead target starts");
                Advance(.9, true);
                break;
            case 8:
                Check(target.Hits == 1, "Dead targets do not receive extra hits");
                target.IsDead = false;
                Check(axe.TryAttack(), "Pending swing starts before pause");
                Time.timeScale = 0;
                Advance(.2);
                break;
            case 9:
                Check(!axe.IsAttacking, "Pause cancels pending melee impact");
                Check(weaponImage.sprite.name == "Axe_00", "Canceling a swing immediately restores idle pose");
                Time.timeScale = 1;
                Advance(.9, true);
                break;
            case 10:
                Check(target.Hits == 1, "Resuming never delivers canceled impact");
                Check(weapons.EquipFirearm() && weapons.CurrentGun == shotgun, "Shotgun can be equipped after axe recovery");
                shotgun.TryShoot();
                ammo = shotgun.CurrentAmmo;
                shotgun.TryReload();
                Check(weapons.EquipAxe() && !shotgun.GetComponent<Reloading>().IsReloading, "Switching cancels shotgun reload");
                Check(weapons.TryPickup(droppedPistol), "Player can recover dropped pistol after leaving pickup");
                Check(weapons.CurrentGun == pistol && weapons.Axe == axe, "Pistol recovery preserves same axe instance");
                var droppedShotgun = UnityEngine.Object.FindObjectsByType<WeaponPickup>().Single(x =>
                    x.GetComponentsInChildren<Gun>(true).Any(g => g == shotgun));
                Check(shotgun.CurrentAmmo == ammo, "Dropped shotgun retains spent-shell count");
                Check(weapons.GetComponentsInChildren<Gun>(true).Length == 1, "Repeated pickups still enforce one firearm");
                axe.SetOwner(weapons.transform);
                UnityEngine.Object.Destroy(testCamera.gameObject);
                UnityEngine.Object.Destroy(target.gameObject);
                travel = SceneManager.LoadSceneAsync("Cylinder");
                Advance(2);
                break;
            case 11:
                if (!travel.isDone) return;
                Check(weapons != null && weapons.Axe == axe && weapons.CurrentGun == pistol,
                    "Axe and single firearm persist between scenes");
                Check(weapons.IsAxeEquipped, "Selected axe persists between scenes");
                Check(weapons.EquipFirearm() && weapons.EquipAxe(), "Both slots work after scene travel");
                // Return to a real scene camera for a visual check of the persistent HUD.
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/axe-gameplay.png"));
                Advance(1);
                break;
            case 12:
                // Freeze only the HUD animator for a contact sheet of its saved, aligned poses.
                animator.enabled = false;
                Advance();
                break;
        }
    }

    static Ray MeleeRay() => new Ray(testCamera.transform.position, CrosshairAim.CreateRay(testCamera).direction);

    static void SetField(object instance, string name, object value)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);

    // Synchronous physics checks in a remote disposable arena. The running session's
    // player, camera, health, ammunition and scene stay in place; only axe tuning is applied.
    static void ValidateBalanceInPlace()
    {
        var checks = new List<string>();
        GameObject arena = null;
        float originalTimeScale = Time.timeScale;
        var randomState = UnityEngine.Random.state;
        void Verify(bool ok, string message)
        {
            if (!ok) throw new Exception(message);
            checks.Add("PASS: " + message);
        }
        try
        {
            Time.timeScale = 1;
            var axePrefab = AssetDatabase.LoadAssetAtPath<Axe>("Assets/_Project/Global/Prefabs/Weapons/AxeEquipped.prefab");
            var tuning = new SerializedObject(axePrefab);
            foreach (var liveAxe in UnityEngine.Object.FindObjectsByType<Axe>(FindObjectsInactive.Include))
            {
                if (liveAxe.GetComponentInParent<PlayerWeaponManager>() == null) continue;
                var live = new SerializedObject(liveAxe);
                foreach (string field in new[] { "damage", "range", "hitRadius", "hitHeight" })
                    live.FindProperty(field).floatValue = tuning.FindProperty(field).floatValue;
                live.ApplyModifiedPropertiesWithoutUndo();
            }
            arena = new GameObject("[Temporary axe balance validation]");
            arena.hideFlags = HideFlags.HideAndDontSave;
            Vector3 origin = new Vector3(12000, 9000, 12000);
            var defender = new GameObject("Isolated player", typeof(CharacterController), typeof(PlayerLife));
            defender.transform.SetParent(arena.transform);
            defender.transform.position = origin;
            var life = defender.GetComponent<PlayerLife>();
            var cameraObject = new GameObject("Isolated camera", typeof(Camera));
            cameraObject.transform.SetParent(arena.transform);
            cameraObject.transform.position = origin;
            var camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            var weapon = UnityEngine.Object.Instantiate(axePrefab, defender.transform);
            weapon.SetOwner(defender.transform);
            SetField(weapon, "playerCamera", camera);
            var hit = typeof(Axe).GetMethod("Hit", BindingFlags.Instance | BindingFlags.NonPublic);
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<Enemy>("Assets/_Project/Global/Prefabs/Enemy/Enemy.prefab");
            var enemy = UnityEngine.Object.Instantiate(enemyPrefab, origin + Vector3.forward * 1.55f,
                Quaternion.Euler(0, 180, 0), arena.transform);
            var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            foreach (var behaviour in enemy.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            var body = enemy.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var capsule = enemy.GetComponent<CapsuleCollider>();
            capsule.center = Vector3.zero;
            capsule.height = 2;
            capsule.radius = .588f;
            SetField(enemy, "spriteAnimator", null);
            SetField(enemy, "target", defender.transform);
            SetField(enemy, "targetLife", life);
            SetField(enemy, "targetController", defender.GetComponent<CharacterController>());
            SetField(enemy, "nextPerception", float.MaxValue);
            SetField(enemy, "nextTargetSearch", float.MaxValue);
            SetField(enemy, "nearbyCuePlayed", true);
            SetField(enemy, "moveSpeed", 0f);
            SetField(enemy, "lastAttackTime", Time.time - 10);
            typeof(Enemy).GetProperty("State").GetSetMethod(true).Invoke(enemy, new object[] { Enemy.BehaviourState.Chasing });
            var enemyUpdate = typeof(Enemy).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            float health = 100;
            int hits = 0;
            enemy.OnHealthChanged.AddListener((value, max) => { health = value; hits++; });
            Physics.SyncTransforms();
            Verify(tuning.FindProperty("damage").floatValue == 55 && Mathf.Approximately(tuning.FindProperty("range").floatValue, 1.5f),
                "Shared prefab has 55 damage and 1.5 reach; active session axes receive the same tuning");
            hit.Invoke(weapon, null);
            Verify(hits == 0, "Axe cannot hit a wide zombie standing outside its 1.5-unit bite range");
            enemyUpdate.Invoke(enemy, null);
            Verify(life.CurrentHealth == life.MaxHealth, "Zombie cannot bite from outside its own range");
            camera.nearClipPlane = .6f;
            hit.Invoke(weapon, null);
            Verify(hits == 0, "Increasing the camera near plane does not extend axe reach");
            enemy.transform.position = origin + Vector3.forward * 1.45f;
            var barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.transform.SetParent(arena.transform);
            barrier.transform.position = origin + Vector3.forward * .35f;
            barrier.transform.localScale = new Vector3(2, 2, .1f);
            Physics.SyncTransforms();
            hit.Invoke(weapon, null);
            Verify(hits == 0, "Short-range axe remains blocked by walls");
            enemyUpdate.Invoke(enemy, null);
            Verify(life.CurrentHealth == life.MaxHealth, "Wall also blocks the zombie's attack");
            UnityEngine.Object.DestroyImmediate(barrier);
            Physics.SyncTransforms();
            enemyUpdate.Invoke(enemy, null);
            Verify(life.CurrentHealth < life.MaxHealth, "A real zombie can damage the player inside the axe's effective reach");
            hit.Invoke(weapon, null);
            Verify(hits == 1 && Mathf.Approximately(health, 45) && !enemy.IsDead, "Axe connects at 1.45 units and deals 55 damage to a 100-health zombie");
            hit.Invoke(weapon, null);
            Verify(hits == 2 && enemy.IsDead, "A standard zombie dies in exactly two axe hits");
            Verify(life.CurrentHealth < life.MaxHealth, "Damage taken remains after winning the melee exchange");

            UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            var appearance = AssetDatabase.LoadAssetAtPath<EnemySpriteAppearance>(
                "Assets/_Project/Global/Prefabs/Enemy/WheelchairZombieAppearance.asset");
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Global/Prefabs/Player/Player.prefab");
            float eyeOffset = playerPrefab.transform.Find("Head/FirstPersonCamera").position.y - playerPrefab.transform.position.y;
            camera.transform.position = origin + Vector3.up * eyeOffset;
            camera.nearClipPlane = .3f;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(arena.transform);
            floor.transform.position = origin + Vector3.down * 1.1f;
            floor.transform.localScale = new Vector3(10, .2f, 10);
            var seated = UnityEngine.Object.Instantiate(enemyPrefab, origin + new Vector3(0, -.16f, 1.55f),
                Quaternion.Euler(0, 180, 0), arena.transform);
            var seatedAgent = seated.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (seatedAgent != null) seatedAgent.enabled = false;
            foreach (var behaviour in seated.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
            seated.GetComponent<Rigidbody>().isKinematic = true;
            seated.GetComponent<Rigidbody>().useGravity = false;
            var seatedCollider = seated.GetComponent<CapsuleCollider>();
            seatedCollider.height = appearance.colliderHeight;
            seatedCollider.radius = appearance.colliderRadius;
            seatedCollider.center = Vector3.up * (-1f + appearance.colliderHeight * .5f);
            SetField(seated, "spriteAnimator", null);
            float seatedHealth = 100;
            int seatedHits = 0;
            seated.OnHealthChanged.AddListener((value, max) => { seatedHealth = value; seatedHits++; });
            Physics.SyncTransforms();
            hit.Invoke(weapon, null);
            Verify(seatedHits == 0, "Larger vertical hitbox cannot reach the seated zombie from outside bite range");
            seated.transform.position = origin + new Vector3(0, -.16f, 1.45f);
            var seatedWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seatedWall.transform.SetParent(arena.transform);
            seatedWall.transform.position = origin + Vector3.forward * .35f;
            seatedWall.transform.localScale = new Vector3(2, 3, .1f);
            Physics.SyncTransforms();
            hit.Invoke(weapon, null);
            Verify(seatedHits == 0, "Expanded swing cannot hit a seated zombie through a wall");
            UnityEngine.Object.DestroyImmediate(seatedWall);
            Physics.SyncTransforms();
            hit.Invoke(weapon, null);
            Verify(seatedHits == 1 && Mathf.Approximately(seatedHealth, 45),
                "Level aim from the real player's eye height hits the shorter wheelchair collider above a floor");
            seated.transform.position = origin + new Vector3(0, -.16f, .65f);
            Physics.SyncTransforms();
            hit.Invoke(weapon, null);
            Verify(seatedHits == 2 && seated.IsDead,
                "Point-blank overlap hits the seated zombie once per swing, preserving the two-hit kill");
        }
        catch (Exception e) { checks.Add("FAIL: " + e); }
        finally
        {
            if (arena != null) UnityEngine.Object.DestroyImmediate(arena);
            Time.timeScale = originalTimeScale;
            UnityEngine.Random.state = randomState;
            File.WriteAllLines("Temp/axe-balance-check-results.txt", checks);
        }
    }

    static void RunBalance()
    {
        switch (step)
        {
            case 0:
                if (!backgroundCaptured)
                {
                    background = Application.runInBackground;
                    backgroundCaptured = true;
                }
                Application.runInBackground = true;
                if (GameMenus.Instance == null) return;
                GameMenus.Instance.StartNewGame();
                Advance(3);
                break;
            case 1:
                if (GameManager.SessionPlayer == null || PlayerWeaponManager.InputBlocked) return;
                var player = GameManager.SessionPlayer;
                player.MovementEnabled = false;
                foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>()) enemy.enabled = false;
                player.Teleport(new Vector3(9000, 9000, 9000), Quaternion.identity);
                balanceLife = player.GetComponent<PlayerLife>();
                weapons = player.GetComponent<PlayerWeaponManager>();
                axe = weapons.Axe;
                Check(weapons.EquipAxe(), "Balanced axe equips normally");
                foreach (Camera camera in Camera.allCameras) { camera.enabled = false; camera.tag = "Untagged"; }
                testCamera = new GameObject("Melee balance camera", typeof(Camera)).GetComponent<Camera>();
                testCamera.tag = "MainCamera";
                testCamera.transform.SetPositionAndRotation(player.transform.position, Quaternion.identity);
                var prefab = AssetDatabase.LoadAssetAtPath<Enemy>("Assets/_Project/Global/Prefabs/Enemy/Enemy.prefab");
                balanceEnemy = UnityEngine.Object.Instantiate(prefab,
                    player.transform.position + Vector3.forward * 1.55f, Quaternion.Euler(0, 180, 0));
                var agent = balanceEnemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null) agent.enabled = false;
                var body = balanceEnemy.GetComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                foreach (var sprite in balanceEnemy.GetComponentsInChildren<EnemySpriteAnimator>()) sprite.enabled = false;
                SetField(balanceEnemy, "spriteAnimator", null);
                SetField(balanceEnemy, "moveSpeed", 0f);
                SetField(balanceEnemy, "patrolSpeed", 0f);
                // Use the widest ordinary zombie collider to test the most generous reach.
                var capsule = balanceEnemy.GetComponent<CapsuleCollider>();
                capsule.center = Vector3.zero;
                capsule.height = 2;
                capsule.radius = .588f;
                enemyHealth = 100;
                enemyHits = 0;
                balanceEnemy.OnHealthChanged.AddListener((health, max) => { enemyHealth = health; enemyHits++; });
                Physics.SyncTransforms();
                Check(axe.TryAttack(), "Swing starts outside zombie bite range");
                Advance(.9, true);
                break;
            case 2:
                Check(enemyHits == 0 && enemyHealth == 100, "Cannot damage even a wide zombie from outside its 1.5-unit bite range");
                Check(balanceLife.CurrentHealth == balanceLife.MaxHealth, "Zombie also cannot bite at that distance");
                testCamera.nearClipPlane = .6f;
                Check(axe.TryAttack(), "Second out-of-range swing starts with a larger camera near plane");
                Advance(.9, true);
                break;
            case 3:
                Check(enemyHits == 0, "Camera near-plane settings do not extend melee reach");
                balanceEnemy.transform.position = weapons.transform.position + Vector3.forward * 1.45f;
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = MeleeRay().GetPoint(.35f);
                wall.transform.localScale = new Vector3(2, 2, .1f);
                Physics.SyncTransforms();
                Check(axe.TryAttack(), "Close-range swing starts with an intervening wall");
                Advance(.9, true);
                break;
            case 4:
                Check(enemyHits == 0, "Short-range axe still cannot hit through walls");
                Check(balanceLife.CurrentHealth == balanceLife.MaxHealth, "Wall also blocks zombie attacks");
                UnityEngine.Object.Destroy(wall);
                Advance(.15, true);
                break;
            case 5:
                Check(axe.TryAttack(), "Close-range attack starts inside the zombie's bite range");
                Advance(.35, true);
                break;
            case 6:
                Check(enemyHits == 1 && Mathf.Approximately(enemyHealth, 45) && !balanceEnemy.IsDead,
                    "A close-range hit deals 55 damage to a real 100-health zombie");
                Check(balanceLife.CurrentHealth < balanceLife.MaxHealth,
                    "The living zombie can retaliate during the close-range exchange");
                Check(axe.IsAttacking && !weapons.EquipFirearm(), "Recovery still commits the player to the melee attack");
                Advance(.5, true);
                break;
            case 7:
                Check(axe.TryAttack(), "A second close-range swing becomes available after recovery");
                Advance(.35, true);
                break;
            case 8:
                Check(enemyHits == 2 && balanceEnemy.IsDead, "Exactly two axe hits kill a standard 100-health zombie");
                Check(balanceLife.CurrentHealth < balanceLife.MaxHealth, "The kill preserves the damage taken in the exchange");
                ValidateBalanceInPlace();
                results.AddRange(File.ReadAllLines("Temp/axe-balance-check-results.txt"));
                Finish();
                break;
        }
    }

    static void RunAudio()
    {
        switch (step)
        {
            case 0:
                if (!backgroundCaptured)
                {
                    background = Application.runInBackground;
                    backgroundCaptured = true;
                }
                Application.runInBackground = true;
                if (GameMenus.Instance == null) return;
                GameMenus.Instance.StartNewGame();
                Advance(3);
                break;
            case 1:
                if (GameManager.SessionPlayer == null || PlayerWeaponManager.InputBlocked) return;
                GameManager.SessionPlayer.MovementEnabled = false;
                weapons = GameManager.SessionPlayer.GetComponent<PlayerWeaponManager>();
                axe = weapons.Axe;
                Check(weapons.EquipAxe(), "Axe can be equipped with its attack sound");
                swingAudio = axe.GetComponent<AudioSource>();
                var clip = (AudioClip)typeof(Axe).GetField("swingSound", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(axe);
                Check(clip == AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/_Project/Global/Audio/Weapon/Axe/axe_swing_sfx_trimmed.wav"), "Prefab references the user's original swing recording");
                Check(clip.loadState == AudioDataLoadState.Loaded, "Swing audio is loaded before the first attack");
                Check(swingAudio != null && !swingAudio.playOnAwake && !swingAudio.loop &&
                    swingAudio.spatialBlend == 0 && !swingAudio.ignoreListenerPause, "Swing audio is 2D, non-looping and obeys menu audio pause");
                Check(!swingAudio.isPlaying, "Equipping the axe is silent");
                Check(axe.TryAttack() && !swingAudio.isPlaying, "Attack starts with its configured animation lead-in");
                Check(!axe.TryAttack(), "Blocked repeat clicks do not start another swing");
                Advance(.16, true);
                break;
            case 2:
                Check(swingAudio.isPlaying && swingAudio.clip.name == "axe_swing_sfx_trimmed",
                    "Swing recording plays during the attack animation");
                float cursor = swingAudio.time;
                Check(!axe.TryAttack() && Mathf.Approximately(cursor, swingAudio.time), "Repeated attack input does not restart the sound");
                Advance(.8, true);
                break;
            case 3:
                Check(!axe.IsAttacking && !swingAudio.isPlaying, "One attack finishes with no loop or leftover audio");
                Check(axe.TryAttack(), "A later attack can start normally");
                Time.timeScale = 0;
                Advance(.2);
                break;
            case 4:
                Check(!axe.IsAttacking && !swingAudio.isPlaying, "Pause cancels a pending sound before it starts");
                Time.timeScale = 1;
                Advance(.8, true);
                break;
            case 5:
                Check(!swingAudio.isPlaying && axe.TryAttack(), "Unpausing is silent and the next attack is available");
                Advance(.16, true);
                break;
            case 6:
                Check(swingAudio.isPlaying, "A new attack plays the recording again");
                axe.CancelAttack();
                Check(!swingAudio.isPlaying && !axe.IsAttacking, "Canceling an active attack immediately stops its sound");
                Finish();
                break;
        }
    }

    static void CheckAxeFrames()
    {
        var startField = typeof(Axe).GetField("swingStartedAt", BindingFlags.Instance | BindingFlags.NonPublic);
        var nextField = typeof(Axe).GetField("nextSwingAt", BindingFlags.Instance | BindingFlags.NonPublic);
        var update = typeof(WeaponSpriteAnimator).GetMethod("UpdateAxePose", BindingFlags.Instance | BindingFlags.NonPublic);
        Check(axe.TryAttack(), "Visual sampling starts a real axe attack");
        float[] times = { .035f, .13f, .23f, .30f, .37f, .435f, .525f, .65f };
        int[] poses = { 0, 1, 2, 3, 4, 5, 6, 0 };
        for (int i = 0; i < times.Length; i++)
        {
            startField.SetValue(axe, Time.time - times[i]);
            update.Invoke(animator, null);
            Check(weaponImage.sprite.name == "Axe_" + poses[i].ToString("00"),
                "Shared swing clock selects correct pose at " + times[i] + " seconds");
        }
        axe.CancelAttack();
        Check(weaponImage.sprite.name == "Axe_00", "Explicit cancellation resets the HUD immediately");
        nextField.SetValue(axe, Time.time);
    }

    static void Finish()
    {
        Application.runInBackground = background;
        File.WriteAllLines(SessionState.GetBool(BalanceOnly, false) ? "Temp/axe-balance-check-results.txt" :
            SessionState.GetBool(AudioOnly, false) ? "Temp/axe-audio-check-results.txt" : "Temp/axe-check-results.txt", results);
        SessionState.SetBool(Active, false);
        SessionState.SetBool(AudioOnly, false);
        SessionState.SetBool(BalanceOnly, false);
        EditorApplication.isPlaying = false;
        step = 0; next = deadline = 0; gameReadyAt = 0; backgroundCaptured = false; results.Clear();
    }
}

public sealed class AxeCheckTarget : MonoBehaviour, IDamageable
{
    public int Hits;
    public float Damage;
    public bool IsDead { get; set; }
    public void TakeDamage(float amount, GameObject source = null) { Hits++; Damage += amount; }
}
