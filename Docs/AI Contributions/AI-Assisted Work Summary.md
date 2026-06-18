# AI-Assisted Work Summary (ChatGPT & Cursor)
In the whole development cycle of the third-person action tower defense game, I adopted AI as a development assistant. AI participated in the design, coding, debugging and documentation work of about half of the functional modules, effectively lowering the difficulty of development and improving work efficiency. The specific assisted contents are as follows:

## 1. Global Event & Input Lock Module
- Assisted in designing the underlying structure of `GameEventManager`, including `HashSet` data storage, singleton implementation and `DontDestroyOnLoad` cross-scene persistence logic.
- Provided implementation ideas for the input lock mechanism, helped sort out the linkage logic between input status and game states (dialogue, scene loading, cutscenes), and optimized mouse display & cursor lock switching rules.
- Completed part of universal component `SetGameFlag` code writing to adapt to Unity built-in event system.

## 2. Asynchronous Scene Loading & Transition System
- Assisted in implementing `SceneLoadRunner` asynchronous loading logic based on `SceneManager.LoadSceneAsync`.
- Helped optimize screen fade-in & fade-out effects, solved the problem that transition animation was affected by game time scale via `unscaledDeltaTime`.
- Sorted out the full loading process: loading tip display, minimum loading duration control, input lock linkage and post-loading state recovery.
- Completed the auxiliary code for cross-scene spawn ID transmission and player automatic repositioning.

## 3. Dialogue System & Opening Cutscene
- Assisted in defining `DialogueData` data structure and writing JSON file parsing logic for dialogue content.
- Optimized the line-by-line text display and left-click to continue logic of `DialogueReader`, and improved the experience of dialogue playback.
- Provided technical solutions for opening black curtain transition, subtitle rolling and one-click skip functions.

## 4. Partial UI Logic & Code Standardization
- Assisted in writing basic logic of world space UI such as monster health bar facing camera.
- Uniformed code naming rules and coding specifications for all scripts.
- Helped write detailed code comments for key functions and modules.
