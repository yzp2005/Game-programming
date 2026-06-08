# Activity 1: Individual Project Snapshot

## Title

**Niko's Adventure**

## One-sentence Idea

A 3D third-person magic exploration game where you play as a wandering mage, looting arcane treasures, fighting magical creatures, and fleeing overwhelming dangers to grow stronger in a fantasy open world.

## Player Action

The player repeatedly explores open-world areas and dungeons to search for loot, casts elemental magic to fight enemies, uses evasive skills or quick escapes to flee from powerful foes, and sells collected treasure to buy upgrades and strengthen the main character.

## Unity Format

3D game; third-person perspective for exploration and combat, with free-roaming camera control and dynamic lighting for the fantasy environment.

## Vertical Slice

A playable slice will include one small open-world area with a dungeon entrance, one basic enemy encounter, a simple combat system with one elemental spell, and a basic loot pickup/sell loop.

The player should be able to explore the area, fight one enemy with a fire spell, pick up treasure, sell it at a basic shop NPC, and see the character's health/mana bars update.

## Core Systems

The first systems I need are:

- Third-person player controller (movement, camera, basic dodge)
- Elemental magic attack system (fire/ice bolt with cooldown)
- Enemy AI (basic patrol and attack behavior)
- Loot pickup and inventory system
- Basic shop system (sell treasure for gold)
- Health and mana management system
- Simple combat feedback (hit effects, damage numbers)

## GitHub Status

Repository will be created at:

```
https://github.com/yzp2005/Game-programming.git
```

## Biggest Risk

The biggest risk is balancing the "loot, fight, flee" loop so that exploration feels rewarding without making combat either too easy or too punishing. Overloading the game with too many spells, enemy types, or open-world content early on could also slow down development.

To reduce this risk, I will start with a minimal playable loop: one spell type, one enemy, one small area, and a simple shop, then add more magic types, enemies, and regions incrementally.

## Next Action

Before the next session, I will create a playable prototype in Unity with:

- A third-person player controller with movement and camera
- One basic elemental magic attack (e.g., fire bolt)
- One simple enemy with basic AI
- A health/mana UI and basic damage feedback
- A single loot pickup and gold system