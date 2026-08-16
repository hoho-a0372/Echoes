# Prototype Build — Progress

**Repo policy:** Asset Store packages (e.g. `Modern GDR - Free icons pack`, `PlatformerSet1`) are gitignored, not committed — they're licensed third-party content, not this project's own work. Re-import them from the Asset Store / Package Manager locally rather than pulling them from git; see `.gitignore`'s "Asset Store purchases/downloads" section for the current list.

**Overall: Days 1–5 all code/scene work complete and verified.** Two things remain before this is a fully playable build: (1) import real `AudioClip` assets and drag them onto `AudioManager`'s 5 fields (Day 4's manual fallback — no code changes needed), and (2) a real in-Editor playtest of the full Title→Stage1→Stage2→Stage3→End flow, since headless MCP sessions can't drive a live cross-scene sequence (see Day 5's Testing note).

## Day 1
**Status: complete (built in a prior session of this same project).**

- [x] `HybridPlayerController.cs` — WASD movement (Rigidbody2D, gravity 0), fire trigger, `SetControlsEnabled(bool)`
- [x] `PingProjectile.cs` — straight-line launch, flash-and-destroy on wall hit, lifespan self-destruct
- [x] `StageExit.cs` — trigger-based Goal, disables player controls + shows Clear UI
- [x] Scene: Global Light 2D intensity 0, Player/Goal/PingProjectile prefab all built, Clear UI wired
- [x] Verified in Play mode: movement, wall-hit flash, Stage Clear trigger — all working

*(Original spec called for mouse-aim firing — this was superseded by Day 2.5 Patch 2 before this session; current `HybridPlayerController.cs` already uses WASD-facing aim, not mouse-aim. Not rebuilt to the "old" mouse-aim spec since the patch supersedes it.)*

### Error log
- See Day 2 error log — the significant Day 1-era bugs (legacy `Input` class throwing under this project's Input System–only setting, `Rigidbody2D.velocity` renamed to `linearVelocity` in Unity 6) were diagnosed and fixed in the prior session; both are permanently fixed in the current code (Input System throughout, `linearVelocity` used consistently).

## Day 2
**Status: complete (built in a prior session), including the 2.5 Patch.**

- [x] Tilemap walls: `Grid`/`Tilemap` with `TilemapCollider2D`, `Assets/Tiles/WallTile.asset` (white 32x32), tagged/layered `Wall`
- [x] Player ambient glow: always-on `Light2D` on Player (`outerRadius=0.5`, `intensity=0.3`), separate from the ping's flash light
- [x] Maze/level layout: current Stage 1 map is a large single room (30x15 interior) with two offset interior partitions forming a zigzag route, rather than a branching maze with dead ends — see Notes/decisions
- [~] Ping bounce (`maxBounces`): implemented, verified, **then deliberately removed again** by Day 2.5 Patch 1 — see below. Net result: no bounce in current code.

### Day 2.5 Patch (applied, superseding parts of Day 1/2)
- [x] **Patch 1 — Ping collision fix**: bounce mechanic fully deleted; `Ping`/`Wall` physics layers created, full collision matrix set (`Ping` collides only with `Wall`) via `Physics2D.IgnoreLayerCollision` — fully scriptable via MCP, no manual fallback needed. On wall hit: velocity zero, sprite hidden (via alpha, not `enabled=false` — see bug below), collider disabled, `FlashAndDestroy()` → `Destroy`.
- [x] **Patch 2 — WASD-direction aiming**: `facingDirection` tracked in `HybridPlayerController.cs`, only updates while moving, fire launches toward `facingDirection`. `FacingIndicator` child sprite (yellow square, no collider) repositions via `localPosition = facingDirection * indicatorOffset`. Mouse click remains the fire trigger; only aim direction changed.
- [x] **Patch 3 — Full-screen map + camera bounds**: `CameraFollow.cs` on Main Camera, `SmoothDamp` toward Player clamped to map bounds; includes an **auto zoom-in safeguard** (reduces `orthographicSize` if the camera's view would exceed the map, preventing the clamp range from ever inverting) instead of literally implementing the spec's "center if narrower than viewport" branch — the safeguard supersedes it (see Notes/decisions).
- [x] Verified: ping vanishes on wall hit with no bounce, passes through Player untouched, FacingIndicator tracks direction correctly including while stationary, firing while stationary uses last-faced direction, camera clamps correctly at both map extremes (confirmed via manual `LateUpdate`/`Physics2D.Simulate` driving — see Testing note under Day 3).

### Error log
#### [Day 1] Error: Legacy `Input` class threw `InvalidOperationException`
- **Where**: `HybridPlayerController.cs` (`Update`/`FixedUpdate`), `EventSystem`
- **What happened**: `InvalidOperationException: You are trying to read Input using the UnityEngine.Input class, but you have switched active Input handling to Input System package`
- **Root cause**: Project's Player Settings → Active Input Handling is "Input System Package" only, not "Both" — the original spec's pseudocode assumed the legacy Input Manager.
- **Fix applied**: Rewrote input reads to use `UnityEngine.InputSystem` (`Keyboard.current`, `Mouse.current`); swapped `EventSystem`'s `StandaloneInputModule` for `InputSystemUIInputModule`.
- **Prevented by**: Checking Project Settings → Active Input Handling before writing input code in any Unity 6 project.

#### [Day 2] Error: `Rigidbody2D.velocity` obsolete
- **Where**: All scripts using Rigidbody2D velocity
- **What happened**: Not a hard error, but `.velocity` is obsolete in Unity 6 and would emit deprecation warnings.
- **Root cause**: Unity 6 renamed `Rigidbody2D.velocity` → `Rigidbody2D.linearVelocity`.
- **Fix applied**: Used `linearVelocity` consistently from the start once identified.

#### [Day 2] Bug: Ping bounce reflected the wrong direction
- **Where**: `PingProjectile.cs` `OnCollisionEnter2D` (during the since-removed bounce feature)
- **What happened**: Bounced ping kept flying into the wall instead of away from it, then got zeroed by the physics solver next step.
- **Root cause**: Two stacked mistakes: (1) the physics solver already resolves/damps `rb.linearVelocity` into the wall *before* `OnCollisionEnter2D` fires, so reflecting the post-resolution velocity reflects near-zero; (2) after switching to `Collision2D.relativeVelocity` to fix that, it turned out `relativeVelocity` is `(other − this)`, the opposite sign of the ping's own travel direction.
- **Fix applied**: `Vector2.Reflect(-col.relativeVelocity, normal)`. (Now moot — bounce was deleted entirely in the 2.5 Patch.)
- **Prevented by**: When reflecting off a physics collision inside `OnCollisionEnter2D`, always use the pre-resolution velocity (`relativeVelocity`, sign-corrected), never `rb.linearVelocity` read inside that callback.

#### [Day 2] Bug: leftover "ghost" collider blocked/deflected later shots
- **Where**: `PingProjectile.cs`
- **What happened**: Reported as "가끔 벽을 인식하지 못하고 튕기거나 멈추는 일이 있다" (occasionally doesn't recognize the wall, bounces or stops) — intermittent, hard to reproduce on demand.
- **Root cause**: On wall hit, only the `SpriteRenderer` was disabled; the `Collider2D` stayed active for the ~0.8s flash-and-destroy lifetime. Since the fire cooldown (0.3s) is shorter than that, an invisible-but-solid "ghost" ping was almost always floating in the room, and new pings could physically collide with it instead of an actual wall.
- **Fix applied**: `Collider2D.enabled = false` immediately on wall hit, alongside the sprite hide.
- **Prevented by**: Anything that becomes "invisible" as a state should also stop being physically solid at the same moment, not just visually hidden.

#### [Day 2.5 Patch] Bug: ping flash light invisible ("빛이 발산되는 로직이 작동안됨")
- **Where**: `PingProjectile.cs` `OnCollisionEnter2D`
- **What happened**: Ping vanished on wall hit with no visible light flash at all, confirmed even by manually cranking `Light2D.intensity` to 10 in the Inspector during a Pause.
- **Root cause**: `GetComponent<SpriteRenderer>().enabled = false` — a GameObject with no active `Renderer` appears to be dropped from URP 2D's per-object render list entirely, which also silently drops that same object's own `Light2D` contribution. Not documented Unity behavior; found by elimination (coroutine execution, Light2D properties, shadow casters, blend styles, sorting layers, and renderer/pipeline assignment were all individually verified correct first).
- **Fix applied**: Hide via `sr.color.a = 0f` instead of `enabled = false`. Keeps the Renderer "active" (object stays in the render list, Light2D renders normally) while still making the sprite invisible.
- **Prevented by**: When something needs to become invisible AND still needs other rendering (lights, particles, etc.) on the same GameObject to keep working, prefer alpha/opacity over `Renderer.enabled = false`.

## Day 3
**Status: complete and verified.**

- [x] `ShadowEnemy.cs`: patrols between `patrolPoints` via Rigidbody2D, `SpriteRenderer` disabled by default (invisible), `Reveal(duration)` enables it and re-hides after `duration` via coroutine, `OnCollisionEnter2D` with tag `Player` calls `HybridPlayerController.Die()`.
- [x] `PingProjectile.cs`: after the flash ramp completes, `Physics2D.OverlapCircleAll(transform.position, flashRadius)` finds all colliders in range and calls `Reveal(0.5f)` on any `ShadowEnemy` found.
- [x] `HybridPlayerController.cs`: `spawnPosition` captured in `Start()`; `Die()` → `DieRoutine()` disables controls, plays a white `deathFlashOverlay` alpha flash (0→1→0 over 0.3s total), resets `transform.position` to `spawnPosition`, re-enables controls.
- [x] Ping cooldown renamed `fireCooldown` → `pingCooldown` (spec's field name), default raised `0.3s → 1.5s`. `cooldownIndicator` (`Image`, `Type.Filled`/`Radial360`) fillAmount tracks `1 - (remaining / pingCooldown)` — starts at 0 right when you fire, fills back to 1 when ready again.
- [x] Scene: `Enemy1` (patrol `(5,-5)↔(5,5)`), `Enemy2` (patrol `(15,-5)↔(15,5)`) — both dark purple circles, Rigidbody2D+CircleCollider2D+`ShadowEnemy`, waypoints as empty GameObjects. `DeathFlashOverlay` (full-screen white `Image`, alpha 0, `raycastTarget=false`) and `CooldownIndicator` (small radial `Image`, bottom-center) added to the existing `Canvas`, both wired to `Player`'s `HybridPlayerController`.
- [x] Verified via MCP (see Testing note for the methodology quirks this surfaced):
  - Enemies start invisible (`SpriteRenderer.enabled=false`) and patrol correctly (position advances toward the active waypoint under driven physics steps).
  - `Reveal()` immediately makes an enemy visible; `Physics2D.OverlapCircleAll` from a simulated ping-flash position correctly finds and reveals a nearby `ShadowEnemy`.
  - `Die()`'s respawn logic (position reset + controls re-enabled) verified directly.
  - **Full physical-collision integration** confirmed: pushed the Player into `Enemy2` under driven physics, confirmed via `DeathFlashOverlay` alpha `>0` (mid-flash) and controls being disabled (forced velocity got zeroed by the Player's own `FixedUpdate`) that the real `OnCollisionEnter2D → Die()` chain fires correctly from an actual physical hit, not just a direct method call.
  - Cooldown indicator: confirmed `fillAmount` drops to 0 on the frame a shot is fired (via simulated mouse-click `InputSystem` events) and is computed correctly on subsequent frames.

### Error log
(No compile errors, runtime exceptions, or MCP tool failures this Day — see Testing note below for a testing-methodology snag that was *not* a code bug.)

### Testing note
While verifying the physical Player↔Enemy collision, an initial test appeared to show the Player passing through `Enemy2` with no reaction (no respawn observed). This was **not a bug** — two testing-methodology issues, both traced and resolved without touching game code:
1. Calling `player.SendMessage("FixedUpdate")` inside the manual physics-step loop caused the Player's own controller to overwrite the velocity I'd set (reading current keyboard state, which was "no input" → zero) every step, before `Physics2D.Simulate()` ever ran. Fix: don't call the Player's `FixedUpdate` when directly driving its Rigidbody2D for a test.
2. Once that was fixed, the collision *did* register (confirmed by velocity transfer onto the enemy's Rigidbody2D from the impact) and `Die()` *did* fire — but `DieRoutine()`'s position-reset line sits after `yield return StartCoroutine(FlashOverlay())`, and this session's `Physics2D.Simulate()`-driven testing doesn't advance the Update-loop-driven coroutine scheduler, so the coroutine was legitimately paused mid-flash, not broken. Confirmed by checking `DeathFlashOverlay.color.a > 0` (mid-animation) and that forced movement was still being zeroed by `controlsEnabled=false`.
This is the same class of limitation noted for the ping-flash coroutine back in the 2.5 Patch section — logged here again because it nearly got misdiagnosed as a Day 3 collision bug.

## Day 4
**Status: complete and verified.**

- [x] `AudioManager.cs`: singleton (`static Instance`, `DontDestroyOnLoad`), `AudioSource`-based. Fields: `pingLaunch`, `wallHit`, `enemyReveal`, `playerDeath`, `stageClear` (no bounce clip — bounce doesn't exist in this build). `PlayPingLaunch()`/`PlayEnemyReveal()`/`PlayDeath()`/`PlayStageClear()` all use `AudioSource.PlayOneShot`; `PlayWallHit(float distance)` sets `audioSource.clip` then calls `PlayDelayed(distance / speedOfSound)` (`speedOfSound = 20f` constant). Every method null-checks its clip first.
- [x] `AudioManager` GameObject created in-scene (`AudioSource` + script, `playOnAwake=false`). All 5 clip fields are currently empty — see manual fallback.
- [x] `CameraShake.cs`: attached to Main Camera, `static Instance`. `Shake(duration, magnitude)` starts a coroutine offsetting a public `CurrentOffset` property (`Random.insideUnitCircle * magnitude` each frame), resetting to zero when done.
- [x] **Design decision**: `CameraFollow.cs` already writes `transform.position` every `LateUpdate` (smoothed + bounds-clamped), so `CameraShake` does **not** also write the transform directly — that would fight `CameraFollow` depending on script execution order. Instead `CameraShake` only exposes `CurrentOffset`, and `CameraFollow.LateUpdate()` adds it on top of its own computed position as the last step. Also had to introduce a separate `basePosition` field in `CameraFollow` (instead of reading `transform.position` back into `SmoothDamp` each frame) — otherwise the shake jitter would itself get fed into next frame's smoothing input and compound.
- [x] Wired into all 4 existing scripts, all calls null-guarded (`if (AudioManager.Instance != null)` / `if (CameraShake.Instance != null)`):
  - `PingProjectile.cs`: `Start()` → `PlayPingLaunch()`. Wall hit → `PlayWallHit(distanceToPlayer)` + `Shake(0.05f, 0.05f)`.
  - `ShadowEnemy.cs`: `Reveal()` → `PlayEnemyReveal()` + `Shake(0.1f, 0.1f)`.
  - `HybridPlayerController.cs`: `Die()` → `PlayDeath()` + `Shake(0.3f, 0.2f)`.
  - `StageExit.cs`: on clear → `PlayStageClear()`.
- [x] Verified via MCP: console clean after all changes and through a full wall-hit test with all `AudioClip` fields empty (confirms the null-guards work — nothing throws). `CameraShake.CurrentOffset` confirmed non-zero immediately after a real wall-hit collision (proving `Shake()` actually gets called through the integration, not just callable in isolation), and `CameraFollow`'s resulting `transform.position` confirmed to include that offset on top of its normal smoothed/clamped position.

### Error log
(No compile errors, runtime exceptions, or MCP tool failures this Day.)

### Manual fallback — audio assets
No internet/file access from this MCP session to source actual audio, so all 5 `AudioClip` fields on the `AudioManager` GameObject are empty. To finish this:
1. Source or generate 5 short clips (freesound.org, or a quick synth like jsfxr/bfxr for the more "retro" ones):
   - **pingLaunch**: short sonar-style ping/blip
   - **wallHit**: metallic impact, a touch of reverb
   - **enemyReveal**: short dissonant sting
   - **playerDeath**: low rumble + impact
   - **stageClear**: ascending chime/fanfare
2. Import them into `Assets/Audio/` (or similar) in Unity.
3. Select the `AudioManager` GameObject in the Hierarchy, and in the Inspector drag each clip onto its matching field (`Ping Launch`, `Wall Hit`, `Enemy Reveal`, `Player Death`, `Stage Clear`) on the `AudioManager` component.
No code changes needed — everything is already wired and null-safe in the meantime.

## Day 5
**Status: complete and verified (with an honest caveat on full cross-scene flow testing — see Testing note).**

- [x] `GameManager.cs`: singleton (`static Instance`, `DontDestroyOnLoad`). `currentStageIndex`, `totalElapsedTime` (accumulated in `Update()` while `timerRunning`). `StartGame()` sets `currentStageIndex=1`, resets timer, starts it, loads `Stage1`. `LoadNextStage()` increments the index; if it exceeds the stage count (derived from `SceneManager.sceneCountInBuildSettings - 2`, excluding Title+End) it stops the timer and loads `EndScreen`, otherwise loads `Stage{n}`. `GetElapsedTime()` returns `"mm:ss"`. `ReturnToTitle()` loads `TitleScreen`.
- [x] `SceneTransition.cs`: singleton, `DontDestroyOnLoad`, lives on its own `Canvas` (`sortingOrder=999`) with a full-screen black `Image` child (found via `[RequireComponent(typeof(Canvas))]` + serialized `fadeImage` reference). `FadeOut(Action)`/`FadeIn(Action)` both ~0.5s alpha lerps, same coroutine pattern as `DeathFlashOverlay`/ping flash.
- [x] `GameManager` wires the two together: every scene load (`StartGame`, `LoadNextStage`, `ReturnToTitle`) goes through a `TransitionTo(Action loadAction)` helper — `FadeOut` → run the load → `FadeIn`, or just run the load directly if `SceneTransition.Instance` is null (defensive fallback, shouldn't happen in the real flow since both singletons boot from the same `TitleScreen` scene).
- [x] `StageExit.cs`: `OnTriggerEnter2D` now just kicks off `ClearRoutine` (with a `triggered` guard against double-firing) — moved the existing disable-controls/show-clearUI/play-stageClear-sfx logic into the coroutine, then `yield return new WaitForSeconds(nextStageDelay)` (2s), then `GameManager.Instance?.LoadNextStage()`.
- [x] `PulsingLight.cs`: sits on a `Light2D`, oscillates `intensity` between 0.2–0.5 via `Mathf.Sin(Time.time * pulseSpeed)`.
- [x] `TitleScreenController.cs` / `EndScreenController.cs`: both use `Keyboard.current.anyKey.wasPressedThisFrame` (Input System — **not** legacy `Input.anyKeyDown`, consistent with the rest of the project) to call `GameManager.Instance.StartGame()` / `.ReturnToTitle()`, each guarded with a one-shot `bool` so it can't double-fire. `EndScreenController` also pulls `GameManager.Instance.GetElapsedTime()` into a `TextMeshProUGUI` in `Start()`.
- [x] Scenes created via MCP:
  - `Assets/Scenes/TitleScreen.unity` — black-background camera, `Global Light 2D` (intensity 0, consistent with the stage scenes), a `Light2D` Point light with `PulsingLight` for atmosphere, `Canvas` with "ECHOES" title + "Press any key" prompt (TextMeshPro, matching the project's existing TMP usage rather than legacy `UI.Text`), `TitleScreenController`, `EventSystem` (`InputSystemUIInputModule`), and the **`GameManager` + `SceneTransition` bootstrap objects** (these are the only place either singleton is instantiated — they `DontDestroyOnLoad` from here through the rest of the flow).
  - `Assets/Scenes/EndScreen.unity` — same camera/light setup, `Canvas` with "COMPLETE" + elapsed-time text (wired to `EndScreenController.elapsedTimeText` via `SerializedObject`) + return-to-title prompt, `EndScreenController`, `EventSystem`.
  - `Assets/Scenes/Stage1.unity` — the former `SampleScene.unity`, renamed via `AssetDatabase.RenameAsset`. All existing Day 1–4 content (Player, Goal/`StageExit`, tilemap, enemies, `AudioManager`, `CameraShake`, UI) carried over untouched.
  - `Assets/Scenes/Stage2.unity`, `Stage3.unity` — see Notes/decisions on why these are literal duplicates of `Stage1`, not new layouts.
- [x] Build settings (`EditorBuildSettings.scenes`) set in order: `TitleScreen, Stage1, Stage2, Stage3, EndScreen`, all enabled.
- [x] Verified via MCP:
  - All 5 new/modified scripts compile cleanly (confirmed via `Type.GetType(...)` lookups after a forced recompile).
  - Per-scene reference checks (via `EditorSceneManager.OpenScene` + `SerializedObject` inspection, since a live cross-scene Play-mode flow isn't reliable here — see Testing note): `TitleScreen` has `GameManager`, `SceneTransition` (with `fadeImage` assigned), `TitleScreenController`, `PulsingLight` all present; `EndScreen` has `EndScreenController` with `elapsedTimeText` assigned; `Stage1` has `StageExit` (with `clearUIPanel` assigned, `nextStageDelay=2`), `AudioManager`, `CameraShake` all present; `Stage2`/`Stage3` both carry their own `StageExit` (inherited from the duplicate).
  - `GameManager.GetElapsedTime()` formatting spot-checked directly (`0s→"00:00"`, `65s→"01:05"`, `605s→"10:05"`) — all correct.
  - Console clean of compile errors/exceptions after every change (only pre-existing, unrelated `MCP Unity` package noise about a duplicate WebSocket port — not from this project's game code).

### Error log
(No compile errors, runtime exceptions from game code, or MCP tool failures this Day. One MCP tool usage error, self-corrected: `Unity.UI.Image` used bare in a `Unity_RunCommand` script resolved to the namespace instead of the type — same class of issue logged back in Day 3, fixed the same way by fully-qualifying `UnityEngine.UI.Image`.)

### Manual fallback — audio assets (carried over from Day 4)
Attempted this session to close out Day 4's one open item (the 5 empty `AudioClip` fields) using this session's `Unity_AssetGeneration_GenerateAsset` (`GenerateSound`) tool, which wasn't available in the Day 4 session. `Unity_AssetGeneration_GetModels` returned an empty model list (`includeAllModels: true` included) — no asset-generation provider is configured in this environment, so `GenerateSound` has nothing to call. Still needs manual sourcing (freesound.org / jsfxr / bfxr) and dragging onto `AudioManager`'s 5 fields — see Day 4's fallback notes for the exact clip list. No code changes needed either way.

### Testing note
Same class of headless-MCP limitation noted on Day 3 (frozen `Time`/frame loop) applies here at a larger scale: a genuine end-to-end Play-mode flow (Title → press key → fade → Stage1 → clear → wait 2s → fade → Stage2 → … → EndScreen → press key → back to Title) spans multiple `SceneManager.LoadScene` calls, and each load tears down whatever driven state a manual `SendMessage`/`Physics2D.Simulate` test harness was using. This was **not attempted** as a live sequential flow — instead, each scene was verified independently (opened via `EditorSceneManager.OpenScene`, checked for the right components and serialized-field wiring). This is narrower than a true integration test: it confirms every piece is present and correctly connected, but does not prove the full sequence executes without a hitch at runtime (e.g., timing of the fade coroutines relative to `SceneManager.LoadScene`, or `DontDestroyOnLoad` object duplication across loads — though that duplication case *is* guarded by the existing `Instance != null && Instance != this → Destroy` pattern already used by `AudioManager` since Day 4, and `GameManager`/`SceneTransition` use the identical pattern). Recommend a manual in-Editor playtest of the full flow before considering this prototype done.

## Post-Day-5 bug fixes (user-reported: wall tiles didn't visually react to light)
User reported that sprites (Player, ping flash) visibly react to `Light2D` but the wall `Tilemap` never did, even up close. Two independent bugs were found and fixed, both pre-existing (not introduced by Days 4-5):

1. **`WallTile.asset` tint was pure black.** `m_Color: {r:0,g:0,b:0,a:1}` — under the Lit shader, final color = texture × tile tint × light, so a black tint multiplies everything to black regardless of light intensity. The underlying `WallTile.png` texture itself was confirmed genuinely white (`GetPixel` → `(1,1,1,1)`), so this was purely a stray tint value (contradicts the Day 2 checklist note that called it a "white 32x32" tile — it must have picked up a black tint at some point during Tile Palette painting). **Fix**: set `Tile.color = Color.white` via script + `RefreshAllTiles()`. Since `WallTile.asset` is one shared asset referenced by `Stage1`/`Stage2`/`Stage3`, this fixed all three at once.
2. **The real blocker**: `PingProjectile.prefab`'s `Light2D` (the flash-on-wall-hit light) had `m_ApplyToSortingLayers = [0]` — targeting only the `Default` sorting layer. The `Wall` sorting layer (uniqueID `337494797`, created in Day 2, *after* this prefab's light was originally configured in Day 1) was never added to its target list, while the `Global Light 2D` and the `Player`'s ambient `Light2D` both already correctly targeted all 4 sorting layers. So the ping's flash — the light source someone would most naturally associate with "닿았을 때" (on contact) near a wall — was structurally incapable of lighting the `Wall` layer at all, independent of tint/color. **Fix**: set the ping's `Light2D.m_ApplyToSortingLayers` to `[0, 337494797, -1725509951, -2090638041]` (all 4 project sorting layers) via `PrefabUtility.EditPrefabContentsScope`, matching what the other two lights already used.

Both fixes verified via `SerializedObject` re-read after saving (color confirmed white, sorting layers array confirmed to include Wall) and a clean console. **Not yet visually confirmed in a live Play session** (no in-Editor playtest performed this pass) — recommend the user do a quick visual check.

## Notes / decisions (Day 5 additions)
- **[Day 5]** Stage2/Stage3 ambiguity (spec assumed 3 stages already existed; this prototype only ever built one): resolved by duplicating `Stage1.unity` twice via `AssetDatabase.CopyAsset`, rather than building a 1-stage-only flow. Chosen because it exercises the *actual* multi-stage transition logic (`LoadNextStage` incrementing through real scene loads) end-to-end, which a 1-stage flow wouldn't touch at all — at the cost of Stage2/Stage3 being identical placeholder layouts, not new content. Follow-up: replace their tilemap layouts with distinct layouts before this leaves "prototype" status.
- **[Day 5]** Wired `SceneTransition`'s fade into `GameManager` even though the re-derived task list didn't explicitly call this out (it only specified creating the script) — an unused fade-transition script would otherwise be dead code, and the whole point of Day 5 being a "polish" day is that scene changes shouldn't be an instant hard cut.
- **[Day 5]** `GameManager` and `SceneTransition` are only instantiated once, as bootstrap objects living in `TitleScreen.unity` — not spawned fresh by each scene. Both `DontDestroyOnLoad` from there, so `TitleScreen` is the required entry point for the persistent singletons to exist at all (matches the build-settings scene order, where `TitleScreen` is index 0).

## Environment notes (superseded checkpoint — kept for reference, Days 4–5 are now done)

*(This section was originally written as an "after Day 3" handoff checkpoint listing Day 4/5 as a remaining task list. Both are now complete — see their own sections above. Keeping the still-useful environment facts below; the task list itself has been removed since it's fully executed.)*

- Physics2D layers: `Player`=8, `Ping`=9, `Wall`=10. Collision matrix: `Ping` collides only with `Wall` (ignored against everything else, including itself and `Player`).
- Established environment quirk (don't rediscover this, just work around it): this project's headless MCP Play-mode sessions don't reliably auto-advance `Time`/the frame loop. To test anything: drive lifecycle methods manually via `GameObject.SendMessage("Start"/"Update"/"FixedUpdate"/"LateUpdate")`, step physics via `Physics2D.Simulate()` with `Physics2D.simulationMode = SimulationMode2D.Script`, and simulate input via `InputSystem.QueueStateEvent`/`StateEvent.From(...)` + `InputSystem.Update()`. Coroutines that `yield return null` or `WaitForSeconds` will NOT complete under this driving — only test their synchronous first step, or bypass them (e.g. temporarily null a serialized reference so an `if (x != null)` branch with the only `yield` in it gets skipped) to verify what's after them. Additionally, cross-scene flows (`SceneManager.LoadScene`) tear down whatever driven state a test harness was using — verify each scene independently via `EditorSceneManager.OpenScene` instead of trying to drive a live multi-scene sequence.
- This project's Active Input Handling is **Input System only** — never use `UnityEngine.Input` (legacy), always `Keyboard.current`/`Mouse.current`.
- `Rigidbody2D.velocity` is obsolete in this Unity version (6000.3.21f1) — always use `.linearVelocity`.
- Fully-qualify `UnityEngine.UI.Image` in any MCP `Unity_RunCommand` script that also creates GameObjects with `typeof(...)` component lists — a bare `Image` resolves to a namespace, not the type, and fails to compile (hit this three times now across Day 3 and Day 5).
- This project uses TextMeshPro (`TMPro.TextMeshProUGUI`) for UI text, not legacy `UnityEngine.UI.Text` — match this in any new UI.

### Open error-log items
None outstanding — everything hit through Day 5 was diagnosed and resolved (see each Day's Error log). Day 4's empty `AudioClip` fields remain a manual (non-error) fallback item — see Day 5's "Manual fallback" note for why AI generation couldn't close it this session either.

---

## Day 7 — Tension, Onboarding, Stage Select, Collectibles
- [x] 7.1 Corridor width redesign (per-zone tension pacing)
- [x] 7.2 Darkness onboarding (intro cutscene + UI tooltip)
- [x] 7.3 Stage select scene (sequential unlock)
- [x] 7.4 Collectibles system
### Error log

#### [7.2] Testing-harness quirk: `HybridPlayerController.rb` NullReferenceException when driving `DarknessIntro` via `SendMessage` right after `EditorSceneManager.OpenScene`
- **Where**: first verification attempt for `DarknessIntro`/`PingTooltip`, calling `introGO.SendMessage("Start")`.
- **What happened**: `NullReferenceException` inside `HybridPlayerController.SetControlsEnabled` at `rb.linearVelocity = Vector2.zero` — `rb` was null.
- **Root cause**: `HybridPlayerController.rb` is assigned in `Awake()`. Opening a scene via `EditorSceneManager.OpenScene` in edit mode does **not** run `Awake()` on existing scene objects the way entering Play mode does — this project's existing scripts had never hit this specifically because prior edit-mode tests either drove freshly-`Instantiate`d objects (which *do* get `Awake()` called) or didn't call a method that touched an `Awake()`-only field this early. Not a bug in `DarknessIntro`'s design — `player.SetControlsEnabled(false)` is exactly what should happen when the intro starts, and works fine in a real Play session where `Awake()` already ran.
- **Fix applied**: none needed in game code — verification scripts now call `player.SendMessage("Awake")` once before exercising any script that touches `HybridPlayerController` after a fresh `OpenScene`.
- **Prevented by**: after `EditorSceneManager.OpenScene` in this project, `SendMessage("Awake")` any scene object whose `Awake()`-initialized fields (cached component refs, etc.) will be touched by a subsequent edit-mode `SendMessage("Start"/...)` call — don't assume `Awake()` already ran just because the scene loaded.

#### [7.2] Non-issue confirmed by testing: intro text alpha reads 0 immediately after the fade-in step
- **Where**: same verification pass, checking `DarknessIntroText`'s alpha right after `DarknessIntro`'s `Start()`.
- **What happened**: alpha read `0` instead of a partially-faded-in value.
- **Root cause**: `Time.deltaTime` is frozen (`0`) outside Play mode in this environment — the same limitation logged repeatedly since Day 3/6.6. `FadeText`'s loop (`t += Time.deltaTime`) never advances past `t=0`, so `Lerp(0,1,0/duration)=0`. Not a bug; the coroutine is legitimately paused mid-fade, exactly like every other coroutine in this project under edit-mode testing.
- **Fix applied**: none needed.

#### [7.2] Non-issue confirmed by testing: `SendMessage HandlePingFired has no receiver!` on a second edit-mode test pass
- **Where**: re-running the `PingTooltip` smoke test a second time in the same Editor session (no recompile in between).
- **What happened**: `PingTooltip.dismissedThisSession` (a `static bool`, intentionally session-persistent) was already `true` from the first test pass's `HandlePingFired` call. The second pass's `Start()` correctly saw it was already dismissed and called `gameObject.SetActive(false)` immediately — so the follow-up `SendMessage("HandlePingFired")` had no active GameObject to dispatch to.
- **Root cause**: not a bug — this is `PingTooltip`'s intended behavior (per its Day 7 design: a session-only static flag, not `PlayerPrefs`, so the tooltip never reappears once dismissed within the same running session, including if the player re-enters Stage1 via Stage Select later). The test's own repeated calls within one Editor domain triggered exactly the "already dismissed" path a real second Stage1 visit would also trigger.
- **Fix applied**: none needed - this incidentally *confirmed* the session-persistence design works as intended, rather than exposing a defect.

#### [7.3] Non-issue confirmed by testing: `DontDestroyOnLoad ... cannot be part of an editor script` when priming a temporary `ProgressManager`'s `Awake()`
- **Where**: verification script for the unlock-progression logic, calling `SendMessage("Awake")` on a scratch `ProgressManager` test object (not the real bootstrap instance).
- **What happened**: `InvalidOperationException` thrown from `Object.DontDestroyOnLoad` inside `ProgressManager.Awake()`.
- **Root cause**: same class of limitation as 7.2's `Awake()`-outside-Play-mode note — `DontDestroyOnLoad` is explicitly Play-mode-only and throws when called from edit-mode scripting. `Instance = this` (the line before it) still ran, so the rest of the test (`IsStageUnlocked`/`MarkStageCleared`/`PlayerPrefs` persistence) executed and verified correctly regardless.
- **Fix applied**: none needed — doesn't affect the real bootstrap object, which only ever calls `Awake()` naturally via actual Play mode / a real build.
- **Prevented by**: don't be alarmed by a `DontDestroyOnLoad` edit-mode exception when `SendMessage`-priming a singleton's `Awake()` for testing in this project — it's expected, and everything before that line in `Awake()` still ran.

### 7.3 detail — Stage select scene
- **`Assets/Scripts/ProgressManager.cs`** (new): `DontDestroyOnLoad` singleton (same pattern as `GameManager`/`SceneTransition`/`AudioManager`/`CameraShake`), tracks `highestClearedStage` (backed by `PlayerPrefs["HighestClearedStage"]`, loaded in `Awake()`). `IsStageUnlocked(n)` returns `n <= highestClearedStage + 1` (stage 1 always unlocked since `highestClearedStage` starts at `0`). `MarkStageCleared(n)` only raises the stored value, never lowers it (clearing stage 3 directly, or re-clearing stage 2 later via replay, can't regress progress) — verified directly.
- **Decision — singleton over a bare `PlayerPrefs`-only static class**: the spec offered either; chose the singleton to match this project's existing convention for every other piece of persistent/cross-scene state, rather than introducing a second pattern for the same kind of thing. Bootstrapped once in `TitleScreen.unity` alongside `GameManager`/`SceneTransition`, same as those.
- **Decision — clear-flow now returns to Stage Select, not auto-chain, except on the true last stage**: per the spec's own recommendation. `StageExit.ClearRoutine` now derives its own stage number from the active scene's name (`"StageN"` → `N`, via `SceneManager.GetActiveScene().name`) rather than trusting `GameManager.currentStageIndex` — necessary because Stage Select buttons load stage scenes directly through `GameManager.LoadStage(n)`, which does keep `currentStageIndex` in sync, but deriving from the scene name is simpler and self-contained rather than relying on that side channel staying correct. After marking the stage cleared, it loads `EndScreen` only if this was the final stage in the build (`stageIndex >= sceneCountInBuildSettings - 3`, i.e. excluding Title/StageSelect/End), otherwise returns to Stage Select. This preserves `EndScreen` as a real destination (the literal spec text didn't address it) rather than orphaning it — a call made and logged per the "resolve ambiguity yourself" rule.
- **`GameManager.cs` reshaped around Stage Select**: `StartGame()`/`LoadNextStage()` (both now unreachable — nothing calls a "start straight into Stage1" or "auto-advance" path anymore) were replaced rather than left as dead code, with `GoToStageSelect()` (Title → Stage Select, doesn't touch the elapsed-time timer — sitting in a menu shouldn't count as play time) and `LoadStage(int)` (Stage Select → a specific stage; starts the timer only if it isn't already running, so re-entering Stage Select mid-run and picking another stage doesn't reset the clock) and `LoadEndScreen()` (extracted from the old `LoadNextStage`'s tail). All three still route through the existing private `TransitionTo` fade helper, so the `SceneTransition` fade-out/fade-in polish from Day 5 is preserved on every new path, not just the old linear one.
- **`TitleScreenController.cs`**: now calls `GameManager.Instance.GoToStageSelect()` instead of `StartGame()` — one-line change, per the spec.
- **`Assets/Scripts/StageSelectController.cs`** (new): holds a serialized array of 5 `{stageIndex, button, background, lockIcon, collectibleCountText}` entries (the `collectibleCountText` slot is built now but left unwired until 7.4). On `Start()`, sets each button's `interactable` from `ProgressManager.IsStageUnlocked`, tints the background (`unlockedColor` blue / `lockedColor` gray placeholder — no real art yet, consistent with this project's existing placeholder-art pattern), toggles a `"LOCKED"` text overlay, and wires `onClick` to `GameManager.Instance.LoadStage(stageIndex)`.
- **`Assets/Scenes/StageSelect.unity`** (new): camera (black background, orthographic, matching `TitleScreen`/`EndScreen`), `EventSystem` with `InputSystemUIInputModule` (this project's Input-System-only convention), `Canvas` (`ScaleWithScreenSize`, `1920x1080` reference — matches `TitleScreen`'s), a "STAGE SELECT" title, and 5 stage buttons in a row, each with a label, an empty (7.4-reserved) collectible-count text, and a "LOCKED" overlay. No dedicated `SceneTransition` canvas needed here — the one persistent instance bootstrapped in `TitleScreen` already covers every scene.
- **Build Settings**: `StageSelect.unity` inserted between `TitleScreen` and `Stage1` (`EditorBuildSettings.scenes`, now 8 entries total: Title, StageSelect, Stage1-5, End) — done via MCP, no manual fallback needed.
- **Verified**: all new/changed scripts compile clean and resolve. Unlock-progression logic directly verified (fresh state: only Stage1 unlocked; clearing Stage1 unlocks Stage2 but not Stage3; clearing Stage3 out of order still unlocks Stage4; re-clearing a lower stage doesn't regress `highestClearedStage`; `PlayerPrefs` persistence confirmed read back correctly) using a scratch `ProgressManager` instance so the real `PlayerPrefs` state wasn't touched by testing. `StageSelectController`'s serialized array confirmed fully wired (all 5 slots: `stageIndex`, `button`, `background`, `lockIcon`, `collectibleCountText` all non-null) via `SerializedObject` inspection. Console clean of game-code errors/warnings (only the pre-existing `MCP Unity` noise, plus the two documented, harmless edit-mode `Awake()`-timing artifacts above).
- **Not independently re-verified**: real button-click → scene-load → fade → correct-stage-loads flow, and the visual appearance of the locked/unlocked tint and "LOCKED" text — inherits the same "can't drive a live UI click + scene load headlessly" limitation already logged for every prior cross-scene flow in this project (Day 5's Testing note, 6.6, etc.). **Recommend a manual in-Editor Play-mode pass**: start from Title, confirm only Stage1 is selectable, clear it, confirm it returns to Stage Select with Stage2 now unlocked, and confirm Stage5's clear correctly reaches `EndScreen` instead.

### 7.4 detail — Collectibles system
- **`Assets/Scripts/Collectible.cs`** (new): a trigger-collider pickup. On `OnTriggerEnter2D` with the `Player` tag, calls `ProgressManager.MarkCollectibleFound(collectibleId)`, plays a small pickup sound (`AudioManager.PlayCollectiblePickup`, new) and a tiny camera shake (`CameraShake`, reusing the existing 6.4-era pattern), then deactivates itself. A `collected` guard makes a second trigger a no-op.
- **`ProgressManager.cs` extended**: a `HashSet<string>` of found collectible ids, persisted as a comma-joined `PlayerPrefs` string (`FoundCollectibles`) — same persistence approach as `HighestClearedStage`, chosen for consistency rather than introducing `PlayerPrefs.SetInt`-per-id sprawl. `MarkCollectibleFound(id)` / `IsCollectibleFound(id)` / `GetCollectibleFoundCountForStage(n)` (counts found ids matching the `"stage{n}_"` prefix — no registration needed, just naming discipline) / `GetCollectibleTotalForStage(n)` (a small hardcoded `{1:2, 2:2, 3:3, 4:2, 5:3}` table, since Stage Select needs every stage's total while none of Stage1-5 are loaded — this is a level-design constant, not runtime-derived state).
- **`AudioManager.cs`**: added `collectiblePickup` clip field + `PlayCollectiblePickup()`, same empty-by-default manual-fallback pattern as every other clip in this project.
- **`Assets/Prefabs/Collectible.prefab`** (new): placeholder art per the spec's own suggestion — small cyan-tinted diamond (`WallTile.png` reused at 45° rotation and `0.35` scale, same reuse-existing-texture approach as `DecoyObject`/`MossZone`), rendered with the existing `MossUnlit.mat` unlit material so it stays visible even at `Global Light 2D` intensity `0` (matches the project's established "unlit for anything that must stay visible in total darkness" pattern from 6.4). `CircleCollider2D` (`isTrigger=true`, `radius=0.6`).
- **Placement — 12 collectibles across the 5 stages** (2-3 each, per spec), all confirmed sitting on floor tiles (`Tilemap.GetTile` at each position's cell returns `null`) using the corridor geometry from 7.1:
  - **Stage1** (2): one in the dead-end stub near spawn (`3.5,-5`), one tucked in the upper detour lane's far corner (`14,4.5`).
  - **Stage2** (2): one just past the `x=12` `CrackedWall` gate (`13,0.5` — rewards breaking it), one in the dead-end stub (`5.5,-3`).
  - **Stage3** (3): one past the shortcut crack on the direct/monster lane (`17,0.5` — risky, behind the `CrackedWall` per the spec's ask), one in the upper detour lane's far corner (`25,6.5`), one in the spawn-side connector shaft corner (`0.5,6.5`).
  - **Stage4** (2): one in the dead-end stub (`9.5,-2` — doubles as the decoy-staging pocket from 7.1), one past the chokepoint on the far side, near Monster2 (`25,0.5`).
  - **Stage5** (3): one just past the `CrackedWall` `AbyssGate` (`16,0.5`), one in the west plaza off the `y=0` moss lane (`11,3` — reachable via the moss stepping-stones per the spec's ask), one in an east-plaza corner (`19,-4`).
- **Stage Select UI**: each button's `collectibleCountText` (built but left unwired in 7.3) is now set in `StageSelectController.Start()` from `ProgressManager.GetCollectibleFoundCountForStage`/`GetCollectibleTotalForStage`, formatted `"{found}/{total}"`.
- **Verified**: all scripts compile clean and resolve. All 12 placed collectibles confirmed sitting on floor cells (not embedded in walls) via direct `Tilemap.GetTile` checks. Full pickup flow directly verified end-to-end on a real placed instance (`Collectible_stage1_item1` in `Stage1.unity`, via `SendMessage("OnTriggerEnter2D", playerCollider)`): `IsCollectibleFound` flips `False → True`, the `GameObject` deactivates, `GetCollectibleFoundCountForStage(1)` reads `1`, `GetCollectibleTotalForStage(1)` reads `2`, and `PlayerPrefs["FoundCollectibles"]` persists `"stage1_item1"` — all confirmed via a scratch test that was fully cleaned up afterward (`PlayerPrefs` keys deleted, the real scene's on-disk state reconfirmed untouched — `activeSelf=True`, `scene.isDirty=False` — after the test session finished poking it). Console clean of game-code errors/warnings throughout the whole 7.4 pass (only the pre-existing `MCP Unity` noise, plus the same class of harmless `Awake()`/`SendMessage`-outside-Play-mode artifacts already logged in 7.2/7.3's error logs).
- **Not independently re-verified**: real Play-mode walking-into-a-collectible (vs. the direct `SendMessage` trigger call used above), the visual/audio feel of the pickup flash+shake+sound, and whether the 12 placement spots are actually *fun* to find (not too easy, not unfairly hidden) — inherits this project's standing "headless sessions can't drive a live Play-mode walkthrough" limitation. **Recommend a manual in-Editor playtest of all 5 stages' hiding spots** alongside the other outstanding manual-verification items from 7.1-7.3.

## Notes / decisions

### 7.1 detail — Corridor width redesign
**Target width chosen: 2 tiles (2 units)** for all "tight" corridors — Player/Enemy `CircleCollider2D` radius is `0.5` (diameter `1`) on both, so 2 units = exactly 2x diameter, the top of the spec's 1.5-2x range. Chose the round number over a literal 1.5x (`1.5` units isn't expressible on a 1-unit tile grid without half-tile art) and confirmed it's still safe: the baked `NavMeshSurface`'s agent-type radius is `0.5` (`NavMesh.GetSettingsByIndex(0).agentRadius`), which eats `0.5` off each side of a corridor during baking, leaving `1.0` unit of actual walkable width in a 2-wide corridor — comfortably more than the `NavMeshAgent`'s own `radius=0.4`. A 1-tile-wide corridor was rejected outright: it would leave zero clearance for a `radius=0.5` collider and risk physics jitter/sticking.

**All 5 stages were fully re-tiled**, not just narrowed — the prior layout (Days 2-5, and the 5-stage design pass) was one large open room (~20-30 units wide) with only one or two thin 1-tile partitions creating a zigzag; there was no actual "corridor" to narrow, since the open floor itself was the play space. Rebuilt each stage's `Tilemap` from a fully-solid interior, carving out floor only in deliberate rectangles (each corridor segment authored as an explicit `(x1,x2,y1,y2)` rect, checked by construction to be exactly 2 units in its short dimension) so every corridor is tight by construction, not by measuring after the fact. This also incidentally fulfills the old Day 2 checklist note that the maze was "not a branching maze with dead ends" — every stage now has at least one real dead-end stub off the main path (also useful as 7.4 collectible spots later).

Per-stage layout (all preserve their existing theme/mechanic placements from the 5-stage design table, repositioned to fit):
- **Stage1** ("corridor A", tutorial, no monsters): serpentine 2-wide corridor, spawn(0,0)→goal(27,0), with two up/down detour loops replacing the old x=10/x=20 zigzag partitions, plus one dead-end stub near spawn. Flood-fill verified solvable (108 cells reachable).
- **Stage2** ("corridor B", cracked-wall tutorial): single straight 2-wide corridor, full width. The two mandatory `CrackedWall` gates (`CrackedWall_x12`, `CrackedWall_x22`) were rescaled from `(1,3,1)` (blocking a 3-tall gap in the old full-height wall) to `(1,2,1)` positioned at `(x, 0.5)`, exactly plugging the new 2-tall corridor. Verified: unreachable with gates intact, solvable once broken (32 vs 68 reachable cells).
- **Stage3** ("hunter's maze A", 1 monster, dilemma): direct 2-wide main lane (monster patrol, blocked at x=15 by `CrackedWall_Shortcut`, rescaled/repositioned the same way as Stage2's gates) plus a full-width 2-wide upper detour lane (y=6-7) connected by shafts near spawn/goal — the "real, unblocked detour" the design called for. Monster repositioned to `(12, 0.5)`, `wanderRadius` kept at `5`. Verified: reachable via detour even with the gate treated as blocked (optional, 135 cells) — the dilemma is genuine, not a forced break.
- **Stage4** ("hunter's maze B", 2 monsters, decoy-forced): single straight 2-wide corridor, no detour at all (matches "decoy-forced" — the only way past Monster1 is misdirection, not an alternate route). Both `ShadowEnemy` instances resettled onto the corridor's y=0.5 centerline; kept their existing tight/loose `wanderRadius`/`hearingRadius` split (`2`/`10` for the gap-guard, `5`/`8` for the far-side patrol). Added one dead-end stub before the chokepoint as a decoy-staging pocket. Verified solvable (66 cells).
- **Stage5** ("abyss center", 3 monsters, moss lane): the one deliberate exception, per the spec's own example — kept two genuinely open plazas (west `8-14 x -6-6`, east `16-21 x -6-6`, both comfortably over the 4x-diameter open-space threshold) bisected by a mandatory 2-wide `CrackedWall_AbyssGate` chokepoint at x=15 (rescaled the same way as the other gates), with tight 2-wide approach/exit corridors on both outer ends. The 6 `MossZone` stepping-stones and 3 monsters were repositioned to sit inside the two plazas (previously scattered across one big open field that no longer exists in this shape) rather than in the now-narrow entrance/exit corridors. Verified: gate mandatory (unreachable while intact, 107 cells), solvable once broken (203 cells).

**NavMesh**: fully rebaked for all 5 stages against the new layouts — the old bake's 114-per-stage `NavMeshWalls` obstacle cubes were positioned for the pre-retile geometry and are now stale/wrong. Rebuilt each stage's wall-obstacle cubes from scratch by iterating every occupied `Tilemap` cell in the (now much sparser) layout, plus 2 extra obstacle cubes per `CrackedWall` gate (covering both rows it blocks) since gates live outside the Tilemap. Re-discovered and correctly handled the same "renderer must be present during bake, stripped after" rule from 6.6's bug log — but this time it applied to `NavMeshFloor` too, not just the wall cubes (the floor GameObject had already had its one-time bake `MeshRenderer` stripped after the *original* 6.6 bake, so a naive rebake without temporarily re-adding one would have silently produced an empty mesh again, exactly the same failure class as 6.6's original bug). Vertex counts post-rebake: Stage1=88, Stage2=26, Stage3=38, Stage4=18, Stage5=47 — all non-zero/non-degenerate. `NavMeshAgent.radius=0.4` confirmed comfortably under the `1.0`-unit clearance a 2-wide corridor leaves after the bake's `agentRadius=0.5` erosion (see above) — no corridor/patrol clipping expected from the width change itself, though actual runtime pathing still carries 6.6's pre-existing "needs a real Play-mode test" caveat, unrelated to this pass.

**Ping-flash-vs-corridor check** (the spec's explicit "not just eyeballing" requirement): `PingProjectile.flashRadius = 3` (unchanged). Against a 2-wide corridor, the flash's radius exceeds the corridor's *width* (so both side walls near the hit point are always lit — unavoidable at any width under 6, and not the problem the spec was targeting), but corridor *segments* are all well over 6 units long (Stage1's main-lane legs run 8-9 units, Stage2/4's straight corridors run the full 30-unit width), so a single flash reveals only a local ~3-unit pocket, never a whole segment end-to-end. This is the actual metric the design brief cared about ("shouldn't remove the need to explore further") and it holds by construction now that corridors are real corridors instead of one open room.

**Console**: clean of game-code errors/warnings through the whole retile+rebake pass (only the pre-existing, unrelated `MCP Unity` package npm/port-conflict noise, timestamped from before this session's work).

**Not independently re-verified**: actual `NavMeshAgent` runtime patrol behavior in the new, much-narrower corridors (does an enemy's wander/chase/return still look and feel right at 2-wide vs. the old wide-open room) — this inherits the exact same "can't drive real Play-mode timing headlessly" limitation logged back in 6.6, now doubly relevant since the corridors are tighter. **Recommend a manual in-Editor Play-mode pass over all 5 stages** (not just 6.6's original ask) before trusting monster patrol feel at the new widths.

### 7.2 detail — Darkness onboarding
- **`Assets/Scripts/DarknessIntro.cs`** (new): on `Start()`, disables player controls (`HybridPlayerController.SetControlsEnabled(false)`), bumps `Global Light 2D.intensity` to a serialized `elevatedIntensity` (default `0.18` — "enough to vaguely see", matching the spec's `0.15-0.2` ask), optionally fades in a short diegetic `TextMeshProUGUI` message, holds, then animates the light back down to `0` over `fadeDuration` while the text fades back out in the final `textFadeDuration` slice of that same fade, and re-enables controls once the light hits `0`.
- **Decision — cutscene scope (Stage1 full, Stages 2-5 lite), built as one script with two configurations rather than two scripts**: Stage1 gets the full beat — `holdDuration=1.75s`, `fadeDuration=1s`, plus the intro text (`"칠흑 같은 어둠. 당신은 혼자다."` — "Pitch-black darkness. You are alone.", 2 short clauses, well under the ~15-word cap). Stages 2-5 get the same component with `introText` left unassigned (the null-check skips all text logic) and much shorter `holdDuration=0.3s`/`fadeDuration=0.4s` — just a quick "lights dim back down" beat so each stage doesn't hard-cut into darkness, without re-explaining a premise the player already learned. Chose one reusable component over two separate scripts since the only real difference is serialized data (text reference + durations), not behavior — avoids duplicating the coroutine logic for a distinction that's purely about tuning.
- **`Assets/Scripts/PingTooltip.cs`** (new): a `CanvasGroup`-driven UI hint, shown only on Stage1, subscribed to a new `HybridPlayerController.OnPingFired` static event (added this session — fires from `TryFirePing()` right after a cooldown-gated `Fire()` succeeds) and fades itself out + deactivates on the first fire.
- **Decision — tooltip text covers both live input paths, not one**: per 6.5/Post-6.6 refinements, desktop click, mobile drag-release, and a dedicated `PingButton` are all simultaneously live (no platform branching in this project - see 6.5's notes). Tooltip reads `"핑 버튼을 누르거나 클릭해서 핑을 발사하세요"` ("Press the ping button or click to fire a ping") rather than assuming one scheme.
- **Decision — session-only static bool, not `PlayerPrefs`**: `PingTooltip.dismissedThisSession` only needs to survive `HybridPlayerController.Die()`'s respawn (which repositions the player but never reloads the scene) and a later return to Stage1 via 7.3's Stage Select within the same running session — it doesn't need to survive an app restart. A `static bool` is simpler than `PlayerPrefs` for that scope and was explicitly confirmed to behave as intended during testing (see Error log below — a second edit-mode test pass hit the "already dismissed" path exactly as a real second Stage1 visit would).
- **Scene wiring** (via MCP): Stage1 got `DarknessIntroText` (centered `TextMeshProUGUI`, alpha starts at `0`) + a `DarknessIntro` object wired to `Global Light 2D`, `Player`, and that text, plus a `PingTooltip` object (top-center `CanvasGroup` + label, positioned clear of the bottom-left joystick, bottom-right `PingButton`/`DecoyButton`, and the right-half `TouchAimZone`) wired to a `CanvasGroup` + `TextMeshProUGUI` label. Stages 2-5 each got just the lite `DarknessIntro` object (`introText` left null, short durations) — no tooltip, matching the "Stage1 only" spec.
- **Verified**: both scripts compile clean and resolve (`Type.GetType` lookup after recompile). Edit-mode smoke test (after correctly priming `Player`'s `Awake()` — see Error log) confirmed: `DarknessIntro.Start()` disables controls and raises `Global Light 2D.intensity` to the elevated value without exception; `PingTooltip.Start()` leaves the tooltip visible (`alpha=1`) pre-dismissal; `HandlePingFired` runs its dismiss path without exception. Console clean of game-code errors/warnings throughout (only the pre-existing `MCP Unity` package noise).
- **Not independently re-verified**: real-time fade timing/visual feel of either the light-to-dark transition or the text/tooltip fades — inherits the same "`Time.deltaTime` frozen outside Play mode" limitation as everything else in this project since Day 3. **Recommend a manual in-Editor Play-mode check of Stage1's opening beat and the tooltip's fade-out on first ping**, alongside the other outstanding manual Play-mode items already logged.

---

## Notes / decisions
- **[Day 3]** `cooldownIndicator.fillAmount` convention: chose "starts at 0 the instant you fire, fills up to 1 when ready again" (not the reverse). Not specified in the prompt; this is the more common convention for ability-cooldown UI.
- **[Day 3]** Renamed `fireCooldown` (0.3s, from Day 2.5) to `pingCooldown` (1.5s) to match Day 3's exact field name/value — a deliberate, large increase from the prior value, since Day 3's spec gave an explicit new number.
- **[Day 2.5 Patch]** Kept `sr.color.a = 0` instead of the spec's literal `SpriteRenderer.enabled = false` for hiding the ping — that exact line was the diagnosed cause of the flash-invisible bug; reverting to it would reintroduce the bug.
- **[Day 2.5 Patch]** Used `Keyboard.current` (Input System) instead of legacy `Input.GetAxisRaw(...)` in aiming code, consistent with the Day 1 fix (this project's Active Input Handling is Input System–only).
- **[Day 2.5 Patch]** Implemented `CameraFollow.cs`'s "map narrower than viewport" protection as an auto zoom-in safeguard (shrinks `orthographicSize` so the clamp range can never invert) rather than the spec's literal "center on that axis" branch — functionally supersedes it; the center-branch would be unreachable dead code under this implementation.
- **[Day 2]** Current Stage 1 layout is a large open room with two zigzag partitions, not a "branching maze with dead ends" as Day 2's original spec described — this was the level actually built and play-tested across this project's history; not being redone to match the literal "maze" description since it already serves the same purpose (an obstacle route from spawn to Goal) and reworking it isn't required by anything in Day 3-5.
- **[Testing]** This project's headless MCP Play-mode sessions do not reliably auto-advance `Time`/the frame loop (`Time.time` observed frozen despite `isPlaying=true`). Established workaround, used throughout: drive lifecycle methods manually via `GameObject.SendMessage("Start"/"FixedUpdate"/"LateUpdate")`, advance physics deterministically via `Physics2D.Simulate()` with `Physics2D.simulationMode = SimulationMode2D.Script`, and simulate real keyboard input via `InputSystem.QueueStateEvent`/`StateEvent.From` + `InputSystem.Update()` rather than relying on ambient frame ticking.

---

## Day 6+ — Feature Expansion

**Status: Priority 1 (6.1-6.6) complete and verified to the extent this headless environment allows. Checkpointing here, before Priority 2, at a clean tier boundary.**

### Verify Priority 1 (summary - see each feature's own "detail"/Error log entries above for specifics)
- Console clean of game-code errors across every change in 6.1-6.6 (only the pre-existing, unrelated `MCP Unity` package port-conflict noise, plus benign edit-mode `SendMessage`-on-physics-callback assertions - both already documented and not regressions).
- 6.1-6.5: directly verified via `Unity_RunCommand` (component checks, `SerializedObject` field wiring, simulated Input System events where level-triggered, public interface methods for UI pointer events, `SendMessage` for lifecycle/private methods) - see each feature's own detail section above.
- 6.6: NavMesh geometry itself is verified real and correct (56 vertices in all 3 stages, not an empty/degenerate bake) and the noise-event subscription chain is verified end-to-end; actual `NavMeshAgent` runtime pathing is **not** verified (environment limitation, not a known bug) - flagged explicitly rather than claimed as passing. **Recommend a manual in-Editor Play-mode test of 6.6 specifically before trusting it.**
- Manual fallbacks accumulated so far (Day 6+): 5 empty `AudioManager` clip fields from 6.1-6.3 (`normalWallClip`/`crackedWallClip`/`monsterClip`/`wallBreak`/`decoyLand`), decoy/moss placeholder art (reused `WallTile.png` with tint/scale changes), no real device/Unity Remote touch test for 6.5, no manual in-Editor Play-mode test for 6.6's agent pathing yet.

### Priority 1 — Core identity mechanics
- [x] 6.1 Material-based ping sound (wall / cracked wall / monster)
- [x] 6.2 CrackedWall (destructible wall)
- [x] 6.3 DecoyThrow (decoy mechanic)
- [x] 6.4 MossZone (sound/light dampening zone)
- [x] 6.5 Mobile touch controls (virtual joystick + drag-aim)
- [x] 6.6 NavMesh-based monster AI (wander / chase noise / return)

#### 6.6 detail
- **Package decision**: `com.unity.ai.navigation` (official Unity package, `NavMeshSurface`/`NavMeshModifier`/`NavMeshAgent`) added successfully via `UnityEditor.PackageManager.Client.Add` — confirms the Package Manager registry **is** reachable from this environment (unlike the unrelated `npm`-executable-missing failure that was blocking the `MCP Unity` package's own bundled server install; those are two different failure modes, don't conflate them). `NavMeshPlus`/baked-3D-plane fallback was not needed.
- **Coordinate-system decision (important, deviates from a naive reading of the spec)**: this package version (`2.0.14`) does **not** have the classic "Rotate XY" 2D-baking toggle some older/community NavMesh tooling had (confirmed by enumerating `NavMeshSurface`'s `SerializedObject` properties — no `m_UseRotateXY` or equivalent exists). So the NavMesh is baked in Unity's standard 3D convention (walkable plane = XZ, up-axis = +Y), and `ShadowEnemy.cs` does the game-space (X,Y) ↔ nav-space (X,0,Z) conversion itself every frame: `agent.updatePosition/updateRotation/updateUpAxis` are all `false`, `GameToNav`/`NavToGame` convert at every `SetDestination`/`Warp` call, and `Update()` pulls `agent.nextPosition` back into `transform.position` (converted) each frame. This is the standard, well-documented technique for adapting Unity's 3D NavMesh to a 2D top-down game without the rotate feature.
- `ShadowEnemy.cs` rewritten as a `Wander`/`ChaseNoise`/`ReturnToStart` state machine (private `enum State` + `switch` in `Update()`), replacing the Day 3 fixed-waypoint patrol entirely — **deliberate upgrade, not a bug**, per the spec's own framing.
  - **Wander**: every `wanderInterval` (3s default), picks a random point within `wanderRadius` (5f) of `spawnPosition` and calls `agent.SetDestination`.
  - **ChaseNoise**: entered via `HandleNoise(Vector2 position, float noiseRadius)`, subscribed to both `PingProjectile.OnPingHit` (new event, see below) and the pre-existing `DecoyObject.OnDecoySpawned` in `OnEnable`/unsubscribed in `OnDisable`. Effective hearing distance = `Mathf.Min(noiseRadius, hearingRadius)` — **decision**: the spec didn't fully specify how a per-sound radius and a per-enemy `hearingRadius` should combine, so the smaller of the two caps the trigger distance (a very loud-but-distant sound is still capped by what this specific enemy can hear; a quiet/short-range sound is respected even for a sharp-eared enemy). Actually catching the player is **not** duplicated here — it's still the pre-existing physical `OnCollisionEnter2D`→`Die()` path from Day 3, completely unchanged; `ChaseNoise` only tracks a `chaseTimeout` (4s default) before giving up and transitioning to `ReturnToStart`.
  - **ReturnToStart**: paths back to `spawnPosition`; once `agent.remainingDistance <= agent.stoppingDistance` (and not `pathPending`), transitions back to `Wander`.
  - `Reveal(float)` (ping-flash visibility) and the `OnCollisionEnter2D` (`CrackedWall`/`Player`) handlers are both **unchanged** from 6.2/Day 3 — confirmed orthogonal to the new AI state, exactly as the spec asked.
  - `hearingRadius` (`8f` default) added as a distinct `[SerializeField]` from `PingProjectile.flashRadius`.
- `PingProjectile.cs`: new `public static event Action<Vector2,float> OnPingHit`, invoked in `OnCollisionEnter2D` right after the (already-existing, 6.1) audio branch, passing `(transform.position, flashRadius)` — but **only when `!dampened`** (a moss-absorbed ping makes no sound to chase, consistent with 6.4's theme). Fires for any hit (`Wall`/`CrackedWall`/`Enemy`), not just wall hits.
- **Robustness fix applied during verification** (see Error log): every `agent.SetDestination`/`agent.remainingDistance`/`agent.nextPosition` access is now guarded with `agent.isOnNavMesh` — protects against a real (if rare) runtime failure mode where an agent ends up off-mesh, in addition to being required for this session's edit-mode testing not to throw.
- NavMesh geometry built for **all three stages** (`Stage1`/`Stage2`/`Stage3`, all sharing the same tile layout per Day 5): a `NavMeshGeometry` root containing a `NavMeshFloor` (walkable, spans the full `(-1,-8)`-`(31,9)` room) and 114 individual 1×1 `NavMeshWalls` obstacle cubes (one per occupied `Tilemap` cell, positioned via `Tilemap.GetCellCenterWorld` — exactly matches the real wall layout, not an approximation), each with a `NavMeshModifier` (`overrideArea`: floor=Walkable, walls=Not Walkable) and `GameObjectUtility.SetStaticEditorFlags(...NavigationStatic)`. All of this geometry is invisible in normal play (see Error log for why `MeshRenderer` removal had to happen *after* baking, not before) and doesn't interact with 2D gameplay physics (3D `MeshCollider`-less primitives with only `NavMeshModifier` - the default `BoxCollider` from `CreatePrimitive` is 3D and invisible to `Physics2D`).
- `EnemySet.prefab`'s `Enemy` child: `Rigidbody2D.bodyType` changed `Dynamic → Kinematic` (NavMeshAgent now owns movement; a Dynamic body would otherwise fight the agent's manually-synced `transform.position` every physics step) and `NavMeshAgent` added (`speed=2`, matching the old `moveSpeed` default). Edited once on the shared prefab via `PrefabUtility.EditPrefabContentsScope` — propagated automatically to both `Enemy` instances in all three stages (confirmed: Stage2/3's enemies are prefab-connected instances of the same `EnemySet.prefab`, not independent copies, despite those scenes predating the prefab's creation).
- Verified: NavMesh bake is real and non-degenerate in all 3 stages (`NavMesh.CalculateTriangulation()` → 56 vertices each, not 0 - an earlier XY-plane-oriented attempt genuinely produced an empty/degenerate mesh and was caught and corrected, see Error log). Noise-event wiring verified end-to-end: a real `DecoyObject` (via `SendMessage("Start")`, same pattern as 6.3) correctly triggers `ShadowEnemy.HandleNoise` through the actual C# event subscription (not called directly) when within hearing range. All state-entry methods (`EnterChase`/`EnterReturnToStart`/`EnterWander`) and `Update()` run without throwing even when `agent.isOnNavMesh` is `false`.
- **Not verified / honest limitation**: actual `NavMeshAgent` pathing behavior (does it really move, does `ChaseNoise`'s timeout really fire, does `ReturnToStart` really detect arrival) could **not** be confirmed at runtime. `agent.isOnNavMesh` stayed `false` for every agent created via edit-mode `Unity_RunCommand` scripting in this session, and an explicit attempt to enter real Play mode (`EditorApplication.isPlaying = true`) did not actually engage Play mode even after waiting (`isPlaying` read back `false`, `Time.time`/`Time.frameCount` stayed at `0`) - this MCP tool cannot reliably drive Play mode entry, at least not synchronously within one command. This is a new, broader instance of the project's long-standing "headless sessions don't reliably advance Time/the frame loop" limitation, now confirmed to extend to `NavMeshAgent` specifically (which appears to require genuine Play mode to activate at all, unlike `Rigidbody2D`/`Physics2D.Simulate()` which this project has successfully driven headlessly since Day 2). **Recommend a manual in-Editor Play-mode test of enemy wander/chase/return behavior before trusting this feature.**

### Error log (6.6-specific, in addition to the shared Priority 1 log above)

#### [6.6] Bug (self-caught before shipping): NavMesh baked empty when geometry was built in the wrong plane
- **Where**: first `NavMeshSurface.BuildNavMesh()` attempt in Stage1.
- **What happened**: `NavMesh.CalculateTriangulation().vertices.Length` was `0` - an apparently-successful bake (`BuildNavMesh()` returned, no exception) that had actually produced nothing usable.
- **Root cause**: geometry (a floor cube + 114 wall cubes) was built lying flat in the game's XY plane (thin along Z), matching this project's 2D gameplay coordinate system - but Unity's default NavMesh baking looks for surfaces facing the +Y "up" axis (the standard XZ-plane-walkable convention), and this package version has no "Rotate XY" option to reinterpret that. A surface facing +Z (this project's "up" in gameplay terms) is invisible to the default baking pass.
- **Fix applied**: rebuilt all NavMesh geometry in the standard XZ plane instead (floor spans X/Z, walls positioned at `(x, 1, z)`) and added the `GameToNav`/`NavToGame` conversion layer in `ShadowEnemy.cs` described above. Verified fix: vertex count went from `0` → `56`.
- **Prevented by**: always sanity-check `NavMesh.CalculateTriangulation().vertices.Length > 0` immediately after any `NavMeshSurface.BuildNavMesh()` call in a project without a 2D-rotate baking option - a "successful" bake with no exception can still be silently empty.

#### [6.6] Bug (self-caught before shipping): NavMesh also baked empty when the source MeshRenderers were removed before baking
- **Where**: same bake, second attempt (after fixing the plane orientation above) - still `0` vertices.
- **What happened**: geometry cubes had their `MeshRenderer` destroyed immediately after creation (intending to keep the helper geometry invisible during normal play), before `BuildNavMesh()` ran.
- **Root cause**: `NavMeshSurface.useGeometry` defaults to `NavMeshCollectGeometry.RenderMeshes` - it collects geometry from enabled `MeshRenderer`s, not from `MeshFilter`s or `Collider`s. Destroying the `MeshRenderer` first meant the surface had literally nothing to collect.
- **Fix applied**: reordered to build geometry → bake (`MeshRenderer` present and enabled) → *then* strip the `MeshRenderer` components. Verified fix: vertex count went from `0` → `56`.
- **Prevented by**: when generating throwaway/invisible geometry purely for `NavMeshSurface` baking, always strip renderers *after* the bake call, never before - or explicitly set `useGeometry = NavMeshCollectGeometry.PhysicsColliders` if renderers genuinely can't exist even temporarily.

#### [6.6] Environment limitation: `NavMeshAgent` never reports `isOnNavMesh = true` in edit-mode `Unity_RunCommand` scripting, and Play mode couldn't be reliably entered from this tool either
- **Where**: all `ShadowEnemy`/`NavMeshAgent` verification attempts this session.
- **What happened**: `agent.Warp(...)` completed without throwing, but `agent.isOnNavMesh` read `false` immediately after, for every test - even with `transform.position` well inside the baked floor's bounds. `agent.SetDestination(...)` then threw `"SetDestination can only be called on an active agent that has been placed on a NavMesh."` (an uncaught exception in the *original*, unguarded code - see the robustness fix above). A follow-up attempt to set `EditorApplication.isPlaying = true` and re-check in a later command did not actually enter Play mode (`isPlaying` read back `false`, `Time.time`/`frameCount` stayed `0`).
- **Root cause (best available explanation, not fully confirmed)**: `NavMeshAgent`, unlike `Rigidbody2D`, appears to require genuinely-running Play mode for its native pathfinding registration to activate at all - edit-mode component creation/field access works (it's a normal `MonoBehaviour` for serialization purposes), but the underlying navigation-mesh placement doesn't engage outside a live player loop. Combined with this MCP tool not reliably driving a real Play-mode transition (possibly because the command's own execution context doesn't survive across the domain reload a Play-mode transition triggers, similar to - but a harder failure than - the recompile-triggered "Unity not detected" reconnection blips already seen throughout this project), full runtime verification of agent pathing isn't achievable in this session.
- **Fix applied**: added `agent.isOnNavMesh` guards everywhere the agent is used (see "Robustness fix applied during verification" above) so the *shipped* code degrades gracefully instead of throwing, regardless of why an agent might be off-mesh at runtime. Verification was narrowed to what edit-mode scripting *can* confirm: the NavMesh geometry itself is real and correctly shaped (56 vertices, not 0), the noise-event subscription chain fires correctly end-to-end, and no method throws anymore under the off-mesh edit-mode condition.
- **Prevented by**: don't trust `isOnNavMesh`/pathing-dependent state read from a pure edit-mode `Unity_RunCommand` script as evidence of a `NavMeshAgent` bug - and don't assume `EditorApplication.isPlaying = true` issued from within a `Unity_RunCommand` script reliably enters Play mode. A genuine in-Editor manual Play-mode test is the only way to confirm `NavMeshAgent` behavior in this project; log verification as honestly incomplete rather than forcing a false pass, exactly as this checklist's testing notes have done for other headless-only limitations since Day 3.

> **Correction (found later in the same overall session, during the 5-stage level design pass)**: `EditorApplication.isPlaying = true` **does** eventually take effect - just with a much longer and unpredictable delay than the ~10-15s waited during the 6.6 check above. Direct evidence: a `Unity_RunCommand` scene-editing call later in this session failed with `"This cannot be used during play mode"`, and a follow-up check confirmed `EditorApplication.isPlaying` was `True` at that point - meaning the Play-mode transition from the earlier 6.6 attempt had silently completed at some point *after* that check gave up and reported `False`, and the session then sat in Play mode through several unrelated tool calls before this was noticed. So the underlying claim above ("Play mode couldn't be reliably entered from this tool") is **not quite right** - Play mode *can* be entered, but (a) confirming it takes much longer than expected, so an early isPlaying check can give a false negative, and (b) nothing automatically exits Play mode again, so it's easy to end up stuck in it without noticing (as happened here) until a scene operation fails. This means 6.6's core NavMeshAgent pathing verification is likely **achievable after all** with a longer wait-and-poll loop before giving up - worth retrying in a future session rather than treating it as a hard environment limitation. Always explicitly check and restore `EditorApplication.isPlaying` state before/after any Play-mode experiment in this environment.

#### 6.5 detail
- `VirtualJoystick.cs`: fixed-position joystick (background ring stays put, handle drags within it via `IPointerDownHandler`/`IDragHandler`/`IPointerUpHandler`, snaps back on release). **Decision**: fixed position over a "floating, appears where you first touch" joystick — simpler to build and verify, and predictable to find without looking (design tradeoff, not a technical limitation).
- `TouchAimFire.cs`: invisible full-height zone covering the right half of the screen, drag-from-press-point determines `AimDirection`, release sets a one-shot `fireTriggered` flag consumed via `ConsumeFireTrigger()`. **Decision**: kept decoy on its own dedicated button rather than trying to layer a decoy gesture into the same drag zone — avoids any gesture ambiguity between "aiming a ping" and "throwing a decoy".
- **Decision (platform reconciliation)**: didn't use compile-time flags (`#if UNITY_ANDROID`) or a runtime `Application.isMobilePlatform` branch at all. Instead, desktop and mobile inputs are combined by *priority*, not branched: `FixedUpdate()` uses WASD if any key is pressed, else falls back to `joystick.InputVector` (which is `(0,0)` if the joystick was never touched) — so on a real device with no keyboard, WASD is always zero and the joystick always wins; in the Editor, WASD keeps working exactly as before. Same pattern for fire: mouse-click OR a consumed touch-drag-release, either can trigger `Fire()`. This avoids any platform-detection code entirely and both paths stay live and testable in the Editor simultaneously, which is also why it was verifiable at all in this MCP-only environment (no real device or Unity Remote available this session).
- `HybridPlayerController.FixedUpdate()`: movement velocity changed from `input.normalized * moveSpeed` to `Vector2.ClampMagnitude(input, 1f) * moveSpeed` — mathematically identical to the old behavior for WASD's discrete ±1 axes (verified: diagonal magnitude 1.41 clamps to 1, same as normalized; single-axis magnitude 1 is unchanged), while also correctly preserving the joystick's partial-push magnitude for analog movement speed (verified: 0.5 push → velocity magnitude 2.5 at `moveSpeed=5`).
- `HybridPlayerController.Update()`: fire trigger is `desktopFire || touchFire`; when the trigger came from a touch-drag release, `facingDirection` is overridden to `touchAimFire.AimDirection` *before* calling `Fire()` (and the `facingIndicator` repositioned to match) — this is the "aiming is independent of movement" mobile behavior from the design doc, while desktop clicks keep firing toward the WASD-driven `facingDirection` as before.
- `DecoyThrow.cs` refactored: extracted the cooldown-gated logic from the `Update()` Space-key branch into a new public `TryThrow()`, called both by the Space-key check and by the mobile decoy button's `OnClick` (via `UnityEventTools.AddPersistentListener`) — removes the duplicate cooldown check that would otherwise exist between the two input paths.
- Scene setup (Stage1/2/3, all three): `TouchAimZone` (invisible `Image`, right half via anchors `(0.5,0)`-`(1,1)`), `JoystickBackground`+`JoystickHandle` (semi-transparent white ring+handle, fixed bottom-left at `(150,150)`, range 80px) with `VirtualJoystick`, `DecoyButton` (yellow, bottom-right, drawn after/on-top-of the aim zone so its click area takes priority). All wired into that scene's `Player`'s `HybridPlayerController` (`joystick`/`touchAimFire` fields) and `DecoyThrow` (button's `OnClick` → `TryThrow()`).
- Verified directly via the public `IPointerDownHandler`/`IDragHandler`/`IPointerUpHandler` interface methods (constructing real `PointerEventData`, not `SendMessage` or reflection — these are just public interface methods): joystick center-press → `(0,0)`; dragged to exactly the range edge → `(1,0)`; dragged past the range → magnitude still clamped to `1`; release → resets to `(0,0)`. `TouchAimFire`: no trigger before any drag; drag direction correctly normalized (`(0,1)` for a straight-up drag); release sets the trigger exactly once (second `ConsumeFireTrigger()` call returns `false`). Full integration test: half-push joystick (`InputVector=(0.5,0)`) → `Rigidbody2D.linearVelocity=(2.5,0)` (`0.5 × moveSpeed 5`) and `FacingDirection=(1,0)` — confirms the analog-magnitude math and the joystick→movement wiring end-to-end.
- **Manual fallback**: real device/Unity Remote touch testing not possible in this MCP-only session — logged as pending, same as 6.6's eventual on-device verification and Priority 2's 6.10 Android build pass.

#### 6.1 detail
- `AudioManager.cs`: `wallHit` field/`PlayWallHit()` replaced entirely (not kept alongside) with `normalWallClip`/`crackedWallClip`/`monsterClip` fields and `PlayNormalWallHit(float)`/`PlayCrackedWallHit(float)`/`PlayMonsterHit(float)`, all routed through a shared private `PlayDelayedClip(clip, distance)` helper (same `PlayDelayed(distance/speedOfSound)` behavior as before, de-duplicated).
- `PingProjectile.OnCollisionEnter2D` branches on tag: `CrackedWall` → `PlayCrackedWallHit`, `Enemy` → `PlayMonsterHit`, `Wall` → `PlayNormalWallHit`.
- **Decision**: reused the existing `Enemy` tag as the "monster" tag rather than introducing a separate `Monster` tag — `ShadowEnemy` objects were already tagged `Enemy` (from work in the previous session), and `PingProjectile.cs` already had an `Enemy`-tag branch wired in from that same prior session. Introducing a parallel `Monster` tag would have meant either retagging existing enemies or maintaining two tags for the same concept.
- Created a new `CrackedWall` tag via `InternalEditorUtility.AddTag`.
- **Manual fallback (audio)**: 3 new clip slots on `AudioManager` are empty, same situation as Day 4's original 5. Suggested character: `normalWallClip` = solid dull thud (unchanged from the old `wallHit`); `crackedWallClip` = hollow, slightly resonant thunk (implies emptiness/weakness behind it); `monsterClip` = dissonant, organic screech (unsettling, non-mechanical).

#### 6.2 detail
- `CrackedWall.cs`: `hitsToBreak` (default 3), `brokenVisualPrefab` (optional), `RegisterHit()` decrements and calls `Break()` at 0, `BreakImmediately()` for the monster-charge case, both no-op once already `broken`. `Break()` disables `Collider2D`+`SpriteRenderer`, optionally instantiates `brokenVisualPrefab`, plays a break sound.
- Added `AudioManager.PlayWallBreak()` + `wallBreak` clip field — a one-shot "wall gives way" sound, distinct from the per-hit `crackedWallClip` (hit sound plays on every `RegisterHit`, break sound plays once when it actually breaks).
- `PingProjectile.OnCollisionEnter2D`: on the `CrackedWall` branch, also calls `CrackedWall.RegisterHit()` via `GetComponent`.
- `ShadowEnemy.OnCollisionEnter2D`: new first branch — colliding with a `CrackedWall`-tagged object calls `BreakImmediately()` and returns early (doesn't fall through to the player-death check).
- Placed `CrackedWall_Test` in `Stage1.unity` at `(10, 2)` — brownish-tinted wall sprite, `Wall` physics layer (layer 6, same as normal walls — confirmed via `Physics2D.GetIgnoreLayerCollision` that Wall↔Ping/Default/Player are all already non-ignored, so no collision-matrix change was needed), `CrackedWall` tag.
- Verified directly (`RegisterHit()` × 3 → `Collider2D`/`SpriteRenderer` both disabled; a 4th call is a no-op; separate instance verified `BreakImmediately()` breaks in one call).

#### 6.3 detail
- `DecoyThrow.cs` (added to `Player.prefab`, so all 3 stages get it automatically — confirmed via `PrefabUtility.GetPrefabInstanceStatus` that Stage1/2/3's `Player` are all `Connected` instances of `Player.prefab`): `decoyPrefab`, `decoyThrowDistance`, `decoyCooldown`, `decoyCooldownIndicator` (same radial-fill pattern as Day 3's ping `cooldownIndicator`). Reads `HybridPlayerController.FacingDirection`/`.ControlsEnabled` (both newly exposed as public read-only properties — previously private).
- **Decision**: bound decoy throw to `Keyboard.current.spaceKey`, distinct from ping's mouse-click fire — explicit placeholder per the prompt, to be replaced by 6.5's mobile UI button.
- `DecoyObject.cs`: static `event Action<Vector2,float> OnDecoySpawned`, fires in `Start()` with its own position + `noiseRadius`; no `Light2D` on the prefab at all (sound-only, per design doc); self-destructs via `Destroy(gameObject, lifespan)`.
- Added `AudioManager.PlayDecoyLand()` + `decoyLand` clip field.
- Created `Assets/Prefabs/DecoyObject.prefab` (small semi-transparent yellow marker sprite, reuses `WallTile.png` as a placeholder texture — no dedicated decoy art yet, see Priority 3's art shopping list) and wired it into `Player.prefab`'s `DecoyThrow.decoyPrefab`.
- Added a `DecoyCooldownIndicator` `Image` (yellow, radial fill) to each of Stage1/2/3's `Canvas`, positioned beside the existing ping `CooldownIndicator`, wired per-scene to that scene's `Player`'s `DecoyThrow` (this part can't live on the prefab since each `Canvas` is scene-local).
- Verified: `DecoyObject`'s noise event fires with the correct position/radius on `Start()`. `DecoyThrow`'s spawn-position math confirmed correct (facing `(1,0)`, distance `3` → decoy spawned at `(3,0,0)`) — **but only by driving `Throw()` directly via `SendMessage`**, not by simulating the actual Space-key press end-to-end; see Error log for why.

#### 6.4 detail
- `MossZone.cs`: `OnTriggerEnter2D`/`OnTriggerExit2D` on `Player` tag set `HybridPlayerController.IsInMossZone` (new public settable property) true/false.
- `HybridPlayerController.Fire()`: after instantiating the ping, calls `PingProjectile.SetDampened(IsInMossZone)`.
- `PingProjectile.cs`: new `dampened` field + public `SetDampened(bool)`. `OnCollisionEnter2D` skips all `AudioManager` hit-sound calls when `dampened` (but still stops velocity, disables collider, hides sprite, and still calls `CrackedWall.RegisterHit()` — the physical interaction is unaffected, only its sound/light signature is suppressed). `FlashAndDestroy()` has an early dampened branch: sets `light2D.intensity = 0` immediately (no ramp, no `OverlapCircleAll` enemy-reveal), waits the same `flashDuration` for pacing, then destroys — same lifetime, no visual/audio footprint.
- **Decision**: implemented the moss visual as a plain `SpriteRenderer` (green tint, `Sprite-Unlit-Default` material so it stays visible even at `Global Light 2D` intensity 0) directly on the same trigger `GameObject`, rather than a second `Tilemap` layer with its own palette/material. A dedicated moss `Tilemap` would need a whole parallel tile-layer + palette + material setup for the same visual outcome; a prototype-stage trigger-zone-with-a-sprite achieves "visibly distinct, always visible" without that overhead. Revisit if moss zones need irregular tile-by-tile shapes later.
- Created a `MossZone` tag and `Assets/Tiles/MossUnlit.mat` (Unlit URP 2D sprite material). Placed `MossZone_Test` in `Stage1.unity` at `(6, -3)`, scale 3×3, green semi-transparent, `BoxCollider2D` (`isTrigger=true`).
- Verified: `MossZone`'s trigger enter/exit correctly flips `HybridPlayerController.IsInMossZone` (driven via `SendMessage("OnTriggerEnter2D"/"OnTriggerExit2D", playerCollider)` on the real component, not just setting the property by hand). Confirmed a ping's `dampened` field ends up `true` when spawned while `IsInMossZone` is `true`. Did not separately re-verify the `AudioManager`-skip / no-ramp branches at runtime beyond code review — they're simple boolean guards on already-verified code paths (6.1's audio branch, the existing flash coroutine).

### Error log

#### [6.3] Testing-harness limitation: `wasPressedThisFrame` never true outside Play mode
- **Where**: MCP `Unity_RunCommand` verification script for `DecoyThrow`, probing `Keyboard.current.spaceKey.wasPressedThisFrame` directly after `InputSystem.QueueStateEvent` + `InputSystem.Update()`.
- **What happened**: `isPressed` correctly reflected `False → True → False` across simulated press/release, but `wasPressedThisFrame`/`wasReleasedThisFrame` read `False` at every step, even immediately after queuing a fresh press with no prior press queued.
- **Root cause**: edge-triggered Input System queries (`wasPressedThisFrame`) key off an internal per-frame update-step counter that only advances with Unity's actual running player loop. Pure edit-mode `Unity_RunCommand` execution (never entering Play mode) doesn't advance that counter between manual `InputSystem.Update()` calls within the same script execution, so every "frame" looks identical and the edge never registers. Level-triggered queries (`isPressed`) aren't affected and worked fine (used successfully for WASD facing-direction tests both here and in the existing ping-fire tests).
- **Fix applied**: none needed in game code — `DecoyThrow.cs` uses the same `wasPressedThisFrame` pattern as the already-shipped, working ping-fire code in `HybridPlayerController.cs`, so this is a testing-methodology constraint, not a product bug. Verified the actual `Throw()` logic (spawn position math) by invoking it directly via `GameObject.SendMessage("Throw")`, bypassing the input-edge-detection step entirely.
- **Prevented by**: for any future edge-triggered input verification in this project, either enter real Play mode first, or bypass the input layer and call the target method directly via `SendMessage`. Don't trust a `False` `wasPressedThisFrame` reading from a pure edit-mode `Unity_RunCommand` script as evidence of a bug.

#### [6.3] MCP tool error: `System.Reflection` is blocked in `Unity_RunCommand` scripts
- **Where**: first attempt at the `DecoyThrow`/`HybridPlayerController` unit test.
- **What happened**: `UNEXPECTED_ERROR: Script uses one or more unauthorized namespaces: Namespace System.Reflection is imported`.
- **Root cause**: the MCP `Unity_RunCommand` sandbox disallows `System.Reflection` entirely (presumably to prevent scripts from bypassing other sandboxing via reflection), which blocked the originally-planned approach of setting private fields (`facingDirection`) and invoking private methods (`Throw()`) directly.
- **Fix applied**: switched to non-reflection alternatives already established in this project — `SerializedObject`/`SerializedProperty` for reading/writing `[SerializeField]` private fields, real simulated Input System events + `SendMessage("FixedUpdate")` for level-triggered state (facing direction), and `GameObject.SendMessage("<methodName>")` (which Unity resolves independent of C# access modifiers, unlike `System.Reflection`) to invoke a private method directly.
- **Prevented by**: never `using System.Reflection` in an `Unity_RunCommand` script in this environment — use `SerializedObject` + `SendMessage` for everything reflection would normally be reached for.

#### [6.4] Benign: `Assertion failed on expression: 'ShouldRunBehaviour()'` from edit-mode `SendMessage` on physics callbacks
- **Where**: MCP `Unity_RunCommand` verification script for `MossZone`, calling `mossGO.SendMessage("OnTriggerEnter2D", playerCol)` / `"OnTriggerExit2D"` outside Play mode.
- **What happened**: Unity logged an internal engine assertion (`ShouldRunBehaviour()`) to the console for each call, surfaced as an `Error`-type console entry.
- **Root cause**: `SendMessage`-invoking a Unity physics-callback method name (`OnTriggerEnter2D`/`OnTriggerExit2D`) outside of an actual physics callback context trips an internal engine sanity check that these methods are only meant to be invoked by the physics engine itself during Play mode. The call still executes and its side effects (setting `IsInMossZone`) are still observed correctly (confirmed via before/after logging) — the assertion is noise, not a failure.
- **Fix applied**: none needed — this is a known-noisy but harmless way to test physics-callback methods outside Play mode; documented here so it isn't mistaken for a regression in a future session.
- **Prevented by**: expect this exact assertion message whenever `SendMessage`-ing an `OnTrigger*`/`OnCollision*` method name outside Play mode in this project; it doesn't indicate the test failed.

## Post-6.6 refinements (user-requested, after the Priority 1 checkpoint)

User feedback after reviewing 6.6/6.5: (1) a ping hit should make an enemy chase the *player* for a fixed duration, not just path to the fixed point where the ping flashed; a decoy should still redirect aggro to its own (fixed) drop point, even overriding an in-progress player-chase. (2) Ping needs a dedicated mobile button (previously only drag-to-fire existed), positioned bottom-right alongside the decoy button, with each cooldown indicator overlaid exactly on its own button rather than floating elsewhere.

- **`ShadowEnemy.cs`**: `ChaseNoise` split into two states, `ChasePlayer` and `ChaseDecoy`. Noise subscriptions also split: `HandlePingNoise` → `EnterChasePlayer()` (no target position needed - `UpdateChasePlayer()` re-`SetDestination`s to the live `GameObject.FindWithTag("Player")` position every frame for `chasePlayerDuration` (3f) before giving up to `ReturnToStart`). `HandleDecoyNoise` → `EnterChaseDecoy(position)` (fixed one-shot destination, existing `chaseTimeout` (4f) give-up behavior, unchanged from before this refinement). **Decision**: `HandleDecoyNoise` always re-enters `ChaseDecoy` if in hearing range regardless of current state (including interrupting an active `ChasePlayer`) - a decoy's whole purpose is redirecting aggro, so it should always win when heard. Catching the player is still handled entirely by the pre-existing `OnCollisionEnter2D`→`Die()` path, untouched by this change.
- **`HybridPlayerController.cs`**: fire logic refactored into public `TryFirePing()` (cooldown-gated, mirrors `DecoyThrow.TryThrow()`'s pattern exactly), callable from both the existing mouse-click/touch-drag-release paths and the new mobile button. Verified: `TryFirePing()` fires once then correctly blocks an immediate second call (cooldown gate confirmed working via a clean before/after clone-existence check, after an initial test script bug - miscounted the source template object as a clone - was caught and fixed).
- Scene setup (Stage1/2/3): new `PingButton` (blue, bottom-right, positioned directly left of the existing `DecoyButton` with a 20px gap) wired to `TryFirePing()`. The existing `CooldownIndicator` (ping) and `DecoyCooldownIndicator` `RectTransform`s were **repositioned to exactly match** `PingButton`'s and `DecoyButton`'s anchors/pivot/size/position respectively (not just placed nearby) and moved to render as the last sibling (on top) - so each indicator now visually overlays its own button instead of floating at its old Day-3-era position.
- **Not independently re-verified**: `ChasePlayer`'s continuous re-targeting and `ChaseDecoy`'s fixed-target behavior are subject to the exact same `NavMeshAgent`-requires-Play-mode limitation already documented in 6.6's Error log (`agent.isOnNavMesh` still `false` in edit-mode testing, so `agent.destination` couldn't be read back as confirmation). State-entry methods (`EnterChasePlayer`/`EnterChaseDecoy`) were confirmed to run without throwing. This refinement inherits 6.6's existing "needs a manual in-Editor Play-mode test" recommendation - doesn't add a new risk, just carries the same one forward.

## 5-Stage Level Design (user-provided design table)

User provided a themed 5-stage progression table and asked for it to be built:

| Stage | Theme (KR) | New learning element |
|---|---|---|
| 1 | 감각의 통로 A | Movement + ping firing tutorial (no monsters) |
| 2 | 감각의 통로 B | Cracked-wall breaking tutorial (no monsters) |
| 3 | 사냥꾼의 미로 A | 1 monster, patrol avoidance + first cracked-wall shortcut dilemma |
| 4 | 사냥꾼의 미로 B | 2 monsters, decoy-forced section |
| 5 | 심연의 중심지 | 3 monsters, moss stepping-stones, all mechanics combined |

All 5 stages share the same outer room footprint (perimeter ring `(-1,-8)`-`(30,8)`, 32×17 — unchanged from Day 2/5 so `CameraFollow` bounds, `Player` spawn `(0,0)`, and `Goal`/`StageExit` position `(27,0)` didn't need touching) and only vary the **interior** wall layout, monster count/placement, and mechanic placement per theme. `Assets/Scenes/Stage4.unity`/`Stage5.unity` created (duplicated from `Stage3.unity` for its component set, then fully rebuilt).

- **Stage1** ("corridor A"): the original Day-2-style 2-partition zigzag (vertical walls at x=10 gap-top, x=20 gap-bottom) — pure movement+ping, no monsters, no cracked walls, no moss. Removed both `EnemySet` instances and the 6.2/6.4 test placeholders (`CrackedWall_Test`/`MossZone_Test`).
- **Stage2** ("corridor B"): two full-height blocking walls (x=12, x=22), each with its 3-cell gap covered by a `CrackedWall` obstacle (not a Tilemap tile) instead of being open — the *only* way through is breaking it. No monsters, no moss.
- **Stage3** ("hunter's maze A"): 1 monster, a genuine shortcut-vs-safe-route dilemma. **This went through a self-caught redesign** — see Error log below; the shipped version is a single full-height wall at x=15 with two openings: a `CrackedWall` at y=0 (direct line between spawn/goal, right where the monster wanders) and a genuinely open gap at y=7 (top edge, away from the monster — a real detour, not just a same-length alternate opening). Monster at `(15,1)`, `wanderRadius=4` (covers the crack, not the detour).
- **Stage4** ("hunter's maze B"): 2 monsters. A single full-height wall at x=14 with exactly one 1-cell gap at y=0 — Monster1 spawns right on top of it with a tightened `wanderRadius=2f`/`hearingRadius=10f` (default is 5f/8f) so it's very hard to slip through undetected without first throwing a decoy elsewhere to pull it off the gap. Monster2 patrols the far side (`(23,0)`, default radius) for general pressure near the goal.
- **Stage5** ("abyss center"): 3 monsters, deliberately *open* interior (not another maze) to match the "abyss" theme. One `CrackedWall` gate at x=15 (final callback to the 6.2 mechanic, same full-height-wall-with-gap pattern as Stage2/4) with a line of 6 `MossZone` "stepping stone" patches at y=0 (x=3,7,11,19,23,27) leading straight through it. All 3 `EnemySet` instances (the 3rd duplicated fresh from `EnemySet.prefab`) spread across the open field at `(7,5)`/`(23,5)`/`(15,-5)` with widened `wanderRadius=6f`, positioned off the y=0 moss lane so following the stepping stones is a meaningfully safer route, not mandatory-but-equal.
- **NavMesh**: fully rebaked for all 5 stages against their new layouts (previous bakes were for the old shared 3-stage-duplicate layout and are now stale). Bake step improved from 6.6's version: `CrackedWall` obstacle GameObjects (which live outside the Tilemap) are now also included as `NavMeshModifier`-marked obstacles, so enemies won't path straight through an unbroken cracked wall in their internal pathing logic. Vertex counts: Stage1=56, Stage2=52, Stage3=31, Stage4=24, Stage5=32 (all non-zero/non-degenerate).
- **Build settings**: `EditorBuildSettings.scenes` updated to `TitleScreen, Stage1, Stage2, Stage3, Stage4, Stage5, EndScreen` (7 total). `GameManager.LoadNextStage()`'s existing `SceneManager.sceneCountInBuildSettings - 2` stage-count logic needed **no code change** — it automatically picked up 5 stages instead of 3.
- **Verified via flood-fill** (not just "looks right" — an actual BFS over the Tilemap+`CrackedWall`-collider-derived walkable graph, run per stage): all 5 stages confirmed solvable spawn→goal *assuming cracked walls get broken*. Separately verified, treating each `CrackedWall`'s actual `BoxCollider2D.bounds` (not an approximated cell range) as blocked: Stage2 and Stage5's cracks are **mandatory** (goal unreachable without breaking, confirmed `False`) — correctly forcing the mechanic; Stage3's crack is **optional** (goal still reachable via the y=7 detour without breaking, confirmed `True`) — correctly implementing the intended dilemma rather than a forced break.

## Post-level-design bug reports (user-reported)

User reported two issues after the 5-stage build: (1) the decoy button didn't respond to clicks, (2) asked for camera-follow / map-boundary / character-clipping-through-the-map to be fixed.

- **Decoy button not clickable - root cause found and fixed**: `DecoyCooldownIndicator` had been repositioned in the "Post-6.6 refinements" pass to exactly overlay `DecoyButton` (same anchors/pivot/size/position) and moved to render as the last sibling (on top) — but its `Image.raycastTarget` was left at Unity's default `true`. Since it sits at the identical screen position as the button and renders above it, `GraphicRaycaster` was hitting the indicator first and never reaching the `Button` underneath, silently swallowing every click. The ping `CooldownIndicator` already had `raycastTarget=false` (apparently set correctly from its original Day 3 creation), which is why only the decoy button was reported broken, not the ping one. **Fix**: set `raycastTarget=false` on both cooldown indicator `Image`s (decoy's fix was the actual bug; ping's was already correct, set again defensively for consistency) across all 5 stages. Confirmed via direct inspection before fixing: `DecoyButton` itself had a correctly-wired `TryThrow` persistent listener the whole time — the button logic was never the problem, only the invisible indicator sitting on top of it intercepting the click.
- **Camera boundary / wall-clipping investigation**: could not reproduce an actual wall-tunneling bug via direct physics testing (`Physics2D.Simulate`-driven ramming at high speed, both straight-line and diagonal, into the perimeter wall, an interior partition, and a room corner — the player correctly stopped at the wall edge every time, never tunneled through). `TilemapCollider2D` was confirmed present/enabled with correctly-regenerated bounds matching the new per-stage layouts, and the `Player`↔`Wall` physics layer pair is not ignored. **However, a real (if more cosmetic) issue was found and fixed**: `CameraFollow`'s `minBounds`/`maxBounds` were `(-1,-8)`-`(31,9)`, but the wall ring's true outer face (tiles are 1×1, centered on integer coordinates `-1..30`/`-8..8`) sits at `(-1.5,-8.5)`-`(30.5,8.5)` — the old bounds over-extended past the actual wall by 0.5 units on the top/right, meaning the camera could clamp to a position that reveals a sliver of empty void just past the wall's outer edge (not an actual walk-through-the-wall bug, but could plausibly read as "the map boundary isn't applied correctly" or even look like the character could walk into that visible gap). **Fix**: tightened bounds to `(-1.5,-8.5)`-`(30.5,8.5)` (the wall ring's actual outer face) across all 5 stages. **Also applied defensively**: `Player`'s `Rigidbody2D.collisionDetectionMode` changed from `Discrete` to `Continuous` in all 5 stages — synthetic tests didn't reproduce tunneling, but `Continuous` is strictly safer against any high-speed clipping through the relatively thin (1-unit) wall colliders that this session's specific test velocities/step sizes might not have happened to trigger (e.g. real mouse/touch-drag-driven input, or the newer joystick analog movement, could produce different per-frame deltas than the synthetic ramming tests used here). Negligible performance cost at this project's scale (a handful of dynamic bodies).
- **Not fully resolved / worth re-checking manually**: since no actual tunneling was reproduced synthetically, the "character clips through the map" report might describe something this session's testing didn't capture (e.g. a Play-mode-only timing issue, or something specific to one of the newer Stage4/5 layouts under real input) — the camera-bounds fix and `Continuous` collision detection are both genuine improvements either way, but if clipping is still observed after this fix, it needs a real in-Editor Play-mode repro (which hits the same Play-mode-testing limitation already logged for 6.6) rather than further synthetic `Physics2D.Simulate` attempts.

## Ping investigation (user-reported: "ping doesn't seem to work properly")

Investigated thoroughly but **could not reproduce a concrete break** - everything inspectable checks out:
- `HybridPlayerController.projectilePrefab` correctly assigned (`PingProjectile` prefab, not null).
- `PingButton`'s persistent listener correctly wired to `Player.TryFirePing` (same pattern as the now-fixed `DecoyButton`).
- `PingProjectile.prefab` has all required components (`Rigidbody2D`, `Collider2D`, `SpriteRenderer`, `PingProjectile` script) on the correct `Ping` physics layer, and `Ping`↔`Wall` collision is not ignored.
- `TryFirePing()` cooldown-gating logic itself was already directly verified working earlier this session (Post-6.6 refinements testing).
- One minor discrepancy noticed (not clearly related): the live `pingCooldown` value on `Player.prefab` reads `1`, not the `1.5` documented as the field's default since Day 3 - likely a stray Inspector edit from sometime in this project's history, not something touched this session. Left as-is since it doesn't explain a "doesn't work" symptom (would just cooldown slightly faster), but worth knowing about if cooldown timing ever comes up again.

**New environment finding while investigating**: confirmed `EditorApplication.isPlaying = true` *does* eventually put the session into genuine Play mode with `Time.time`/`Time.frameCount` actively advancing (reached `Time.time≈32s`, `frameCount` in the thousands) - a real breakthrough versus 6.6's original "Play mode can't be entered" conclusion. However, **`Time` only appears to advance while a `Unity_RunCommand` call is actively being processed** - a real-world `sleep`/wait between tool calls, with no active RPC pumping the Editor's message loop, produces **zero** frame progress (`Time.time` was bit-for-bit identical across a 3-second real wait with no tool call in between). This makes simulating "hold a click for N real seconds and let the game's own Update() loop notice it" fundamentally impractical from this tool - `wasPressedThisFrame`-style edge detection needs an actual per-frame boundary crossing that this environment can't reliably produce even in genuine Play mode, only in true interactive use (a human actually playing, or a proper standalone/batchmode test runner). Session was cleanly exited back to Edit mode afterward (confirmed `isPlaying=false`) - not left stuck in Play mode this time.

**Conclusion at the time**: nothing found and fixed here, unlike the decoy button and camera-bounds issues above. All static wiring and previously-tested logic paths look correct. Asked the user to describe the exact symptom rather than keep re-verifying wiring that already checked out.

### Root cause found on follow-up: `Physics2D.simulationMode` was left in `Script` (manual) mode - this session's own fault

User's follow-up symptom report nailed it precisely: **"character can't move, only the facing/aim rotation works"** + **"ping spawns but light/wall-detection/movement all fail"**. Both symptoms have one shared explanation: `Rigidbody2D.linearVelocity` was being set correctly every `FixedUpdate` (for both `Player` and `PingProjectile`), but that velocity was **never being integrated into actual position**, because the *global* `Physics2D.simulationMode` had been left set to `Script` instead of the normal `FixedUpdate` - meaning Unity was no longer auto-stepping 2D physics at all, only ever advancing it when something explicitly calls `Physics2D.Simulate()`. Direct transform writes (like `facingIndicator.localPosition = facingDirection * indicatorOffset`, which isn't physics-driven) kept working fine, which is exactly why aiming/rotation still visibly responded while actual movement didn't - and why the ping could still `Instantiate()` (not physics-dependent) but then just sit motionless, never colliding with anything (no wall hit → no light flash, no sound, no `CrackedWall`/monster interaction).

- **Where this came from**: earlier in *this same session*, several verification scripts (the wall-collision ramming tests, the diagonal-corner tests) explicitly set `Physics2D.simulationMode = SimulationMode2D.Script;` to drive deterministic manual physics steps for testing - a pattern used successfully and safely throughout this project since Day 2. The difference this time: those test scripts never set it back to `SimulationMode2D.FixedUpdate` afterward. Because this is a *global* Editor/project setting (not scoped to a single script execution or scene), it stayed in `Script` mode for every scene opened afterward, including whatever the user was actually testing in - a real regression this session introduced and failed to clean up.
- **Confirmed directly**: `Physics2D.simulationMode` read back as `Script` when checked. Also confirmed in `ProjectSettings/Physics2DSettings.asset` (`m_SimulationMode: 0` after the fix = `FixedUpdate`; would have read a different value while broken) - meaning this had actually been persisted to the project settings file, not just a transient in-memory Editor state, so it would affect *any* future session opening this project until fixed.
- **Fix applied**: `Physics2D.simulationMode = SimulationMode2D.FixedUpdate;` + `AssetDatabase.SaveAssets()`. Verified: `Physics2D.simulationMode` now reads `FixedUpdate`, and `Physics2DSettings.asset` reflects it. Also double-checked no other global state was left dirty from testing this session (`Time.timeScale=1` correct, `EditorApplication.isPlaying=False`/`isPaused=False` correct - the Play-mode session from the earlier ping investigation had already been cleanly exited).
- **Prevented by**: any `Unity_RunCommand` script in this project that sets `Physics2D.simulationMode = SimulationMode2D.Script` for deterministic manual testing **must** restore it to `SimulationMode2D.FixedUpdate` before the script ends (or at least before ending the session) - it is a global, persisted setting, not scoped to the calling script or even to Edit mode. This is now the single most important housekeeping rule for any future `Physics2D.Simulate()`-based verification in this project; retroactively, every prior session's manual-physics tests *should* have been checked for this too, though no evidence surfaced that this was ever left broken before this session.

## Camera boundary hardening (user-requested follow-up)

User asked for the camera boundary to be made unable-to-escape. Re-reading `CameraFollow.cs` while investigating found a real (if narrower) gap: the `minBounds`/`maxBounds` clamp was only applied to `smoothed` (the `SmoothDamp`-eased follow position) - `CameraShake.CurrentOffset` was then added **after** that clamp, with the combined result written straight to `transform.position` unclamped. A shake event (wall-hit ping, decoy landing, enemy reveal, player death) firing while the camera was already near a map edge could push the camera's actual rendered position past the boundary for the duration of the shake, revealing a sliver of space beyond the walls.

- **Fix**: `CameraFollow.LateUpdate()` now re-clamps the *final* position (`smoothed + shakeOffset`) against the same `minBounds`/`maxBounds` range, not just the pre-shake `smoothed` value. When there's no shake (`shakeOffset = Vector3.zero`), this is a no-op (clamping an already-in-range value against the same range doesn't change it) - normal follow behavior is unaffected either way.
- **Verified**: compiles clean; confirmed by code inspection that the added clamp uses the identical bounds/`camHalfWidth`/`camHalfHeight` values already used for the pre-shake clamp, so the final position is guaranteed within range by construction (`Mathf.Clamp`'s output is always within its given range, regardless of input). A live runtime confirmation (pushing an extreme shake through several real frames) hits the same `SmoothDamp`-needs-real-`Time.deltaTime` limitation noted elsewhere in this project's edit-mode testing - not re-litigated here, the fix is simple enough to trust by inspection.
- This is a single shared script fix (`Assets/Scripts/CameraFollow.cs`) - applies automatically to all 5 stages without any per-scene changes needed.
- Also re-confirmed no other global Editor state was left dirty from testing during this investigation (`Physics2D.simulationMode` still `FixedUpdate`, `EditorApplication.isPlaying` correctly `False` - note: `EditorSceneManager.OpenScene` briefly failed with `"cannot be used during play mode"` mid-investigation, most likely because **the user was independently testing the game in Play mode in their own Editor session at that moment** - not something this session triggered; no forced-exit action was taken since `isPlaying` had already returned to `False` by the time it was checked).

### Error log (level design)

#### Bug (self-caught before shipping): Stage3's original two-partition dilemma layout had a flanking leak that fully bypassed the intended mechanic
- **Where**: first version of Stage3's rework (horizontal partition y=3 spanning only x=4-16, plus a separate vertical partition x=20).
- **What happened**: not caught by simple visual inspection — only found by manually re-tracing the walkable graph. The horizontal partition at y=3 only spanned x=4 to x=16, not touching either side perimeter wall; the vertical partition at x=20 started at a different x than where the horizontal partition ended (x=16). This left an *unintended* fully-open corridor at x=17..19 connecting the bottom and top halves of the room with no obstruction at all - a player could walk from spawn straight up through that gap and around both partitions entirely, never needing to approach the cracked-wall shortcut, defeating the entire "dilemma" premise the stage was designed around.
- **Root cause**: treating a single-row/single-column partition as if it fully "seals" a region, without checking whether it actually touches a perimeter wall (or another partition) on *both* ends. A horizontal wall only blocks vertical movement within its own x-range - it does nothing to stop a player from simply walking around either end if that end isn't sealed against something else.
- **Fix applied**: redesigned as a single full-height wall (touching both the top and bottom perimeter walls, no gaps except the two intentional ones) instead of two separately-placed partial partitions - see the finalized Stage3 description above. Verified via the flood-fill check (see above) that this version is *not* trivially bypassable.
- **Prevented by**: for any future level layout in this project, a partition is only a real barrier if it touches a perimeter wall (or another confirmed-sealing partition) on **both** ends spanning the relevant axis - and even then, verify with an actual flood-fill/BFS over the tile grid rather than trusting visual/mental tracing, exactly as this session eventually did. This class of bug is easy to introduce and easy to miss by eye.

#### Testing-methodology note: manual cell-range math for a scaled `BoxCollider2D` is error-prone - use `Collider2D.bounds` instead
- **Where**: first attempt at the "are cracked walls actually mandatory" verification script.
- **What happened**: a manually-computed cell range from `transform.localScale` (using `Mathf.RoundToInt(scale/2)` for a half-extent) produced a range that was off by one cell for a scale of exactly `3` (`RoundToInt(1.5)` banker's-rounds to `2`, not `1`), silently mis-covering the actual gap and producing a false "not mandatory" result for Stage2/Stage5 that had nothing to do with the actual level design.
- **Fix applied**: switched to reading the real `BoxCollider2D.bounds` (`b.min`/`b.max`) and converting those world-space bounds to cell coordinates, which exactly matches what the physics engine (and thus the actual game) considers blocked - no manual scale-to-cell-range math needed.
- **Prevented by**: when a verification script needs to know what area a collider covers, read the collider's actual `bounds`, don't re-derive it from `transform.localScale`/position by hand.

### Priority 2 — Integration and completeness
- [ ] 6.7 Coordinate system prep for future single-map merge
- [ ] 6.8 Checkpoint-based death/respawn (not stage-restart)
- [ ] 6.9 Mobile resolution/safe-area handling
- [ ] 6.10 Android build + device test pass
### Error log

### Priority 3 — Polish
- [ ] 6.11 AI-generated texture/moss art integration
- [ ] 6.12 Monster variety (visual-type vs audio-type)
- [ ] 6.13 Sound-based minimap hint system
### Error log

## Notes / decisions (Day 6+)
- **[6.1]** Reused the existing `Enemy` tag instead of adding a `Monster` tag — see 6.1 detail above.
- **[6.2]** `CrackedWall` placed on the same `Wall` physics layer (6) as normal walls rather than a new layer — the collision matrix already permits Ping/Default/Player to all collide with layer 6 (confirmed, not assumed), so a new layer would add complexity with no behavioral benefit.
- **[6.3]** Decoy throw bound to `Space` as an explicit placeholder desktop binding, to be replaced by 6.5's dedicated mobile button.
- **[6.3]** Decoy prefab and moss-zone visual both reuse `WallTile.png` as placeholder art with tint/scale/material changes rather than new sprite assets — no image generation available this session (same constraint as Day 4's audio), consolidated into the Priority 3 art shopping list.
- **[6.4]** Moss zone visual implemented as a plain unlit-material `SpriteRenderer` rather than a second `Tilemap` layer — see 6.4 detail above for the reasoning.
- **[Testing]** Two new edit-mode `Unity_RunCommand` testing constraints discovered this session, both logged in the Error log above: `wasPressedThisFrame` never registers outside Play mode (use `SendMessage("<method>")` to bypass input edge-detection when verifying trigger logic directly), and `System.Reflection` is blocked entirely in this sandbox (use `SerializedObject` + `SendMessage` instead).

## SESSION CHECKPOINT (Day 6+, Priority 1 complete, Priority 2 not started)

**Stopping proactively at the Priority 1/2 boundary**, per the master prompt's checkpoint rule — all six Priority 1 features (6.1-6.6) are done and this is a clean tier boundary; Priority 2 (6.7-6.10) is a comparably large chunk of work (a coordinate-system planning doc, a checkpoint/respawn system, mobile safe-area handling, and an Android build pass) and starting it without a fresh budget assessment risks leaving it half-built. Nothing is half-written: 6.1-6.6 are complete, verified (to the extent honestly possible - see 6.6's caveat), and saved.

### What's fully done and verified (don't redo)
- **6.1 Material-based ping sound**, **6.2 CrackedWall**, **6.3 DecoyThrow**, **6.4 MossZone**, **6.5 Mobile touch controls**, **6.6 NavMesh-based monster AI** — all complete, all detailed in their own "detail" subsections above, all with Error log entries for anything noteworthy hit along the way.
- Test/placeholder objects that exist only in `Stage1.unity` (not Stage2/3): `CrackedWall_Test` at `(10,2)`, `MossZone_Test` at `(6,-3)` — verification placeholders, not level design.
- `NavMeshGeometry` (floor + 114 wall-obstacle cubes + `NavMeshSurface`, baked) now exists in **all three stages** and is real/non-degenerate (56 vertices each).
- `EnemySet.prefab`'s `Enemy` child now has `NavMeshAgent` + `Rigidbody2D.bodyType=Kinematic` - propagates to all 6 enemy instances across the 3 stages automatically since they're all connected prefab instances.
- Packages: `com.unity.ai.navigation` (2.0.14) successfully added this session - Package Manager registry is confirmed reachable from this environment.
- **Known-incomplete verification, not a bug**: 6.6's actual `NavMeshAgent` runtime pathing (wander/chase/return actually moving correctly) could not be confirmed headlessly - `isOnNavMesh` never became `true` in edit-mode scripting, and `EditorApplication.isPlaying = true` did not reliably enter Play mode from within a `Unity_RunCommand` script. **A manual in-Editor Play-mode test of 6.6 is the single highest-value manual-verification item outstanding right now** - do this before building further on top of the AI system.

### What's NOT started at all (no files created, no scene changes)
- All of Priority 2: 6.7 (coordinate-merge prep doc), 6.8 (checkpoint-based respawn), 6.9 (mobile safe-area handling), 6.10 (Android build pass)
- All of Priority 3: 6.11 (art integration), 6.12 (monster variety), 6.13 (sound-based minimap)

### Remaining task list (re-derived, for a fresh session with no memory of this conversation)

**6.7 — Coordinate system prep for future single-map merge** (planning/documentation only, no code)
1. Create `Assets/Docs/MapMergeNotes.md` (or a checklist section) documenting each stage's `Tilemap` world-space origin/bounds. Current known values (all 3 stages share the same layout per Day 5): `Tilemap` cellBounds `(-1,-8)` to `(31,9)` (32×17), `CameraFollow` bounds match exactly, `Player` spawns at `(0,0)`, `Goal`/`StageExit` at `(27,0)`.
2. Propose an offset convention for a future merge (e.g. Stage N offset by `(N*200, 0)`) so scenes could later be merged without repositioning tilemaps, just re-parenting. Don't actually merge anything.

**6.8 — Checkpoint-based death/respawn**
1. `Assets/Scripts/CheckpointZone.cs`: trigger volume, on Player entry updates a stored last-checkpoint position - either a field on `HybridPlayerController` (simplest, matches this project's existing pattern of keeping player state on the controller itself, e.g. `spawnPosition`) or a small new singleton (only if multiple systems will need to read it - probably not needed, prefer the simpler option and log the choice).
2. Modify `HybridPlayerController.Die()`/`DieRoutine()`: respawn at the last checkpoint instead of the fixed `spawnPosition` captured in `Start()`. Keep `spawnPosition` as the initial/default checkpoint (i.e. "no checkpoint touched yet" = stage start, not a null/error state).
3. Place 1-2 `CheckpointZone` instances per stage at logical rest points - given Stage1-3 currently share one simple layout (open room + 2 zigzag partitions + the `CrackedWall_Test`/`MossZone_Test` placeholders from 6.2/6.4), a reasonable placement is just past the first partition and again just before the Goal. Replicate to Stage2/3 the same way 6.3/6.5's per-scene UI wiring was done (open each scene, add, wire, save).
4. Verify: die after passing a checkpoint, confirm respawn at the checkpoint not stage start - same `SendMessage`/direct-call verification pattern used throughout 6.1-6.6.

**6.9 — Mobile resolution/safe-area handling**
1. Confirm each stage's `Canvas`/`CanvasScaler` is `Scale With Screen Size` with a mobile-appropriate reference resolution (6.5 already used `1920x1080` for `TitleScreen`/`EndScreen` in Day 5 - check what Stage1/2/3's Canvas currently uses and align if needed).
2. `Assets/Scripts/SafeAreaFitter.cs`: reads `Screen.safeArea`, applies it to a `RectTransform`'s anchors. Apply to the mobile UI panels added in 6.5 (`JoystickBackground`, `TouchAimZone`, `DecoyButton`) plus the existing `CooldownIndicator`/`DecoyCooldownIndicator`.
3. Verify: `Screen.safeArea` in the Editor typically equals the full screen (no notch to simulate) - expect this to be another "can't fully verify headlessly, needs a real device or at least Editor Device Simulator" item; log honestly rather than forcing a pass.

**6.10 — Android build + device test pass**
1. Check `EditorUserBuildSettings.activeBuildTarget` / try `EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android)` via `Unity_RunCommand` - if the Android module isn't installed in this Unity Editor instance, this will fail cleanly and should be logged as a manual step (Unity Hub → install Android Build Support) rather than retried.
2. If the platform switch succeeds: set package name (`PlayerSettings.applicationIdentifier`), minimum API level, orientation (check the design doc - 6.5's control scheme, joystick bottom-left + aim zone right half, strongly implies **landscape**, but this wasn't explicitly stated anywhere seen so far - confirm or default to landscape and log the assumption).
3. Attempt an actual build (`BuildPipeline.BuildPlayer`) only if the above succeeds; otherwise log the exact manual steps needed.
4. This is very likely to end up **mostly a manual checklist** rather than something this MCP session can fully execute - no Android device/emulator is available in this environment either, so even a successful build can't be installed/run here. Be upfront about that rather than overclaiming.

**Priority 3 (6.11-6.13)**: full specs are in the original Day 6+ master prompt (preserved in this conversation's history - not reproduced here since Priority 2 should come first). Notably 6.12 has an explicit **design-open question flagged by the master prompt itself** ("confirm with me whether audio-type monsters should still be killable/avoidable the same way... before fully implementing") - when reaching 6.12, actually stop and ask rather than assuming, per the master prompt's own carve-out from the "resolve ambiguity yourself" rule.

### Open error-log items
None outstanding for 6.1-6.6 - every error/exception/limitation hit this session (6 total: 2 testing-harness quirks in 6.3, 1 MCP-sandbox constraint in 6.3, 1 benign assertion pattern in 6.4, and 2 self-caught-and-fixed NavMesh baking bugs plus 1 environment limitation in 6.6) is documented above with root cause and resolution/workaround.

### Manual fallback items accumulated so far (Day 6+)
- 5 empty `AudioManager` clip fields: `normalWallClip`/`crackedWallClip`/`monsterClip` (6.1), `wallBreak` (6.2), `decoyLand` (6.3) - on top of Day 4's original 5, now 10 total empty clip slots project-wide.
- Decoy object and moss zone both use placeholder art (tinted/scaled `WallTile.png`) pending real sprites - added to the Priority 3 (6.11) art shopping list, alongside whatever 6.11 itself identifies.
- No real device/Unity Remote touch test for 6.5's joystick/drag-aim.
- **No manual in-Editor Play-mode test yet for 6.6's `NavMeshAgent` pathing - highest-priority manual follow-up.**

## Day 8 — UI Navigation, Enemy Wander, Proximity Reveal
- [x] 8.1 Restart button (in-stage reset)
- [x] 8.2 Back button (return to Stage Select)
- [x] 8.3 Enemy Wander mode enhancement
- [x] 8.4 Proximity-based dim reveal (ambient glow) + ping full reveal (tiered)

All four complete: scripts written, pause UI built and wired into all 5 stage
scenes, console clean throughout, and the `ProgressManager` cross-reload
survival assumption confirmed live in real Play mode (not just structurally
argued - see the updated 8.1/8.2 detail below). 8.3/8.4's own Play-mode
walkthrough (watching an enemy wander/idle-look/reveal for 30-60s) was not
done - see "Not independently re-verified" below, consistent with this
project's standing pattern for anything needing a live, human-timed
observation rather than a scripted check.

### 8.1/8.2 detail — PauseMenu
- **`Assets/Scripts/PauseMenu.cs`** (new): shared pause menu per the master
  prompt's default. Toggled by a persistent on-screen pause button (mouse or
  touch) and, for desktop, Escape (`Keyboard.current.escapeKey`) - this
  project already supports both desktop and mobile input side by side
  everywhere else (`HybridPlayerController`), so PauseMenu follows suit
  rather than picking one scheme.
- `Pause()`: `Time.timeScale=0`, shows `pausePanel`, and additionally calls
  `player.SetControlsEnabled(false)` - `Time.timeScale=0` alone does not stop
  `Update()`-driven input reads (ping fire, decoy throw both read
  `Keyboard`/touch state in `Update`, not `FixedUpdate`), so without this a
  paused player could still fire through the menu. Remembers the pre-pause
  `ControlsEnabled` value and restores exactly that on `Resume()` (not an
  unconditional re-enable), so this composes correctly if pause is ever
  reachable while `Die()`/`StageExit` have already disabled controls for
  their own reasons.
- `Restart()`: resets `Time.timeScale=1` then
  `SceneManager.LoadScene(SceneManager.GetActiveScene().name)` - reloading
  recreates every per-scene object from scratch (enemies, cracked walls,
  player position) with no manual reset code.
- `BackToStageSelect()`: resets `Time.timeScale=1` then loads `"StageSelect"`.
- **`ProgressManager` survival - confirmed live in real Play mode**, per the
  master prompt's explicit "verify this assumption holds, don't just
  assume." First attempt (via `EditorSceneManager.OpenScene`, not Play mode)
  correctly failed with `ProgressManager.Instance == null` - `Awake()` never
  runs for edit-mode-opened scene objects (same class of limitation logged
  repeatedly since Day 7.2/7.3), so the `DontDestroyOnLoad` singleton simply
  doesn't exist outside real Play mode. Redone properly: opened
  `TitleScreen.unity`, set `EditorApplication.isPlaying = true` (confirmed
  actually entered Play mode - `GameManager.Instance`/`ProgressManager.Instance`
  both non-null afterward), then via `Unity_RunCommand`:
  `ProgressManager.Instance.MarkCollectibleFound("test_day8_verify")` →
  `SceneManager.LoadScene("Stage1")` → re-checked `IsCollectibleFound` →
  `True` → reloaded the active scene again (simulating `PauseMenu.Restart()`)
  → re-checked again → still `True`. Confirms the assumption across two real
  scene loads, not just one. (Minor, unexplained aside: the active scene
  read back as `StageSelect` rather than `Stage1` immediately after the
  first `LoadScene("Stage1")` call within that same command - most likely a
  `TitleScreenController`/`GoToStageSelect` transition already in flight
  from residual input state in this long-running Play session, not a
  `ProgressManager` issue; the persistence result itself was unaffected
  either way.) Test id cleaned up from `PlayerPrefs["FoundCollectibles"]`
  before exiting Play mode (confirmed real save data - 12 legitimate found
  collectibles - was otherwise undisturbed); confirmed
  `EditorApplication.isPlaying=False`/`isPaused=False` after exiting.

### 8.4 revision (user-requested, after initial build): fixed alpha instead of tiered/proximity-computed
User feedback while testing: the ping-reveal alpha was being dynamically
computed every frame (via the Hidden/Dim/Revealed tier composition) rather
than being a fixed value, and the default state should be Dim, not Hidden.

- **`ShadowEnemy.cs` simplified**: removed the whole Tier-1 proximity system
  (`ambientGlowRadius`/`ambientGlowRadiusFallback`, the player-`Light2D`
  lookup, `dimActive`/`TickDimReveal`/`DimCheckInterval`, and the
  per-frame `ApplyVisibility()` recomputation). `dimAlpha=0.35` (same value,
  field renamed from `ambientDimAlpha`) is now the enemy's **constant
  baseline** - always visible at that dim level, no longer gated by distance
  to the player's ambient glow.
- **`Reveal(duration)`** now sets a fixed `SetAlpha(1f)` once, then a
  coroutine (`DimAfter`, replacing the old per-frame tier check) sets a
  fixed `SetAlpha(dimAlpha)` once after `duration` - alpha is set exactly
  twice per ping-reveal, never recomputed on a tick.
- **`using UnityEngine.Rendering.Universal;`** removed (no longer needed -
  `Light2D` isn't referenced anymore). 8.3's proximity-hearing tick
  (`TickProximityHearing`, unrelated to visibility) is untouched.
- The 8.4 "balance check" note logged earlier (dim-reveal radius vs.
  hearing-boost radius) is now moot - dim is no longer distance-based.
- **Verified**: compiles clean (post-refresh type-lookup); live-checked on
  Stage3's real `EnemySet` instance - `dimAlpha` correctly still reads
  `0.35` despite the field rename (its default initializer matches the old
  serialized value, so no behavior regression from the rename itself).
- **Not re-verified in Play mode**: same standing gap as the rest of 8.4 -
  code-reviewed and field-value-confirmed, not watched live.

### 8.1/8.2 scene wiring (all 5 stages)
Built once on `Stage1.unity`, verified, then replicated identically to
`Stage2-5.unity` (same "open, add, wire, save" pattern as Day 7.3/6.2-style
per-scene UI work). Per stage, added to the existing `Canvas`:
- **`PauseButton`**: top-right corner (`anchorMin=anchorMax=pivot=(1,1)`,
  `anchoredPosition=(-20,-20)`, `60x60`), dark semi-transparent square
  (`RGBA(0.15,0.15,0.15,0.85)`) with a `"II"` TMP label - no dedicated pause
  icon sprite exists in this project's art yet, so plain text was used
  rather than reusing an unrelated icon (`DecoyButton`/`PingButton`'s
  `ToggleOff_Bright` sprite doesn't fit). Placed top-right specifically to
  avoid the existing bottom-right `DecoyButton`/`PingButton`/cooldown-indicator
  cluster.
- **`PausePanel`**: full-screen dim background (`anchorMin=(0,0)`,
  `anchorMax=(1,1)`, `RGBA(0,0,0,0.75)`), starts `SetActive(false)`.
  Contains three centered buttons (`260x60`, light gray `RGBA(0.9,0.9,0.9,0.95)`,
  black TMP labels), stacked vertically 70px apart: `ResumeButton`,
  `RestartButton`, `BackToSelectButton`. These are this project's first
  text-labeled gameplay buttons (every existing button - `DecoyButton`,
  `PingButton`, Stage Select's stage buttons - is icon/color-only); flagging
  as a small, deliberate style departure rather than matching precedent that
  didn't fit ("Resume"/"Restart"/"Back to Select" don't have obvious icons).
- **Wiring**: a `PauseMenu` component was added directly to each scene's
  `Canvas` GameObject (not a separate dedicated object - Canvas already acts
  as this project's per-scene UI root, e.g. hosting `PingTooltip`/`DarknessIntroText`
  directly), with `pausePanel` assigned via `SerializedObject`. Each button's
  `onClick` was wired via `UnityEditor.Events.UnityEventTools.AddPersistentListener`
  (so it's a real persistent listener, not a runtime-only subscription) to
  `PauseMenu.TogglePause`/`Resume`/`Restart`/`BackToStageSelect` respectively.
  Verified after every save: `SerializedObject`-read `pausePanel` reference
  correct, all 4 buttons report exactly 1 persistent listener each with the
  correct method name, `PausePanel.activeSelf=False` by default. Re-verified
  identically across all 5 stages in one final pass. Console clean after
  every scene save (checked via `Unity_GetConsoleLogs`, filtered against the
  benign `[Command Cache]` noise already documented in this project's
  tooling notes).

### 8.3/8.4 detail — ShadowEnemy.cs rewrite
Both features land in the same file since both extend the Wander state /
sprite-visibility logic. Tuning values (all `[SerializeField]`, overridable
per-enemy-instance in the Inspector like this project's other tunables):

- **Wander waypoints**: `PickWanderPoint()` now uses
  `NavMesh.SamplePosition` to validate/snap a random point in `wanderRadius`
  (previously an unvalidated `Random.insideUnitCircle` target), and rejects a
  candidate within `wanderPointSeparationFactor * wanderRadius` of any of the
  last `WanderHistorySize=3` points actually visited (up to 5 resample
  attempts, falls back to spawnPosition if all 5 fail). `wanderPointSeparationFactor=0.4`
  - chosen as a fraction of `wanderRadius` rather than an absolute distance
  because this project's per-stage `wanderRadius` already varies 2f-6f
  (Stage4 vs Stage5); an absolute separation would have made Stage4's tight
  radius nearly unsatisfiable.
- **Proximity hearing boost**: `proximityHearingMultiplier=1.4` (midpoint of
  the requested 1.3-1.5x range) applied when the player is within
  `proximityHearingTriggerFactor=2.0 * hearingRadius`, ticked every
  `HearingCheckInterval=0.3s`, and **only while Wandering** (resets to 1x on
  entering any other state) - this is specifically a passive-wander-tension
  mechanic, not a change to actual noise-hearing (ping/decoy `noiseRadius`
  still caps it via `Mathf.Min`, unchanged).
- **Idle-look sub-state**: `idleLookChance=0.4` (40%) per wander-cycle,
  `idleLookMinDuration=0.5`/`idleLookMaxDuration=1.5`. Implemented as a
  coroutine (`IdleLookThenMove`) that sets `agent.isStopped=true`, flips
  `SpriteRenderer.flipX` as a placeholder "turned to look" cue (no
  directional sprite system exists in this project yet), waits the random
  duration, then resumes toward the already-picked wander target.
  **Interruption handled explicitly**: `EnterChasePlayer`/`EnterChaseDecoy`/
  `EnterReturnToStart` all call a new `CancelIdleLook()` first (stops the
  coroutine, clears `isStopped`) - without this, a ping/decoy noise arriving
  mid-idle-look would let the stale coroutine's `MoveTo` call fire later and
  silently override the chase destination just set.
- **Tiered visibility (8.4)**: rewrote `Reveal()`/the old `HideAfter`
  coroutine into a priority-composed system per the master prompt's spec -
  `ApplyVisibility()` computes `alpha` fresh each frame as
  `Hidden(0) < Dim(ambientDimAlpha) < Revealed(1, time-limited via
  revealedUntil)`, so the two tiers never fight. `ambientDimAlpha=0.35`
  (middle of the requested 0.3-0.4 range). Switched `SpriteRenderer` from
  toggling `.enabled` to always-`enabled=true` with alpha-driven visibility -
  this project's own Day 2.5 Patch lesson (`enabled=false` silently drops a
  GameObject from URP 2D's render list, which also affects any `Light2D` on
  the same object) doesn't directly apply here (`ShadowEnemy` has no
  per-enemy light), but alpha-based hiding is what the two-tier composition
  needs anyway, and it's the established project convention for this exact
  situation.
- **Ambient glow radius source**: read live from the Player's own `Light2D`
  (`player.GetComponent<Light2D>().pointLightOuterRadius`) at `Start()`,
  falling back to a serialized `ambientGlowRadiusFallback=0.5` if the Player
  or its Light2D can't be found - per the master prompt's explicit
  "read this value from HybridPlayerController's Light2D component" option.
  **Confirmed live via `Unity_RunCommand`**: `Player.prefab`'s root `Light2D`
  reads `outerRadius=0.5, intensity=0.3` (matches the Day 2 design value used
  as the fallback). Also found, and deliberately left alone as out of scope:
  a **second, undocumented `Light2D`** on a child object of `Player.prefab`
  (`outerRadius=3, intensity=20`, name has a leading space - `" Light 2D"`)
  that doesn't match any documented feature. Not used for anything in Day 8;
  flagging for awareness only.
- **Balance check (8.4's own request)**: at default per-stage values, the
  dim tier (`ambientGlowRadius≈0.5`) triggers well inside the boosted
  hearing-proximity tier (`2x hearingRadius` = 16 units at the default
  `hearingRadius=8`, down to 4-6 units on Stage4/5's tightened values) - the
  player can be sensed passively long before they're close enough to see
  anything dim. This is **the intended relationship**, not a numbers
  mismatch: hearing-boost is meant to create tension at a distance, dim
  reveal is meant to reward genuinely close, careful approach. No balance
  concern to flag.
- **Tick intervals**: `HearingCheckInterval=0.3s`, `DimCheckInterval=0.15s` -
  both per-enemy timers (not global), logged per the master prompt's request;
  picked from the low end of the suggested ranges since this project's
  current max enemy count per stage is 3 (Stage5), where per-frame cost
  isn't yet a real concern, but ticking is still cheap insurance.

### Error log
#### [Tooling] Error: `Unity_RunCommand` scripts failed with `CS8805: Program using top-level statements must be an executable`
- **Where**: every `Unity_RunCommand` call this session, initially.
- **What happened**: even a trivial one-line `Debug.Log(...)` script failed
  compilation with `CS8805`.
- **Root cause**: this session's `Unity_RunCommand` tool requires the
  "golden template" (`internal class CommandScript : IRunCommand` with
  `Execute(ExecutionResult result)`), not bare top-level statements - the
  tool auto-appends a wrapping `namespace` block that's invalid alongside
  top-level statements. This differs from how prior sessions' checklist
  entries describe using this class of tool; the exact required shape
  wasn't obvious from the tool description alone until reading its
  full schema.
- **Fix applied**: used the golden template for every command from then on.
- **Prevented by**: always use `internal class CommandScript : IRunCommand`
  with `void Execute(ExecutionResult result)` for any future
  `Unity_RunCommand` call in this project - never bare top-level statements.

#### [Tooling] Error: `'Image' is a namespace but is used like a type` (CS0118)
- **Where**: a `Unity_RunCommand` inspection script referencing `Image`
  bare (for `GetComponent<Image>()`).
- **What happened**: compile error, same class of issue logged repeatedly
  since Day 3/Day 5/post-6.6.
- **Root cause**: unchanged from those prior entries - a bare `Image` in a
  script that also creates/references GameObjects resolves to the
  `UnityEngine.UI` namespace-in-context ambiguity, not the type, in this
  project's specific script-globals setup.
- **Fix applied**: fully-qualified `UnityEngine.UI.Image`.
- **Prevented by**: (unchanged from prior entries) always fully-qualify
  `UnityEngine.UI.Image` in any `Unity_RunCommand`/`Unity_RunCommand`-style
  script in this project.

#### [Scene wiring] Finding (not a Day 8 bug): `Assets/Scenes/Stage1.unity` currently has every real gameplay root object disabled
- **Where**: `Assets/Scenes/Stage1.unity`, discovered while looking up the
  Canvas hierarchy to build the pause-menu UI (not caused by anything in
  this Day 8 session - no scene file was opened with intent to modify before
  this was found, and it was found on the *first* open).
- **What happened**: `EditorSceneManager.OpenScene("Assets/Scenes/Stage1.unity")`
  loads correctly (`isLoaded=True`, `rootCount=14`), but every one of the 13
  real root GameObjects (`Main Camera`, `Player`, `Canvas`, `EventSystem`,
  `Goal`, `AudioManager`, `NavMeshGeometry`, both `Collectible_stage1_*`,
  `DarknessIntro`, `BackgroundGrid`, `MapGrid`) reads `activeSelf=False`.
  The only **active** root object is one named
  `(untitled backup)-1786090142 (1)_0` (`SpriteRenderer`+`Animator`, no
  other project script). `git diff --stat` confirms `Stage1.unity` is
  currently modified-but-uncommitted on disk (494500 -> 500912 bytes,
  reported as a binary diff by git) - this predates this Day 8 session
  entirely; nothing in this session wrote to that file before the finding.
- **Root cause (inferred, not confirmed)**: the object's name matches
  Unity's own auto-generated naming for a scene recovered from its
  crash-recovery/backup mechanism. The most likely explanation is that an
  Editor crash-recovery prompt was accepted (in this session's connected
  Editor instance, or a prior one) and the recovered content got saved over
  `Stage1.unity`, disabling the real scene content in the process. This is
  a guess, not a confirmed root cause - I did not attempt to dig further
  since fixing/investigating scene corruption is outside this prompt's
  scope (8.1-8.4 only) and touches a file state the user hasn't seen yet.
- **Fix applied**: **none by this session - deliberately left as-is at the
  time.** Re-enabling the 13 disabled objects would have been simple, but
  there was no way to confirm that was the correct fix (vs. discarding
  whatever the active backup object represented) without the user's input,
  and it wasn't something this prompt asked to be touched. Confirmed at the
  time this was isolated to `Stage1.unity` - `git status` showed every other
  scene file (`Stage2-5`, `TitleScreen`, `StageSelect`, `EndScreen`) clean.
- **Resolution**: resolved outside this session, by the user, between the
  finding being reported and scene-wiring work resuming - re-checked live
  and `Stage1.unity` now has all 13 real objects active again (rootCount
  14→13, the backup object is gone). Scene wiring proceeded normally once
  this was confirmed.
- **Prevented by**: n/a - was a scene-file issue outside this session's
  code, not a code-level bug.

## Notes / decisions
- **[8.1]** Pause menu (not a persistent corner Restart button) chosen per
  the master prompt's own default, since it also hosts 8.2's Back button.
- **[8.1]** Input scheme: pause button (mouse+touch) + Escape (desktop),
  matching this project's existing side-by-side (not platform-exclusive)
  input support.
- **[8.2]** No exit-confirmation dialog, per the master prompt's explicit
  instruction to skip it for this pass.
- **[8.3]** See tuning values in the 8.3/8.4 detail section above.
- **[8.4]** See tuning values and the balance-check conclusion in the
  8.3/8.4 detail section above.
- **[8.4]** `SpriteRenderer` visibility switched from `.enabled` toggling to
  always-enabled + alpha, to support tier composition - see detail above.

## Day 8 status: complete
Scripts written, pause UI built and wired into all 5 stage scenes,
`ProgressManager` survival confirmed live in real Play mode, console clean
throughout. See the detail sections above for exactly what was verified vs.
what still needs a manual pass.

### Not independently re-verified (manual follow-up)
- **8.1/8.2 full interactive pass**: button wiring and the `ProgressManager`
  survival case were verified (persistent listeners present with correct
  method names; a real Play-mode reload test passed), but actually clicking
  `PauseButton`/`Resume`/`Restart`/`Back to Select` with a mouse/touch and
  watching the panel show/hide was not done - inherits this project's
  standing "can't drive a live UI click through headless MCP" limitation
  (same class of gap logged for `DecoyButton`/`PingButton`/Stage Select
  buttons back in Day 7).
- **8.3 Wander variety/idle-look, live-observed**: the tuning values and
  interruption-safety logic (`CancelIdleLook`) were verified by reading the
  component's serialized fields and by code inspection, not by watching an
  enemy for 30-60s in real Play mode as the master prompt's Verify step
  asked. **Recommend a manual in-Editor Play-mode watch of a Stage3/4/5
  enemy** - same class of gap as 6.6's still-outstanding NavMeshAgent
  Play-mode check.
- **8.4 Tiered reveal, live-observed**: same gap - the alpha-composition
  logic was verified by code review and by confirming the exact tuning
  values live on a real `ShadowEnemy` instance (Stage3), but walking up to
  an enemy and firing a ping to see Dim→Revealed→Dim/Hidden happen on
  screen was not done.
