# Activity 1: Individual Project Snapshot

## Title

**The Rover and the Village**

------

## One-sentence Idea

A 3D single-player tower defense game where you play as a wandering mage who befriends villagers and leads them in defending their home against waves of incoming enemies.

------

## Player Action

The player explores the village, talks to villagers through a dialogue system to unlock events and allies, then prepares defenses and directly participates in combat to repel enemy waves — managing positioning, abilities, and NPC allies to protect the village from being overrun.

------

## Unity Format

3D game; third-person perspective for exploration and combat, with dynamic lighting and a village environment. Tower defense structure with wave-based enemy spawning and player-directed NPC ally behavior.

------

## Vertical Slice

A playable slice will include one village scene with a dialogue interaction, one wave of enemies approaching from a set direction, and a basic combat encounter where the player and at least one NPC ally fight together.

The player should be able to:

- Walk around the village and trigger a dialogue with a villager
- Have an event flag update after the conversation
- Engage and defeat a wave of enemies using the combat system
- See health bars and basic combat feedback for the player, NPC, and enemies

------

## Core Systems

The first systems needed are:

- **Third-person player controller** (movement, camera, basic attack)
- **Dialogue system** (trigger conversations with villagers, display dialogue box)
- **Event flag system** (track quest/story progression based on player actions)
- **Combat system** (player attack, NPC ally AI, enemy AI with patrol and attack)
- **Wave spawning system** (spawn enemies in waves from set entry points)
- **Inventory system** (item pickup and management)
- **Animation system** (player, NPC, and enemy animations)
- **Health management** (health bars for player, allies, and enemies)

------

## GitHub Status

Repository available at:

```
https://github.com/yzp2005/Game-programming.git
```

------

## Biggest Risk

The biggest risk is coordinating multiple systems (dialogue, event flags, combat, wave spawning) so they work together without conflicts — for example, ensuring enemy waves don't trigger before the story events are resolved, or NPC allies behaving correctly during combat.

To reduce this risk, each system will be built and tested independently first, then integrated one at a time using the event flag system as the central connector.

------

## Next Action

Before the next session, the following should be in place:

- A walkable village scene with basic environment layout
- At least one NPC with a triggerable dialogue interaction
- One event flag that updates after the dialogue ends
- A basic enemy that spawns and moves toward the village
- Player attack and a simple health system with UI