# Prototype Build — Progress

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

## Notes / decisions
- **[Day 3]** `cooldownIndicator.fillAmount` convention: chose "starts at 0 the instant you fire, fills up to 1 when ready again" (not the reverse). Not specified in the prompt; this is the more common convention for ability-cooldown UI.
- **[Day 3]** Renamed `fireCooldown` (0.3s, from Day 2.5) to `pingCooldown` (1.5s) to match Day 3's exact field name/value — a deliberate, large increase from the prior value, since Day 3's spec gave an explicit new number.
- **[Day 2.5 Patch]** Kept `sr.color.a = 0` instead of the spec's literal `SpriteRenderer.enabled = false` for hiding the ping — that exact line was the diagnosed cause of the flash-invisible bug; reverting to it would reintroduce the bug.
- **[Day 2.5 Patch]** Used `Keyboard.current` (Input System) instead of legacy `Input.GetAxisRaw(...)` in aiming code, consistent with the Day 1 fix (this project's Active Input Handling is Input System–only).
- **[Day 2.5 Patch]** Implemented `CameraFollow.cs`'s "map narrower than viewport" protection as an auto zoom-in safeguard (shrinks `orthographicSize` so the clamp range can never invert) rather than the spec's literal "center on that axis" branch — functionally supersedes it; the center-branch would be unreachable dead code under this implementation.
- **[Day 2]** Current Stage 1 layout is a large open room with two zigzag partitions, not a "branching maze with dead ends" as Day 2's original spec described — this was the level actually built and play-tested across this project's history; not being redone to match the literal "maze" description since it already serves the same purpose (an obstacle route from spawn to Goal) and reworking it isn't required by anything in Day 3-5.
- **[Testing]** This project's headless MCP Play-mode sessions do not reliably auto-advance `Time`/the frame loop (`Time.time` observed frozen despite `isPlaying=true`). Established workaround, used throughout: drive lifecycle methods manually via `GameObject.SendMessage("Start"/"FixedUpdate"/"LateUpdate")`, advance physics deterministically via `Physics2D.Simulate()` with `Physics2D.simulationMode = SimulationMode2D.Script`, and simulate real keyboard input via `InputSystem.QueueStateEvent`/`StateEvent.From` + `InputSystem.Update()` rather than relying on ambient frame ticking.
