# AGENTS.md

## Project Context

This is a Unity project.

Primary stack:

* Unity 6000.x
* C#
* HDRP
* TextMeshPro
* DOTween, if already present in the project
* R3 and UniTask, if already present in the project
* Custom DI container under `Assets/_Source/Scripts/DI`

The project is a game project with gameplay, inventory, equipment, UI, item configs, rendering tools, editor tools, and asset import workflows.

## Main Rule

Work narrowly. Do not inspect or edit unrelated files unless the task explicitly requires it.

Before making broad architectural changes, inspect the relevant files and produce a short implementation plan.

## Code Style

Use the following C# conventions:

* Private fields must start with `_`.

```csharp
private int _currentAmmo;
[SerializeField] private Transform _spawnPoint;
```

* Public properties, methods, classes, structs, interfaces, and events use PascalCase.

```csharp
public int CurrentAmmo { get; private set; }
public void Reload() { }
```

* Constants use UPPER_CASE.

```csharp
private const int MAX_ITEMS = 64;
```

* Avoid public fields.
* Prefer `[SerializeField] private` fields over public serialized fields.
* Keep MonoBehaviours small and focused.
* Avoid god objects such as oversized `GameManager`, `Bootstrap`, `InventoryController`, or `PlayerController`.
* Do not add comments that merely repeat the code.
* Add comments only for non-obvious Unity-specific behavior, serialization constraints, asset pipeline assumptions, or architectural boundaries.

## Unity Serialization Rules

Unity serialization safety is critical.

Do not rename serialized fields unless absolutely necessary.

If serialized fields must be renamed:

* Do not use `FormerlySerializedAs`.
* Restore inspector references manually through code, editor tooling, prefab/scene migration, or explicit serialized asset updates.
* Explain exactly how references were preserved.

Do not break existing prefab, scene, ScriptableObject, material, animator, or inspector references.

Before changing serialized fields, inspect usage in:

* Prefabs
* Scenes
* ScriptableObjects
* Editor scripts
* Custom inspectors
* Runtime reflection or serialization code

## Folder Boundaries

Main scripts are under:

```text
Assets/_Source/Scripts
```

Do not touch this folder unless it is relevant to the current task:

```text
Assets/_Source/Scripts/DI
```

The DI folder contains a custom dependency injection container. Use it when it improves architecture, but do not refactor or replace the DI container unless explicitly requested.

Preferred folder separation:

```text
Assets/_Source/Scripts/Gameplay
Assets/_Source/Scripts/Inventory
Assets/_Source/Scripts/Items
Assets/_Source/Scripts/UI
Assets/_Source/Scripts/Rendering
Assets/_Source/Scripts/Editor
Assets/_Source/Scripts/Services
Assets/_Source/Scripts/Infrastructure
Assets/_Source/Scripts/Configs
```

Do not create new top-level architecture folders without a clear reason.

## Architecture Rules

Follow SOLID pragmatically.

Prefer:

* Small services with clear responsibility
* Interfaces only when they reduce coupling or enable testing
* Composition over inheritance
* Explicit dependencies
* ScriptableObjects for configs and item data
* Separate runtime logic from editor tooling
* Separate gameplay logic from UI
* Separate data/configs from behavior

Avoid:

* Overengineering
* Abstract factories for simple object creation
* Interfaces with only one implementation unless they are useful for DI, testing, or architectural boundaries
* Static global access for gameplay state
* Large MonoBehaviours that own unrelated systems
* Hidden scene dependencies
* Runtime code depending directly on editor-only APIs

## Async and Reactive Code

If R3 is used:

* Prefer clear observable lifetimes.
* Dispose subscriptions properly.
* Avoid leaking subscriptions from MonoBehaviours.
* Do not mix old UniRx patterns unless the project already uses them and migration is not part of the task.

If UniTask is used:

* Use cancellation tokens where lifetime matters.
* Prefer cancellation tied to MonoBehaviour or service lifecycle.
* Avoid `async void` except for Unity event entry points where unavoidable.
* Do not replace simple synchronous code with async code without a concrete reason.

## Editor Scripts

Editor-only scripts must be placed in an `Editor` folder.

Editor scripts must not be included in runtime assemblies.

For editor tools:

* Support Undo where appropriate.
* Mark dirty modified assets or scenes.
* Save assets only when necessary.
* Avoid destructive operations without clear confirmation or explicit task requirement.
* Preserve `.meta` files and asset GUIDs.

## Assets, Prefabs, Scenes, and Meta Files

Do not delete `.meta` files.

Do not regenerate assets unnecessarily.

Do not change import settings, materials, textures, prefabs, scenes, render pipeline assets, or project settings unless the task specifically requires it.

When changing prefabs or scenes:

* Keep changes minimal.
* Explain what changed.
* Mention any required manual Unity steps.

## HDRP Rules

This project uses HDRP.

When working with materials or shaders:

* Prefer HDRP-compatible shaders.
* For Lit materials, use HDRP/Lit.
* Be careful with texture property names.
* Do not assume URP shader properties are valid in HDRP.
* Do not downgrade materials to Built-in or URP shaders.

When working with rendering:

* Avoid changes that depend on the current scene lighting unless explicitly desired.
* Icon rendering, preview rendering, and isolated render tools should use isolated cameras, lights, layers, and render targets where possible.

## UI Rules

Separate UI view logic from gameplay state.

For MVVM-style UI:

* View should handle Unity references and visual updates.
* ViewModel should expose state and commands.
* Model/services should own gameplay data and persistence.
* Do not put inventory/equipment/gameplay rules directly into UI components.

For TextMeshPro:

* Use TMP components explicitly.
* Avoid runtime string allocations in hot paths when possible.

## Testing and Validation

When changing code:

* Run the smallest relevant validation available.
* Prefer targeted compile checks, unit tests, or editor tests if the project has them.
* Do not run expensive or unrelated test suites unless necessary.

If validation cannot be run:

* Say so explicitly.
* Explain what should be checked manually in Unity.

Before finishing, review the diff for:

* Broken serialized references
* Unintended scene/prefab changes
* Unrelated formatting changes
* Missing null checks
* Subscription or async lifetime leaks
* Editor/runtime assembly separation issues

## Response Policy

Only provide a response after the entire task is fully completed.

Do not write progress updates, intermediate messages, partial summaries, or status reports while working.

The final response should include only the completed result.


## Response Format

After completing a task, respond with:

1. Short summary
2. Changed files
3. Important implementation details
4. Risks or limitations
5. Manual Unity steps, if any

Do not explain basic Unity or C# concepts unless asked.

Keep the final response concise and technical.

## Token and Scope Discipline

To save context and tokens:

* Do not perform broad repository scans unless necessary.
* Start from the files mentioned in the user request.
* Search only the smallest relevant folder first.
* Do not inspect generated folders such as `Library`, `Temp`, `Obj`, `Build`, `Logs`, or `UserSettings`.
* Do not read large assets unless needed.
* Do not summarize unrelated code.
* Do not rewrite files only for formatting.
* Do not create new architecture layers unless directly useful for the task.

## Forbidden Unless Explicitly Requested

Do not:

* Update Unity packages
* Change render pipeline assets
* Change project settings
* Rename serialized fields casually
* Use `FormerlySerializedAs`
* Replace the DI container
* Reorganize the whole project
* Touch scenes or prefabs unnecessarily
* Add new dependencies
* Convert HDRP assets to URP or Built-in
* Introduce large architectural rewrites for small bug fixes

## Communication Language

When responding to the user, use Russian unless the user explicitly asks for English.

Code, class names, method names, variables, commit messages, file names, and technical identifiers should remain in English.