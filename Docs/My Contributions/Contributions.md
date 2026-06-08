# Game Project Contribution Documentation
Based on self-developed C# scripts under the `Assets/Scripts` directory.
Game Type: Third-person action + Tower defense + Open-world exploration + Dialogue & quest driven

---
## Table of Contents
1. [Overall Project Overview](#1-overall-project-overview)
2. [Global Core Framework Module](#2-global-core-framework-module)
3. [Tower Defense, Monster AI & Wave System](#3-tower-defense-monster-ai--wave-system)
4. [Health & Damage System](#4-health--damage-system)
5. [Minimap Navigation System](#5-minimap-navigation-system)
6. [Scene Loading & Teleport System](#6-scene-loading--teleport-system)
7. [Player, Camera & Interaction System](#7-player-camera--interaction-system)
8. [Dialogue, Quest & Opening Narrative System](#8-dialogue-quest--opening-narrative-system)
9. [UI Auxiliary System](#9-ui-auxiliary-system)
10. [Overall Architecture & Comprehensive Contribution Summary](#10-overall-architecture--comprehensive-contribution-summary)

---
## 1. Overall Project Overview
### 1.1 Gameplay Loop
Open world exploration → NPC dialogue & quests → Activate level flags → Monsters march to attack the core (Dawncore)
→ Player eliminates enemies → Protect core HP → Proceed to next wave / next level

### 1.2 Technical Features
- **Component-oriented design**: Each script has single responsibility, connected via Inspector configuration to reduce hard coding.
- **Event-driven architecture**: Global flag system connects dialogue, interaction and level triggering.
- **Single-scene multi-level design**: Multiple wave groups exist in one large map, activated by level controller on demand.
- **Dual health system**: Separate health logic for monsters and defense core, with independent AI and UI logic.
- **Global input lock**: Disable player movement & attack during dialogue, loading and cutscenes to avoid state conflicts.

### 1.3 Script Scale
Total 38 C# scripts inside `Assets/Scripts`, divided into subfolders and root directory.

---
## 2. Global Core Framework Module
### Included Scripts
GameEventManager.cs, SetGameFlag.cs, PlayerInputLock.cs, PlayerInputLockController.cs

### Core Work
Built the underlying decoupling framework for the whole project, including global event flag system and global input lock mechanism. Serves as the foundation for module communication and state management, linking dialogue, levels, combat and scene loading.

### Technical Implementation & Personal Contribution
1. Designed a global flag system based on `HashSet<string>`. Implemented singleton pattern and `DontDestroyOnLoad` to persist data across scenes. Provided standard `Set/Has` APIs and event callbacks to realize decoupled logic between dialogue, levels and quests without hard references.
2. Developed `SetGameFlag` as a universal component compatible with Unity native event system. Supports visual configuration in Editor to lower maintenance cost.
3. Created a static global input lock system to uniformly control player input. Lock movement, attack and interaction during dialogue, scene loading and cutscenes. Synchronously manage mouse visibility and cursor lock status for different game states.
4. Wrote auxiliary controller for input lock to support quick state configuration in Unity Editor.

---
## 3. Tower Defense, Monster AI & Wave System
### Included Scripts
MonsterChaseAI.cs, MonsterPath.cs, MonsterSpawner.cs, WaveManager.cs, LevelController.cs

### Core Work
Implemented a complete single-scene multi-level tower defense system, including monster path definition, dynamic spawning, finite-state monster AI, wave scheduling and multi-level management. This is the core gameplay module of the project.

### Technical Implementation & Personal Contribution
1. **Monster Path System**: Auto collect child objects as waypoints. Define target core and attack radius. Use `Gizmos` to draw paths and ranges for visual debugging. Decouple path logic from spawning logic.
2. **Monster Spawner**: Instantiate monster prefabs dynamically. Inject path data and minimap tracking components at runtime. Expose standard spawn APIs for wave system calling.
3. **Monster AI**: Implemented finite state machine driven by animation parameters, including idle, move and attack states. Realize XZ-plane movement to adapt to uneven terrain. Automatically locate attack target, with double damage judgment via attack cooldown and animation events. Disable AI automatically after monster death for performance optimization.
4. **Wave Manager**: Configurable wave data structure supports pre-wave delay, multiple spawn points, wave interval and loop mode. Fully manage coroutine lifecycle to prevent leakage.
5. **Level Controller**: Core implementation of single-scene multi-level architecture. Deeply integrated with global flag system to form chain workflow: dialogue triggers levels, and levels activate subsequent stages. Add protection logic to prevent repeated activation. Provide complete APIs for level start, stop, restart and status query.

---
## 4. Health & Damage System
### Included Scripts
MonsterHealth.cs, CoreHealth.cs, ProjectileDamage.cs

### Core Work
Built dual independent health systems for monsters and defense core. Realize projectile damage detection, HP change and death logic to support the whole combat loop.

### Technical Implementation & Personal Contribution
1. Separated two sets of health components for different roles. Monster health supports delayed HP recovery after being hit, death animation, AI deactivation, collider shutdown and delayed destruction. Core health keeps lightweight logic for HP deduction and UI synchronization only.
2. Designed multiple events for health components (HP changed, damaged, dead) for UI and AI subscription, separating data and presentation logic.
3. Developed projectile damage component attached to player attack prefabs. Apply damage exclusively to monsters via collision detection. Search target components through transform hierarchy to improve reusability.

---
## 5. Minimap Navigation System
### Included Scripts
MinimapController.cs, MinimapWorldTracker.cs, MinimapTrackable.cs

### Core Work
Developed a full 3D-world to 2D-minimap system. Real-time display positions of player, enemies and key objectives. Support hotkey toggle and automatic hiding under specific game states.

### Technical Implementation & Personal Contribution
1. Adopted three-layer decoupled architecture: trackable tag, global position collector and minimap renderer. Clear module division.
2. Global tracker uses singleton pattern to collect world coordinates periodically. Handle late registration for delayed activated objects to ensure normal display.
3. Support three map boundary calculation modes. Complete coordinate normalization and conversion from world space to UI space. Use clamp logic to keep icons inside map bounds.
4. Realize hotkey show/hide function. Connect with global input lock to auto hide minimap during dialogue and loading. Classify icons for enemies, allies and objectives with different colors, and dynamically generate icon objects.

---
## 6. Scene Loading & Teleport System
### Included Scripts
SceneLoadRunner.cs, SceneTransition.cs, PlayerSpawnPoint.cs, PlayerSpawnOnLoad.cs, TeleportPortal.cs

### Core Work
Implemented asynchronous scene loading, fade transition effect and cross-scene player position delivery. Include portal trigger, spawn point matching and loading state control for smooth open-world scene switching.

### Technical Implementation & Personal Contribution
1. Scene loader uses singleton and `DontDestroyOnLoad`. Based on `SceneManager.LoadSceneAsync` to implement async loading. Apply fade in/out with `unscaledDeltaTime` to avoid impact from game time scale.
2. Full loading process control: lock player input during loading, show loading tips and set minimum display duration to improve user experience. Unlock input and hide transition UI after loading completes.
3. Custom static class to deliver spawn point ID across scenes and fix player position offset after scene switch.
4. Realize spawn point marking and auto player reposition. Portal component supports visual configuration of target scene and spawn ID for one-click teleport.

---
## 7. Player, Camera & Interaction System
### Included Scripts
CharactorController.cs, CameraController.cs, IndoorHouse.cs, PlayerAimInteract.cs, InteractPrompt.cs, magiccirclrcontroller.cs

### Core Work
Realize third-person character control, camera follow, indoor camera transition, object interaction (doors, barriers), NPC dialogue trigger and attack visual effect control. Main player operation module.

### Technical Implementation & Personal Contribution
1. **Player Controller**: Use `CharacterController` to implement WASD movement, jump and gravity. Separate ground and air status. Design exclusive aiming rules to fix abnormal aiming state. Implement stationary aiming and projectile firing logic, compatible with global input lock.
2. **Third-person Camera**: Smooth follow, mouse rotation and pitch angle limit. Support indoor camera offset and smooth transition when entering / leaving indoor areas.
3. **Scene Interaction**: Use screen center raycast to realize door control and area prompt. Classify interactive objects by layer and set interaction distance limit. NPC trigger shows interaction tips and activates dialogue. Support pre-flag check to restrict interaction based on quest progress.
4. Attack effect controller links with shooting logic to enrich visual performance.

---
## 8. Dialogue, Quest & Opening Narrative System
### Included Scripts
DialogueReader.cs, DialogueData.cs, DialogueManager.cs, OpeningBlackCurtain.cs, IntroNarrationLines.cs, IntroSkipButton.cs, QuestDisplay.cs

### Core Work
Realize JSON-based dialogue playback, opening cutscene with narration, skip function and quest text update. Build complete narrative workflow from opening sequence to dialogue, quests and level activation.

### Technical Implementation & Personal Contribution
1. Define dialogue data structure to parse external JSON files. Implement line-by-line text display, character name presentation and left-click progression. Auto lock input during dialogue and trigger events after dialogue ends for external logic subscription.
2. Opening sequence system includes full black screen transition, rolling narration and skip function. Auto start initial dialogue and update quest UI after opening cutscene.
3. Quest UI controller updates text dynamically by receiving events from dialogue and level modules.
4. Split dialogue reading, data definition and global management for better hierarchy and reusability.

---
## 9. UI Auxiliary System
### Included Scripts
MonsterHealthBarUI.cs, EnemyUICanvasBillboard.cs, EventHintUI.cs, TMPAlphaPulse.cs, AnnouncementDisplay.cs

### Core Work
Implement monster HP bar, interaction tips, text animation and in-game announcement. Complete visual presentation for all auxiliary UI functions.

### Technical Implementation & Personal Contribution
1. Monster HP bar subscribes HP change events. Use `RectTransform` interpolation to achieve smooth HP transition. Auto hide HP bar after monster death.
2. World-space HP bar faces main camera every frame to fix view offset in 3D scene.
3. Unified manager for interaction tips to avoid UI conflict caused by multiple triggers.
4. Develop text pulse animation and announcement display based on TextMeshPro to enrich UI visual effects.

---
## 10. Overall Architecture & Comprehensive Contribution Summary
1. **Architecture Design**: Adopted component-oriented and event-driven architecture. All modules are loosely coupled with single responsibility. The global flag system and input lock connect all business logic. Most functions support visual configuration for easier content iteration.
2. **Core Function Development**: Independently completed 7 major systems: tower defense monster AI & wave management, dual health combat system, async scene loading, third-person character & camera control, JSON dialogue system and minimap navigation. Cover gameplay, combat, narrative, scene and navigation.
3. **Detail Optimization**: Solved practical problems including terrain adaptation, state conflict, coroutine leakage and UI view offset. Applied Editor debugging tools, event callbacks and state machines to ensure stability, performance and usability.
4. **Full Workflow Connection**: Built a complete game loop: Opening cutscene → NPC dialogue → Activate levels → Monster attack → Player combat → Quest update. Combine open-world exploration and tower defense gameplay organically.