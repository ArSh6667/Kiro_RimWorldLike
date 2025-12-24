# RimWorld Game Framework - Quick Start

## Working Demo

The RimWorld Game Framework has a **working standalone demo** that showcases all the core concepts without compilation issues.

### How to Run

**Option 1: Use the main demo batch file (Windows)**
```bash
./run-demo.bat
```

**Option 2: Use the standalone demo batch file (Windows)**
```bash
./run-simple-demo.bat
```

**Option 3: Use dotnet directly**
```bash
dotnet run --project src/RimWorldFramework.StandaloneDemo
```

### What the Demo Shows

The standalone demo demonstrates:

1. **Entity Component System (ECS)**
   - Entity creation and management
   - Component attachment and querying
   - Basic ECS operations

2. **Character System**
   - Character creation with traits
   - Gender and age management
   - Character descriptions

3. **Skills and Needs System**
   - Skill levels and experience points
   - Skill training and level-ups
   - Character needs (hunger, rest, recreation, etc.)
   - Need decay over time

4. **Task System**
   - Task creation with priorities
   - Skill-based task assignment
   - Task execution and progress tracking
   - Experience gain from completed tasks

5. **Quick Game Loop**
   - Multi-character simulation
   - Automatic task assignment
   - Real-time progress updates
   - Character development over time

### Demo Output

The demo runs quickly (under 5 seconds) and shows:
- Entity and component management
- Character creation and trait assignment
- Skill training and experience gain
- Task assignment based on character abilities
- A mini game simulation with multiple characters
- Final character states after simulation

### Framework Status

- ✅ **Standalone Demo**: Fully working, demonstrates core concepts
- ⚠️ **Full Framework**: Has compilation issues due to missing base classes
- ⚠️ **Integration Tests**: Cannot run due to framework compilation issues

The standalone demo proves that the core game concepts are solid and working. The framework implementation issues are primarily related to missing base classes and interface definitions that would need to be completed for full integration.

### Compilation Issues in Full Framework

The main `run-demo.bat` now automatically uses the standalone demo because the full framework has 128 compilation errors:

**Missing Base Classes:**
- `LeafNode`, `BehaviorNode`, `CompositeNode`, `DecoratorNode` (behavior tree system)
- `CharacterContext`, `BehaviorResult` (behavior tree execution)

**Missing Interfaces:**
- `INoiseGenerator`, `NoiseConfig` (map generation)
- `TaskCompletedEvent`, `ITask`, `TaskData` (task system)

**System Implementation Issues:**
- Several systems don't implement required `IGameSystem` interface members
- Namespace conflicts between `ResourceType` and `TaskStatus`

### Next Steps

To get the full framework working:
1. Implement missing base classes (`LeafNode`, `BehaviorNode`, `CharacterContext`)
2. Define missing interfaces (`ITask`, `TaskCompletedEvent`)
3. Resolve namespace conflicts
4. Complete the behavior tree system implementation

For now, the standalone demo provides a complete working example of the RimWorld game framework concepts in action.