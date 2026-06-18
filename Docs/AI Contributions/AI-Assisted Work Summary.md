# AI-Assisted Work Summary (ChatGPT & Cursor)

In the whole development cycle of the third-person action tower defense game, I adopted AI as a development assistant. AI participated in the design, coding, debugging and documentation work of about half of the functional modules, effectively lowering the difficulty of development and improving work efficiency.

**Important note on division of work:** Core gameplay architecture — including the event flag system design, tower defense wave logic, monster AI state machine, minimap structure, dual health system, and most scene integration — was designed and implemented by me. AI mainly provided draft code, debugging help, documentation support, and secondary-feature assistance. All AI output was reviewed, tested in Unity, and modified before use.

The specific assisted contents are as follows:

---

## 1. Input Lock & Pause Menu

- Provided implementation ideas for the input lock mechanism (`PlayerInputLock`, `PlayerInputLockController`), helped sort out the linkage logic between input status and game states (dialogue, scene loading, cutscenes, NPC placement mode), and optimized mouse display and cursor lock switching rules.
- Assisted with the ESC pause menu flow in `ExitMenuController`: toggling `Time.timeScale`, showing/hiding the menu panel, and restoring gameplay state on close.
- Helped connect input lock with other systems so that dialogue, loading screens, and the pause menu do not conflict with player movement or combat.

---

## 2. Asynchronous Scene Loading & Transition System

- Assisted in implementing `SceneLoadRunner` asynchronous loading logic based on `SceneManager.LoadSceneAsync`.
- Helped optimize screen fade-in and fade-out effects; solved the problem that transition animation was affected by game time scale via `unscaledDeltaTime`.
- Sorted out the full loading process: loading tip display, minimum loading duration control, input lock linkage, and post-loading state recovery.
- Completed auxiliary code for cross-scene spawn ID transmission (`PlayerCrossScene`, `PlayerSpawnOnLoad`) and player automatic repositioning at spawn points.
- Suggested wiring patterns for `TeleportPortal` and `FlagSceneLoadTrigger` to work with the loading runner without hard scene references.

---

## 3. Dialogue System & Opening Cutscene

- Assisted in defining `DialogueData` data structure and writing JSON file parsing logic (`DialogueJsonParser`) for dialogue content.
- Optimized the line-by-line text display and left-click to continue logic of `DialogueReader`, and improved the experience of dialogue playback.
- Provided technical solutions for opening black curtain transition (`OpeningBlackCurtain`), subtitle rolling (`IntroNarrationLines`), and one-click skip (`IntroSkipButton`).
- Helped with portrait lookup logic in `DialoguePortraitDatabase` and background audio mapping via `BgAudioData`.
- Suggested flag-trigger patterns for `FlagDialogueTrigger` so dialogue completion could notify the event system without tight coupling.

---

## 4. Save / Load System (Partial Assistance)

- Suggested JSON save file layout and debounce timing for `GameSaveSystem` so rapid flag changes do not write to disk too frequently.
- Assisted with auto-save trigger hooks (flag added/removed, skill loadout change, scene change) and the Continue flow on `MainMenuController`.
- Helped debug spawn ID restoration after loading a save in a non-menu scene.

*The overall save architecture and integration with `GameEventManager` were my own design; AI helped mainly with serialization details and edge-case fixes.*

---

## 5. UI, Menus & In-Game Feedback (Partial Assistance)

- Assisted in writing basic logic of world-space UI such as monster health bar facing camera (`EnemyUICanvasBillboard`).
- Helped draft scroll-to-bottom behaviour and message line layout for `GameMessageFeed` / `GameMessageLineView`.
- Suggested panel show/hide flow for `LevelVictoryPanel`, `TutorialVideoPanel`, and `FlagTutorialVideoTrigger`.
- Assisted with click-handler components (`UIClickTMP`, `UIClickText`, `UIClickImage`) used by menus and the exit screen.
- Helped standardise fight-level hotkey gating in `FightLevelInputGate` so skill keys 1–4 and E/Q only work during combat phases.
- Uniformed code naming rules and coding specifications across scripts; helped write comments for key functions and modules.

*Monster HP bar animation, quest display logic, and core UI layout in scenes were my own work.*

---


## 6. NPC Placement Mode (Debugging & Integration Help)

- Helped debug raycast placement checks in `NpcPlacementController` (ground probe, obstacle overlap, UI pointer blocking).
- Suggested guards in `CharactorController` and `CameraController` to disable movement, aiming, and camera rotation while placement mode is active.
- Assisted with preview display logic in `CharacterPreviewStage` and zone occupancy checks in `NpcPlacementZone`.

*The placement system concept, zone design, and prefab workflow were my own work.*

---

## 7. Combat & Camera Bug-Fix Sessions (Cursor Agent)

During playtesting, Cursor was used as a debugging assistant. AI did **not** design the core combat or camera systems, but helped diagnose and patch specific bugs:

- **Airborne aiming bug** — aiming while jumping broke camera zoom and attack state; fix involved grounded-only aim rules and `rmbReleasedSinceAir` in `CharactorController` and `CameraController`.
- **Minimap display bug** — icons clipping outside map bounds or missing for late-spawned enemies; fix involved clamp logic and late registration in `MinimapWorldTracker`.
- **Input overlap** — dialogue, placement mode, and combat inputs conflicting; fix involved checking `PlayerInputLock.IsLocked` and `NpcPlacementController.IsActive` in multiple scripts.
- **Game over rollback** — assisted with structuring flag add/remove rules in `DawncoreDefeatController` after Dawncore destruction.


---

## 9. Documentation & Course Deliverables

- Helped draft and structure project documentation: README polish, Development Report sections, control guide text (WASD / mouse / ESC), and script categorisation lists under `Assets/Scripts`.
- Assisted with formatting the Asset Citation table and aligning documentation wording with actual implemented features.
- Used AI to rephrase commit summaries and session plan notes for clarity before submission.

---

## Summary Table

| Area | My role | AI role |
|------|---------|---------|
| Event flag architecture | Designed and implemented core API | Boilerplate triggers, integration suggestions |
| Tower defense / monster AI | Full design and implementation | Bug-fix suggestions during playtesting |
| Player controller & camera | Full design and implementation | Airborne aim / input conflict fixes |
| Minimap | Full architecture and implementation | Debugging late-spawn and clamp issues |
| Scene loading | Designed flow; implemented with AI draft help | Async load code, fade timing, spawn delivery |
| Dialogue & opening | Designed workflow; implemented with AI draft help | JSON parsing, UI playback, skip logic |
| Save system | Designed architecture and integration | JSON layout, debounce, Continue flow help |
| UI / menus | Layout and core logic | Billboard UI, message feed, pause menu drafts |
| NPC placement | System design and scene setup | Raycast debugging and input guards |
| Documentation | Content decisions and accuracy review | Drafting, formatting, categorisation |

All AI-generated or AI-suggested code was reviewed, tested in Unity, and adjusted to fit the project's flag-driven architecture. Final design decisions, scene wiring, and integration responsibility remained with me.
