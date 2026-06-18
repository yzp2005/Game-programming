# Development Report — The Rover and the Village

**Project:** Final Assessment — 3D Tower Defense / Action Game  
**Engine:** Unity 2022.3  
**Author:** Zhanpeng Yang  
**Repository:** https://github.com/yzp2005/Game-programming.git

---

## 1. Design Choices

### Core Concept

The game is titled **The Rover and the Village**. The player controls a wandering mage who arrives at a quiet village, befriends villagers through dialogue, and helps defend the settlement from incoming enemy waves. The design deliberately mixes three genres:

- **Open-world exploration** — walk around a stylized fantasy village, enter buildings, and interact with NPCs.
- **Narrative / quest progression** — story beats are unlocked through conversations and event flags, not linear level menus.
- **Tower defense combat** — enemies follow predefined paths toward a defense core (Dawncore); the player and placed NPC allies fight to protect it.

This combination was chosen to make the project more than a pure combat demo: exploration and dialogue give context to each battle, and the flag system ensures battles only start after the story is ready.

### Player Experience

- **Third-person perspective** for both exploration and combat, keeping camera and controls consistent.
- **Hold right mouse to aim, left click to fire** — aiming is intentional and grounded-only, so combat feels deliberate rather than spam-clicking.
- **NPC ally placement** — before fights, the player can place villagers in defense zones (E key + left click), turning tower defense into an active preparation phase instead of passive turret placement.
- **Skill preparation panel** — players choose abilities into a loadout bar before combat, replacing an earlier plan for a currency-based shop (see Section 5).
- **Fail-forward on defeat** — when the Dawncore is destroyed, the game shows a lose screen and rolls back to a checkpoint based on story flags, rather than forcing a full restart from the main menu.

### Scope Control

The original vertical slice (Session plan) targeted: one village scene, one dialogue, one wave, one ally. The final version expanded to multiple scenes (Suntail Village, North Village), multi-wave levels, save/load, minimap, tutorial video, player guide, and a full opening narration — but deliberately dropped the inventory/shop system to keep integration manageable.

---

## 2. Technical Decisions

### Architecture

| Decision | Rationale |
|----------|-----------|
| **Component-oriented scripts** | Each script has a single responsibility and is wired through the Unity Inspector, reducing hard-coded references between systems. |
| **Event flag system (`GameEventManager`)** | A global `HashSet<string>` with `Set` / `Has` / `Remove` APIs and C# events connects dialogue, level triggers, scene loads, music, and UI without tight coupling. |
| **Global input lock (`PlayerInputLock`)** | One static gate disables movement, aiming, and combat during dialogue, loading, cutscenes, and pause menu — preventing state conflicts. |
| **Single-scene multi-level design** | Multiple wave groups live in one large map; `LevelController` activates them on demand when flags appear, avoiding repeated scene loads for each wave. |
| **Dual health system** | `MonsterHealth` (full death logic, recovery, AI shutdown) and `CoreHealth` (lightweight, UI-synced) are separated because monsters and the defense core have different lifecycle needs. |

### Key Systems

**Combat & AI**
- Monsters follow child-object waypoints via `MonsterPath`, move on the XZ plane for terrain compatibility, and use a simple FSM (idle → move → attack) driven by animation parameters.
- `WaveManager` handles spawn timing, intervals, and coroutine lifecycle; `LevelController` listens for flags and starts/stops waves.
- Player projectiles use raycast-based aiming from screen center; NPC allies use homing projectiles (`NPCHomingProjectile`).

**Narrative**
- Dialogue content is stored in external JSON files and parsed by `DialogueJsonParser` / `DialogueReader`.
- Opening uses a black curtain (`OpeningBlackCurtain`), rolling narration (`IntroNarrationLines`), and skip support (`IntroSkipButton`).

**Persistence**
- `GameSaveSystem` auto-saves flags, skill loadout, and current scene to JSON under `persistentDataPath`.
- Cross-scene player position is handled by `PlayerCrossScene`, `PlayerSpawnPoint`, and `PlayerSpawnOnLoad`.

**Scene flow**
- `SceneLoadRunner` uses `LoadSceneAsync` with fade transitions based on `unscaledDeltaTime` so loading works even when `timeScale` is 0.
- `TeleportPortal` and `FlagSceneLoadTrigger` support in-world and flag-driven scene changes.

**UI**
- Modern UI Pack and TextMeshPro for menus, health bars, quest text, game-over panel, and in-game message feed.
- World-space health bars use `EnemyUICanvasBillboard` to always face the camera.

### Script Organisation

90 C# scripts under `Assets/Scripts`, grouped into folders: `EventSystem`, `Monster`, `Dialogue`, `Save`, `SceneLoading`, `Minimap`, `Skills`, `UI` (including `SkillPrep`), and root-level player/world scripts.

---

## 3. Problems and Limitations

### Integration Challenges

The biggest risk identified at project start — coordinating dialogue, flags, combat, and wave spawning without conflicts — proved accurate. Examples encountered:

- Enemy waves triggering before dialogue finished → solved by gating wave start on specific flags via `LevelController`.
- Player still moving or attacking during dialogue → solved by `PlayerInputLock` and `PlayerInputLockController`.
- Aiming while airborne caused camera zoom and combat state bugs → solved by `rmbReleasedSinceAir` rules in both `CharactorController` and `CameraController`.

### Known Limitations

| Limitation | Detail |
|------------|--------|
| **Single-player only** | No multiplayer or co-op support. |
| **No inventory system** | Listed in early design docs but never implemented; skill loadout replaced the planned shop/currency loop. |
| **Mouse scroll wheel unused** | Camera zoom is tied to right-mouse hold on ground, not scroll wheel. |
| **Heavy asset dependency** | Village environment, characters, VFX, skybox, and UI pack come from the Unity Asset Store — custom work is mainly in C# systems and scene layout. |
| **Manual testing only** | No automated unit or playmode tests; regressions are caught by manual playthroughs. |
| **One developer** | All design, scripting, scene setup, and debugging by a single author; no formal code review process. |
| **Legacy Input Manager** | Uses Unity's classic input (`Input.GetAxis`, `GetMouseButton`) rather than the new Input System package. |

### Performance & Edge Cases

- Coroutine leaks in wave spawning were addressed by full lifecycle management in `WaveManager`.
- Minimap icons for late-spawned enemies required a late-registration path in `MinimapWorldTracker`.
- Save debouncing (0.15 s) prevents excessive disk writes when multiple flags change in quick succession.

---

## 4. Testing and What Changed Because of Testing

Testing was done manually throughout development — play each new feature in the Editor, then do full-scene walkthroughs before Git commits. There is no formal test suite.

### Changes Driven by Testing

**Dialogue & input (commit: `Add dialogue sys & fix bugs`)**
- Found that dialogue and player movement could overlap → added global input lock and mouse cursor visibility rules.

**Minimap (commit: `fix the minimap`)**
- Icons went off-map or failed to appear for late-spawned enemies → added clamp logic and late-registration in the tracker.

**Combat & aiming (ongoing `fix bugs` commits)**
- Aiming in mid-air broke camera zoom and attack animation → added grounded-only aim rules and `rmbReleasedSinceAir` flag.
- NPC placement mode conflicted with combat input → `NpcPlacementController.IsActive` now blocks attack and camera input.

**Feature pivot (commit: `Feature Change and Add the Asset Citation`)**
- Original shop/currency preparation system was hard to balance and integrate → replaced with skill loadout panel (`SkillPrepPanel`, `SkillLoadout`, hotkeys 1–4 gated by `FightLevelInputGate`).

**UX improvements (commits: `Release - Add Player Guide Functionality`, `Implement game over panel and in-game information UI`)**
- Playtesters (and self-testing) found controls unclear → added in-game player guide.
- Core destruction had no clear feedback → added lose UI with flag-based rollback via `DawncoreDefeatController`.
- Added `GameMessageFeed` for in-game announcements and quest hints.

**Save system**
- Testing scene reloads revealed lost flag state → implemented `GameSaveSystem` with auto-save on flag/skill/scene changes and Continue from main menu.

**Path & wave testing (commit: `The monster's path`)**
- Enemies clipped terrain or skipped waypoints → refined `MonsterPath` Gizmo debugging and XZ-only movement in `MonsterChaseAI`.

---

## 5. Reflection on How the Game Developed from Concept to Final Version

### Early Course Work → Final Project

The repository shows a clear progression:

1. **4.24 The Cosmos** — solar system simulator; first Unity 3D exercise (orbits, skybox, audio).
2. **5.9 2D Test** — 2D space shooter assembled from provided assets; learned scene composition and basic combat.
3. **Unity_Event_Driven** — bullet/damage/death event chain demo; directly informed the final game's event-driven flag architecture.
4. **Activity 1 Snapshot (May 19)** — formalised *The Rover and the Village* concept, vertical slice, and risk analysis.
5. **Final game (`my game`)** — full 3D implementation built over multiple sessions.

### Concept → Vertical Slice → Full Game

| Stage | What existed |
|-------|--------------|
| **Initial concept** | Third-person mage, village story, tower defense waves, dialogue + flags as core connectors. |
| **Vertical slice target** | One village, one NPC dialogue, one flag, one enemy wave, basic health bars. |
| **Mid-development** | Dialogue JSON system, event flag manager, teleport, monster paths, wave manager, minimap, async scene loading. |
| **Late development** | Second scene (North Village), NPC placement defense, skill prep, save/load, opening narration, tutorial video triggers, game-over rollback, player guide, quest/message UI. |
| **Dropped / deferred** | Inventory system, in-game shop with currency (planned Sessions 6–7, replaced by skill loadout). |

### Design Evolution

The game started as "tower defense with story" and grew into "story-driven defense with player-placed allies." The event flag system became the spine of the entire project — every major feature (dialogue end, level start, music change, scene load, skill prep unlock) hangs off flags. This was the most important architectural decision and the main reason the expanded scope remained manageable.

---

## 6. Personal Contribution

All custom gameplay code under `Assets/Scripts` (90 scripts) was designed and implemented by **Zhanpeng Yang**. Work falls into these areas:

### Systems Built Independently

1. **Global framework** — `GameEventManager`, `PlayerInputLock`, flag-driven triggers (`FlagDialogueTrigger`, `FlagSceneLoadTrigger`, `FlagObjectActive`, etc.).
2. **Tower defense core** — `MonsterPath`, `MonsterSpawner`, `MonsterChaseAI`, `WaveManager`, `LevelController`, `DawncoreDefeatController`.
3. **Combat & health** — `CharactorController`, `ProjectileDamage`, `MonsterHealth`, `CoreHealth`, `NPCShootController`, `NPCHomingProjectile`.
4. **Player & camera** — `CameraController`, `IndoorHouse`, `PlayerAimInteract`, `magiccirclrcontroller` (MagicCircleFade).
5. **Narrative** — JSON dialogue pipeline, opening cutscene scripts, quest display.
6. **Scene flow** — async loading, fade transitions, teleport portals, cross-scene spawn.
7. **Minimap** — three-layer trackable / tracker / renderer architecture.
8. **Save system** — `GameSaveSystem`, `GameSaveData`, bootstrap.
9. **UI & UX** — main menu, exit/pause menu, skill prep panel, game-over panel, message feed, player guide hooks, fight-level input gate.
10. **NPC defense placement** — `NpcPlacementController`, zones, preview stage, removal interact.

### Scene & Content Work

- Scene layout and lighting in Suntail Village and North Village (using Asset Store environments).
- Dialogue JSON authoring for story beats (Rover meets Lily/Lucy, post-fight monologues, ending).
- Inspector wiring of flag triggers, wave entries, spawn points, and rollback rules.

### Documentation

- README, project snapshot, next-step session plans, asset citation table, contribution documentation, and this development report.

---

## 7. Use of Templates, Assets, Tutorials, or AI Support

### Unity Asset Store Assets

All third-party assets follow the standard Unity Asset Store EULA. Full citation is in `Docs/Asset Citation/Asset Citation.md`. Key packages:

| Category | Asset |
|----------|-------|
| Environment | Lakefell Stylized Fantasy Village, Idyllic Fantasy Village, Suntail Village |
| Characters | Riko, Anime-Style Girl Haruka, City People Free Samples, Battle Royale Hero PBR |
| Skybox | Polyverse Skies Low Poly Skybox Shaders |
| VFX | Toon Projectiles (Hovl Studio) |
| UI | Modern UI Pack |
| Shaders | UnityChan Toon Shader |

Custom scripts, scene logic, dialogue content, and system integration are original work; art and prefabs are primarily from purchased/free store assets.

### Course Materials & Tutorials

- In-class activities (Cosmos, 2D shooter, event-driven demo) provided foundational Unity skills.
- Unity documentation for `CharacterController`, `SceneManager.LoadSceneAsync`, coroutines, and TextMeshPro.
- No single external tutorial was followed end-to-end; systems were designed from course concepts and project requirements.

### AI Support (ChatGPT & Cursor)

Full detail is in `Docs/AI Contributions/AI-Assisted Work Summary.md`. AI assisted roughly **half** of the functional modules — primarily as a draft-code, debugging, and documentation helper, **not** as the author of core gameplay architecture.

**Division of work (no overlap with Section 6):** Systems such as the event flag API, tower defense wave logic, monster AI FSM, minimap architecture, dual health system, and scene integration were designed and implemented by the developer. AI support was concentrated in the areas below.

| Area | AI role | Developer role (kept separate) |
|------|---------|--------------------------------|
| Input lock & pause menu | Cursor/mouse rules, ESC pause flow (`ExitMenuController`) | Decided when/where to lock input across all game states |
| Async scene loading | `SceneLoadRunner` draft, fade with `unscaledDeltaTime`, spawn delivery | Designed loading flow and wired portals / flag triggers |
| Dialogue & opening | JSON parsing, line display, black curtain / skip logic | Story content, dialogue JSON authoring, trigger placement |
| Save / load (partial) | JSON layout, debounce, Continue flow debugging | Save architecture and `GameEventManager` integration |
| UI & feedback (partial) | Billboard HP bar, message feed scroll, click handlers, fight hotkey gate | HP bar animation, quest UI, scene layout |
| Skill prep panel (partial) | List/detail/slot UI flow, Editor helpers | Skill data model and combat hook-up |
| NPC placement (partial) | Raycast/obstacle debugging, input guards during placement mode | System design, zones, prefabs, scene setup |
| Flag trigger components | Repetitive boilerplate (`FlagObjectActive`, `FlagSceneLoadTrigger`, etc.) | Core `GameEventManager` API and flag-driven design |
| Bug-fix sessions (Cursor) | Diagnosed air aim, minimap clamp, input overlap, game-over rollback | Implemented and playtested all fixes |
| Documentation | Development Report, control guide, script lists, Asset Citation formatting | Accuracy review and final content decisions |

All AI-generated or AI-suggested code was reviewed, tested in Unity, and modified before use. Final design decisions, scene wiring, and integration responsibility remained with the developer.

---

*End of report.*
