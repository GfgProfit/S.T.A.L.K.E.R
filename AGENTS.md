# AGENTS.md

## Project Context

This is a Unity game project.

Primary stack:
- Unity 6000.x
- C#
- HDRP
- TextMeshPro
- DOTween, only if already present in the project
- R3, only if already present in the project
- UniTask, only if already present in the project
- Custom DI container under `Assets/_Source/Scripts/DI`

Main scripts are located under:

```text
Assets/_Source/Scripts
```

The project contains gameplay systems, inventory, equipment, UI, item configs, rendering tools, editor tools, and asset import workflows.

Work narrowly. Do not inspect, edit, rename, move, reformat, or summarize unrelated files.

## Instruction Priority

Follow instructions in this priority order:

1. Explicit user request
2. Hard Constraints
3. Project-specific rules in this file
4. Existing project patterns
5. General best practices

If a rule conflicts with a generic best practice, personal preference, or default agent behavior, follow the higher-priority rule.

If the user explicitly asks for something that conflicts with this file, follow the explicit user request.

## Hard Constraints

These rules override all implementation preferences.

FORBIDDEN:
- Sending progress updates
- Sending intermediate messages
- Sending partial summaries
- Sending implementation plans unless explicitly requested
- Running Unity builds
- Running Unity assembly compilation
- Running full project compilation
- Running `dotnet build`
- Running `msbuild`
- Running package restore
- Running full test suites
- Running expensive validation commands
- Running automatic code formatters
- Performing repository-wide scans
- Searching the entire `Assets` folder without a discovered dependency
- Reading unrelated files
- Editing unrelated files
- Rewriting files only for formatting

REQUIRED:
- Reply only once after the task is fully completed
- Keep the final response concise and technical
- Use Russian in user-facing responses unless the user explicitly requests another language
- Preserve existing code style
- Preserve serialized Unity references
- Minimize token usage
- Inspect only files directly related to the task
- Implement the smallest safe change that solves the task

## Communication Rules

Do not send progress updates, status messages, intermediate summaries, or partial results.

Do not say what you are about to do while working.

Do not ask for confirmation unless the task is impossible or unsafe without missing information.

Do not explain basic Unity, C#, Git, or IDE concepts unless asked.

Respond only after the task is completed.

The final response must use Russian unless the user explicitly requests English.

Code, class names, method names, variable names, namespaces, file names, folder names, package names, commit messages, and technical identifiers must remain in English.

## Final Response Format

The final response must contain only these sections:

1. Short summary
2. Changed files
3. Important implementation details
4. Risks or limitations
5. Manual Unity steps, if any

Keep the final response concise and technical.

If validation was not run because it is forbidden or unsafe, say so in `Risks or limitations` and list manual checks in `Manual Unity steps`.

## Existing Code First

Before introducing a new pattern, inspect nearby relevant code.

Prefer consistency with existing project code over theoretical improvements.

When modifying an existing feature:
- Follow surrounding architecture
- Follow surrounding naming
- Follow surrounding folder structure
- Follow surrounding dependency patterns
- Follow surrounding formatting
- Follow existing serialization patterns

Do not rewrite working code to match a preferred pattern.

Do not introduce a new architectural style if the nearby code already has a clear pattern.

If the project already uses R3, UniTask, MVVM, DOTween, or the custom DI container in the affected area, continue using the same approach.

## Scope Discipline

Start from files explicitly mentioned by the user.

If no file is mentioned, start from the smallest likely folder based on the feature name.

Expand scope only when a discovered dependency requires it.

FORBIDDEN:
- Repository-wide scans
- Searching the entire `Assets` folder by default
- Searching generated folders
- Reading unrelated files
- Opening large files unless necessary
- Summarizing unrelated code
- Inspecting assets only to understand general project structure

Do not inspect these folders:
- `Library`
- `Temp`
- `Obj`
- `Build`
- `Builds`
- `Logs`
- `UserSettings`
- `.git`
- `.vs`
- `.idea`
- `.vscode` unless explicitly relevant
- `MemoryCaptures`
- `Recordings`

Do not inspect generated files, cache files, build outputs, or IDE files unless explicitly requested.

## Change Scope Control

Implement the smallest change that solves the requested task.

Do not:
- Refactor unrelated systems
- Rewrite working code
- Rename unrelated symbols
- Rename serialized fields casually
- Move files without explicit request
- Split classes without explicit request
- Merge classes without explicit request
- Create new architecture layers without direct need
- Convert code to another paradigm without direct need
- Change public APIs unless required by the task
- Change behavior outside the requested scope

Bug fixes must be surgical.

Feature additions must integrate with existing patterns.

Formatting-only changes are allowed only inside the modified lines needed for the task.

## Token Discipline

Minimize context usage.

Prefer targeted inspection over broad exploration.

Avoid:
- Opening many files at once
- Reading large assets
- Reading scenes or prefabs unless required
- Repeating file contents in the final response
- Explaining unrelated architecture
- Listing unrelated findings

Use the smallest useful amount of information to complete the task correctly.

## Forbidden Commands

Never run these unless the user explicitly requests them:
- Unity builds
- Unity batchmode builds
- Unity assembly compilation
- Unity project-wide compilation
- `dotnet build`
- `dotnet test`
- `dotnet restore`
- `msbuild`
- package restore commands
- full test suites
- expensive linters
- automatic formatters
- project-wide analyzers that trigger compilation

Allowed validation:
- Static review
- File-level checks
- Syntax-level reasoning
- Small targeted checks that do not compile or rebuild the Unity project

If validation cannot be run safely, do not run it. Mention the limitation in the final response.

## C# Code Style

Private fields must start with `_`.

```csharp
private int _currentAmmo;
[SerializeField] private Transform _spawnPoint;
```

Constants must use UPPER_CASE.

```csharp
private const int MAX_ITEMS = 64;
```

Interfaces must start with `I`.

```csharp
public interface IInventoryService
{
}
```

Use PascalCase for:
- Classes
- Structs
- Interfaces
- Public properties
- Public methods
- Events
- Enum names
- Enum values

Use camelCase for:
- Local variables
- Method parameters

Avoid public fields.

Prefer `[SerializeField] private` fields over public serialized fields.

Do not split method parameters across multiple lines.

Correct:

```csharp
public void Initialize(IInventoryService inventoryService, IItemFactory itemFactory, int maxItems)
{
}
```

Incorrect:

```csharp
public void Initialize(
    IInventoryService inventoryService,
    IItemFactory itemFactory,
    int maxItems)
{
}
```

Do not split `if`, `while`, `for`, `foreach`, `switch`, `using`, `lock`, lambda, or ternary conditions across multiple lines.

Correct:

```csharp
if (_inventoryService != null && _itemFactory != null && _maxItems > 0)
{
}
```

Incorrect:

```csharp
if (_inventoryService != null &&
    _itemFactory != null &&
    _maxItems > 0)
{
}
```

Keep braces on separate lines.

Do not use expression-bodied members if surrounding code does not use them.

Do not add comments that merely repeat the code.

Add comments only for:
- Non-obvious Unity behavior
- Serialization constraints
- Asset pipeline assumptions
- Editor tooling side effects
- Architectural boundaries
- Non-obvious performance or lifetime reasoning

## Formatting Restrictions

Preserve surrounding file formatting.

Do not reformat entire files.

Do not change line wrapping unrelated to the task.

Do not run automatic code formatters.

Keep method parameters on one line.

Keep conditions on one line.

Keep existing indentation style.

Keep existing namespace style.

Keep existing ordering of fields, methods, and Unity lifecycle methods unless changing it is required.

## Unity Serialization Safety

Unity serialization safety is critical.

Assume every serialized field may be referenced by:
- Prefabs
- Scenes
- ScriptableObjects
- Editor tooling
- Custom inspectors
- Save systems
- Runtime reflection
- Animation bindings
- Timeline bindings

Do not rename serialized fields unless explicitly requested.

Do not use `FormerlySerializedAs`.

Do not remove serialized fields unless explicitly requested or proven unused in the affected assets.

Do not change serialized field types unless explicitly required.

Do not change `[SerializeField]` visibility or backing fields unless required.

Prefer preserving serialized data over code cleanliness.

If serialized fields must be changed:
- Inspect relevant usage first
- Preserve references through explicit migration, editor tooling, serialized asset updates, or manual restoration
- Explain exactly how references were preserved in the final response

Do not break existing references in:
- Prefabs
- Scenes
- ScriptableObjects
- Materials
- Animators
- Inspectors

## Folder Boundaries

Main scripts are under:

```text
Assets/_Source/Scripts
```

Do not touch this folder unless the task is relevant:

```text
Assets/_Source/Scripts/DI
```

The DI folder contains a custom dependency injection container. Use it when it improves architecture, but do not refactor or replace it unless explicitly requested.

Preferred folders:

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

Editor-only scripts must be placed in an `Editor` folder.

Runtime scripts must not depend on editor-only APIs.

## Dependency Rules

Do not add new dependencies unless explicitly requested.

Do not add:
- NuGet packages
- Unity packages
- Third-party libraries
- Editor extensions
- Reactive frameworks
- Async frameworks
- DI frameworks

Prefer dependencies already present in the project.

Use DOTween only if already present in the project.

Use R3 only if already present in the project.

Use UniTask only if already present in the project.

Do not replace existing project dependencies with alternatives.

## Dependency Injection Rules

Use the existing DI container when introducing application-level services.

Do not replace the DI container.

Do not introduce another DI framework.

Do not refactor the DI container unless explicitly requested.

Prefer existing registration patterns.

Prefer constructor injection where supported by the project.

Do not instantiate application services inside MonoBehaviours when they are expected to be injected.

Do not introduce static service locators for gameplay state.

Do not bypass DI with global singletons unless the surrounding code already uses that pattern and changing it is outside the task scope.

## Architecture Rules

Follow SOLID pragmatically.

Prefer:
- Small focused services
- Clear responsibilities
- Explicit dependencies
- Composition over inheritance
- ScriptableObjects for configs and item data
- Separate runtime logic from editor tooling
- Separate gameplay logic from UI
- Separate data/configs from behavior
- Interfaces only when they reduce coupling, support DI, enable testing, or define architectural boundaries

Avoid:
- Overengineering
- Abstract factories for simple object creation
- Interfaces with only one implementation unless useful for DI, testing, or boundaries
- Static global access for gameplay state
- Hidden scene dependencies
- Large god objects
- Runtime code depending directly on editor-only APIs
- Large architectural rewrites for small bug fixes

## MonoBehaviour Rules

Keep MonoBehaviours small and focused.

MonoBehaviours should coordinate Unity lifecycle, Unity references, scene references, and visual components.

MonoBehaviours should not own unrelated systems.

Avoid oversized classes such as:
- `GameManager`
- `Bootstrap`
- `InventoryController`
- `PlayerController`

Do not put persistence, inventory rules, equipment rules, or business logic directly into UI MonoBehaviours.

Do not introduce hidden scene dependencies.

Prefer explicit serialized references or injected dependencies according to nearby project patterns.

## R3 Rules

Use R3 only if it is already present in the project.

If surrounding code uses R3, continue using R3.

Prefer:
- `ReactiveProperty`
- Observable streams
- Explicit ownership
- `CompositeDisposable` or equivalent lifetime ownership
- Disposal tied to object, service, or MonoBehaviour lifetime

Every subscription must have a clear lifetime.

Every subscription must be disposed.

Do not leak subscriptions from MonoBehaviours.

Do not store reactive state statically unless ownership and lifetime are explicit.

FORBIDDEN:
- Orphan subscriptions
- Static reactive state without ownership
- Mixing R3 and UniRx in new code
- Introducing UniRx if R3 is used
- Introducing alternative reactive frameworks
- Subscribing without disposal
- Capturing destroyed Unity objects in long-lived subscriptions

## UniTask Rules

Use UniTask only if it is already present in the project.

If surrounding code uses UniTask, continue using UniTask.

Prefer UniTask over `Task` for Unity runtime code.

Use cancellation tokens when lifetime matters.

CancellationToken must be propagated through the entire async chain.

Cancellation should be tied to MonoBehaviour, service, or operation lifetime where appropriate.

FORBIDDEN:
- `async void` except Unity event entry points where unavoidable
- Fire-and-forget async code without explicit intent
- `Task.Run` for Unity gameplay code
- Blocking async calls with `.Result`
- Blocking async calls with `.Wait()`
- Ignoring cancellation for long-running operations
- Replacing simple synchronous code with async without a concrete reason

## MVVM Rules

If a UI feature already follows MVVM, continue using MVVM.

For new UI features, prefer MVVM when it reduces coupling and matches nearby UI architecture.

View responsibilities:
- Unity references
- Visual updates
- Animations
- Binding to ViewModel
- Forwarding user input to ViewModel commands

ViewModel responsibilities:
- Presentation logic
- UI state
- Commands
- Reactive state
- Formatting display data when appropriate

Model and service responsibilities:
- Gameplay logic
- Inventory logic
- Equipment logic
- Persistence
- Business rules
- Data ownership

FORBIDDEN:
- Gameplay logic inside View
- Inventory rules inside UI components
- Equipment rules inside UI components
- Direct scene dependencies inside ViewModel
- Direct Unity object manipulation inside ViewModel unless the surrounding project pattern already does it
- UI components directly mutating gameplay state when a ViewModel or service exists

## UI Rules

Separate UI view logic from gameplay state.

Use TMP components explicitly.

Avoid runtime string allocations in hot paths where practical.

UI components should not own gameplay rules.

UI should communicate through ViewModels, services, commands, or existing project patterns.

Do not mix UI rendering concerns with inventory, equipment, persistence, or item config rules.

## TextMeshPro Rules

Use TextMeshPro types explicitly:
- `TMP_Text`
- `TextMeshProUGUI`
- `TMP_InputField`

Do not use legacy `UnityEngine.UI.Text` for new UI unless surrounding code requires it.

Avoid repeated string allocations in frequently updated UI.

## Editor Scripts

Editor-only scripts must be placed in an `Editor` folder.

Editor scripts must not be included in runtime assemblies.

Runtime code must not reference `UnityEditor`.

For editor tools:
- Support Undo where appropriate
- Mark modified assets or scenes dirty
- Save assets only when necessary
- Avoid destructive operations without explicit confirmation or task requirement
- Preserve `.meta` files
- Preserve asset GUIDs
- Avoid changing import settings unless explicitly required
- Avoid modifying scenes or prefabs unless explicitly required

## Asset Safety

Assume the project contains production assets.

Do not modify:
- Scenes
- Prefabs
- Materials
- Textures
- ScriptableObjects
- Animator controllers
- Animation clips
- Timeline assets
- Import settings
- Render pipeline assets
- Project settings
- `.meta` files

unless explicitly requested or directly required by the task.

Do not regenerate assets unnecessarily.

Do not recreate prefabs, materials, or ScriptableObjects to fix code issues.

Do not modify GUIDs.

When asset modification is required:
- Keep changes minimal
- Explain exactly what changed
- Mention manual Unity steps if needed

## Prefab and Scene Safety

Do not touch scenes or prefabs unless explicitly required.

If a prefab or scene must be changed:
- Keep changes minimal
- Preserve references
- Preserve object hierarchy unless the task requires changes
- Preserve component order unless the task requires changes
- Explain what changed in the final response

Do not make broad scene cleanup changes.

Do not apply unrelated prefab overrides.

## HDRP Rules

This project uses HDRP.

When working with materials or shaders:
- Prefer HDRP-compatible shaders
- Use `HDRP/Lit` for Lit materials
- Be careful with texture property names
- Do not assume URP shader properties are valid in HDRP
- Do not downgrade materials to Built-in or URP shaders unless explicitly requested
- Do not convert HDRP assets to URP unless explicitly requested

When working with rendering:
- Avoid changes that depend on current scene lighting unless explicitly required
- Preview and icon tools should use isolated cameras, lights, layers, and render targets where possible
- Do not change render pipeline assets unless explicitly requested

## Material and Shader Safety

Do not change material shaders unless explicitly requested.

Do not overwrite material properties without preserving existing values.

Do not assume `_BaseMap`, `_BaseColor`, `_MaskMap`, `_NormalMap`, or other shader properties exist without checking.

Do not modify shared project materials during preview, icon, import, or editor tooling tasks unless explicitly requested.

Prefer temporary materials, isolated previews, or reversible changes for tooling.

## Item, Inventory, and Equipment Rules

Keep item data, item configs, inventory logic, and equipment logic separated.

Prefer ScriptableObjects for static item data and configs.

Do not put inventory or equipment rules directly into UI components.

Do not duplicate item state between systems unless required by existing architecture.

Do not introduce hidden dependencies between item configs and scene objects.

Use existing item and inventory patterns before adding new abstractions.

## Services and Persistence Rules

Services should have clear ownership and lifetime.

Do not use static services for mutable gameplay state unless the surrounding project already does.

Persistence code must be explicit and isolated.

Do not change save data schema unless explicitly required.

If save data schema changes are required:
- Preserve backward compatibility when practical
- Explain migration risks
- Mention manual validation steps

## Error Handling and Null Safety

Prefer explicit null checks where Unity references may be missing.

Do not hide errors silently.

For editor tools, report actionable errors with enough context.

For runtime code, avoid spammy logs in hot paths.

Do not add excessive logging.

Use `Debug.LogError`, `Debug.LogWarning`, or exceptions according to nearby project conventions.

## Performance Rules

Avoid allocations in hot paths.

Avoid LINQ in hot paths unless surrounding code already uses it and performance is irrelevant.

Avoid repeated `GetComponent` calls in update loops.

Avoid unnecessary `FindObjectOfType`, `FindObjectsOfType`, `GameObject.Find`, or tag searches.

Cache references when appropriate.

Do not optimize prematurely outside the task scope.

## Testing and Validation

Use the smallest relevant validation available.

Do not run full builds, Unity assembly builds, package restore, or expensive test suites.

Before finishing, review the changed code for:
- Broken serialized references
- Unintended scene or prefab changes
- Unrelated formatting changes
- Missing null checks
- Subscription leaks
- Async lifetime issues
- Editor/runtime assembly separation issues
- Violations of project code style

If validation was not run, explain why and list manual Unity checks.

## Manual Unity Validation

When relevant, mention manual checks such as:
- Open affected scene or prefab
- Verify inspector references
- Enter Play Mode
- Test affected UI flow
- Test affected inventory/equipment flow
- Verify editor tool behavior on a copy or selected assets
- Check Console for errors

Do not claim Unity validation was performed unless it was actually performed.

## Forbidden Unless Explicitly Requested

Do not:
- Update Unity packages
- Add new packages
- Change render pipeline assets
- Change project settings
- Rename serialized fields
- Use `FormerlySerializedAs`
- Replace the DI container
- Reorganize the project
- Touch scenes unnecessarily
- Touch prefabs unnecessarily
- Touch materials unnecessarily
- Touch ScriptableObjects unnecessarily
- Add new dependencies
- Convert HDRP assets to URP or Built-in
- Introduce large architectural rewrites for small bug fixes
- Run builds
- Run assembly compilation
- Run full test suites
- Run automatic formatters

## Self Check Before Starting

Before starting work, verify internally:
- The task scope is clear
- Only relevant files will be inspected
- No Unity build will be executed
- No assembly compilation will be executed
- No package restore will be executed
- No progress messages will be sent
- Existing code style will be preserved
- Serialized references will remain intact

If any item cannot be satisfied, adjust the approach before editing.

Do not send this self-check to the user.

## Final Verification

Before completing the task, verify:
- No Unity build was executed
- No assembly compilation was executed
- No package restore was executed
- No unrelated files were modified
- No unrelated formatting was applied
- No serialized references were broken
- No `.meta` files were deleted or regenerated unnecessarily
- Code style matches surrounding files
- Private fields use `_`
- Interfaces use `I` prefix
- Constants use UPPER_CASE
- Method parameters are not split across multiple lines
- Conditions are not split across multiple lines
- R3 subscriptions have valid lifetimes if R3 was used
- UniTask cancellation is propagated if UniTask was used
- Editor/runtime separation is preserved
- Final response is concise and uses the required format

If any item fails, revise the implementation before responding.
