# RimWorld Game Framework - Quick Start Demo

## Fixed Encoding Issues

The demo has been updated to use English text to avoid console encoding problems. All Chinese characters have been replaced with English equivalents.

## Quick Run

1. **Windows**: Double-click `run-demo.bat`
2. **Linux/Mac**: Run `./run-demo.sh`

## What You'll See

The demo will show:

```
=== RimWorld Game Framework Demo ===
Press 'q' to quit the game

Initializing game framework...
Game framework initialized successfully!

Creating initial game content...
Generating 100x100 map (seed: 1234567)...
Map generation complete! Terrain types: 8
Created character: Alice (ID: 1)
Created character: Bob (ID: 2)
Created character: Charlie (ID: 3)
Created task: Construction Task (Priority: High)
Created task: Gathering Task (Priority: Normal)
Created task: Research Task (Priority: Normal)
Created task: Cooking Task (Priority: Low)
Created task: Cleaning Task (Priority: High)
Initial game content created successfully!

Game started!
Real-time status updating... (Press 'q' to quit)
```

## Live Status Updates (Every 5 seconds)

```
=== RimWorld Game Framework Demo - Live Status ===
Game Time: 00:00:15
Current Characters: 3

=== Game Statistics ===
Tasks Completed: 2
Skill Level Ups: 1
Characters Created: 3
Research Completed: 0
Buildings Constructed: 0

=== Milestones Achieved ===
* First Character: Create your first character

=== Character Status ===
Alice (ID: 1):
  Mood: 78% | Efficiency: 72%
  Hunger: 25% | Fatigue: 18% | Recreation: 12%
  Top Skills: General:3, Construction:2, Crafting:1

Bob (ID: 2):
  Mood: 85% | Efficiency: 79%
  Hunger: 15% | Fatigue: 22% | Recreation: 8%
  Top Skills: Intellectual:4, Cooking:3, Mining:2

Charlie (ID: 3):
  Mood: 71% | Efficiency: 65%
  Hunger: 32% | Fatigue: 28% | Recreation: 19%
  Top Skills: Mining:5, Construction:2, General:1

Press 'q' to quit the game
```

## Key Features Demonstrated

1. **Real-time Updates**: Character needs increase over time
2. **Task System**: Automatic task assignment and completion
3. **Skill System**: Experience gain and level ups
4. **Progress Tracking**: Statistics and milestone achievements
5. **System Integration**: All subsystems working together

## Controls

- **Run**: Automatic after startup
- **Quit**: Press 'q' key
- **Observe**: Screen refreshes every 5 seconds

The demo showcases a complete game framework with all major systems integrated and working together!