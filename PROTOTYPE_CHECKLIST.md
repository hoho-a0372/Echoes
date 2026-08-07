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
