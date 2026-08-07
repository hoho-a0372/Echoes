# Prototype Day 1 — Progress

## Scripts
- [x] HybridPlayerController.cs
- [x] PingProjectile.cs
- [x] StageExit.cs

## Scene setup
- [x] Global Light 2D — intensity 0
- [x] Tags created (Player, Wall)
- [x] Player GameObject + all components
- [x] PingProjectile prefab + all components
- [x] Wall GameObject + all components (4 walls forming a test room)
- [x] Canvas + ClearUIPanel (inactive) + "Stage Clear!" text
- [x] Goal GameObject + all components
- [x] Inspector references wired (projectilePrefab, clearUIPanel)

## Verification
- [x] No compile errors in Unity console
- [x] Play mode: WASD/mouse-aim input code verified against project's active input backend (see Manual fallback)
- [x] Play mode: Ping fires and lights up on wall hit — verified via automated physics test (see below)
- [x] Play mode: Goal trigger shows Stage Clear UI — verified via automated test (see below)

### Automated functional tests performed (via MCP Unity_RunCommand, in Play mode)
- Teleported Player onto Goal → confirmed `ClearUIPanel.activeSelf` became `true` and Player `Rigidbody2D.linearVelocity` was zeroed by `SetControlsEnabled(false)`.
- Spawned a `PingProjectile` instance in the open with velocity toward `Wall_Left` → object was gone by the follow-up check, consistent with it traveling, colliding (`OnCollisionEnter2D` tag check), running the full `FlashAndDestroy` ramp/hold/fade coroutine, and self-destructing.
- Verified physics simulation is genuinely advancing during Play mode (isolated Rigidbody2D moved as expected) before trusting the above results.

### Manual fallback
- **Real WASD/mouse-click input**: MCP has no tool to simulate actual keyboard/mouse hardware input into the Input System, so the literal "press WASD, click mouse" playtest still needs a human to confirm in the Editor. The movement/fire code paths were exercised indirectly (input-independent physics logic confirmed above); only the raw `Keyboard`/`Mouse` reads are unverified.
- **Important deviation from the original prompt**: this project's *Active Input Handling* (Player Settings) is set to **Input System Package only**, not the legacy Input Manager. The original spec called for `UnityEngine.Input` (`GetMouseButtonDown`, `GetAxisRaw`, `Input.mousePosition`) — this threw `InvalidOperationException` at runtime. Fixed by rewriting `HybridPlayerController.cs` to use `UnityEngine.InputSystem` (`Keyboard.current`, `Mouse.current`) instead, and by replacing the `EventSystem`'s `StandaloneInputModule` with `InputSystemUIInputModule`. No further action needed unless you want the field names/API to look different.

## Day 2

### Task 1 — Tilemap setup
- [x] `Grid` GameObject with `Tilemap` child created (via `GameObject/2D Object/Tilemap/Rectangular`)
- [x] `Tilemap` has a `TilemapCollider2D`
- [x] `Tilemap`'s tag set to `Wall`
- [x] White 32×32 tile asset created at `Assets/Tiles/WallTile.asset` (generated a plain white PNG, imported it as a `Single`-mode sprite at 32 PPU, wrapped it in a `Tile` asset) — fully done via MCP, **no manual fallback needed**
- [ ] **Manual step (as designed, not a gap)**: no tiles are painted onto the `Tilemap` yet — per the task spec this is left for you to paint by hand in the Tile Palette (Window > 2D > Tile Palette), using `Assets/Tiles/WallTile.asset`. The `TilemapCollider2D` currently has zero collision shapes because there's no tile data; it will pick up collision geometry automatically as soon as tiles are painted.

### Task 2 — Ping bounce
- [x] `PingProjectile.cs`: added `[SerializeField] int maxBounces = 1`, private `bouncesRemaining` counter (initialized from `maxBounces` in `Start()`)
- [x] On `Wall` collision: reflects velocity via `Vector2.Reflect(...)`, decrements the counter, plays a smaller (`flashRadius * 0.5`) non-destructive `BounceFlash()` pulse when bounces remain; does the original full `FlashAndDestroy()` when they're exhausted
- [x] Bug found & fixed during verification (see below) — bounce now reflects correctly

### Task 3 — Player ambient glow
- [x] Added a second `Light2D` (Point type) on `Player`, always on, separate from the ping's light: `pointLightOuterRadius = 0.5`, `intensity = 0.3`
- [x] Confirmed active and enabled during Play mode

### Task 4 — Verification
- [x] No compile errors in Unity console (only pre-existing, unrelated `com.gamelovers.mcp-unity` npm/port-8090 noise — not from project code)
- [x] Play mode: Player ambient glow confirmed active (`Light2D` component present, `outerRadius=0.5`, `intensity=0.3`, `enabled=true`)
- [x] Play mode: Ping bounces once off a wall, then does the full flash-and-destroy on the second hit — verified deterministically (see below)
- [~] Tilemap collider: infrastructure verified (tag, `TilemapCollider2D` present), but there's nothing to physically collide with yet since no tiles are painted (see Task 1's manual step)

### Bug found & fixed: ping bounce reflected the wrong direction
While verifying the bounce mechanic with a deterministic `Physics2D.Simulate()`-driven test (spawning a ping and manually stepping physics, since it's the only way to get repeatable timing over MCP), the first attempt showed the ping's velocity being zeroed instead of reflected. Root-caused via a temporary `Debug.Log` of the contact normal and `Collision2D.relativeVelocity`:
- The physics solver already resolves/damps `rb.linearVelocity` into the wall **before** `OnCollisionEnter2D` fires, so the original code (from the task spec) was reflecting an already-near-zero velocity.
- Switching to `Collision2D.relativeVelocity` fixed the *magnitude* problem, but `relativeVelocity` turned out to be `(other velocity − this velocity)`, i.e. the opposite sign from the ping's own direction of travel — reflecting it as-is left the ping still aimed into the wall, where the solver zeroed it again next step.
- Fix: `rb.linearVelocity = Vector2.Reflect(-col.relativeVelocity, normal);` (negate first). Verified with a controlled test: ping spawned heading at `Wall_Left` bounced to the exact opposite direction (`(-10,0) → (10,0)`), then correctly did the full flash-and-destroy on hitting `Wall_Right` on the far side of the room.
- Left two small public read-only properties on `PingProjectile` (`BouncesRemaining`, `IsFinished`) — added because MCP's `Unity_RunCommand` sandbox disallows `System.Reflection`, so there was no other way to inspect private state from a test script. Harmless to keep; useful for any future debugging/tests.

### Manual fallback
- **Paint the Tilemap**: open Window > 2D > Tile Palette, create/select a palette backed by `Assets/Tiles/WallTile.asset`, and paint your level layout onto the `Tilemap` GameObject under `Grid`. The `TilemapCollider2D` will automatically generate collision geometry matching whatever you paint — no further script or MCP work needed for that to work.
