# AGENTS.md — context for AI coding tools

AI tools read this file before working on the repo. Keep it short, true and up to date.
Claude Code reads it through `CLAUDE.md`, which contains one line: `@AGENTS.md`.

## Project and sources

Snake Post is a portrait Android Snake roguelike. Players steer continuously, collect Parcels as body segments, deliver them to matching addresses, choose Upgrades, and progress through a shop, career system, and the separate PostShift sorting minigame.

- `DECISIONS.md` records explicit game-design decisions. Follow the latest applicable decision.
- `Parcel_Snake_GDD.typ` is a **game-design planning document**. Use it to understand intended gameplay, terminology, and constraints. It is **not** an implementation specification: its diagrams, suggested architecture, balancing numbers, equations, and example formulas do not prescribe code structure or exact algorithms.
- `README.md` and `pitch.md` provide project overview and presentation context.
- Read the relevant material and existing code, callers, tests, and project settings before editing. Prefer working behavior and confirmed decisions over speculative prose. Flag material conflicts; otherwise choose a sensible implementation without asking about every unspecified detail.
- Do not rewrite the GDD to justify an implementation or treat tentative design ideas as locked requirements. Keep documentation changes within the requested scope.

At present this repository contains design documents but no Unity project. The intended target is Unity 6 on Android, in portrait; GameObjects and 2D physics are the initial direction. Once a project exists, check its actual Unity version, scripting support, package manifest, and conventions rather than relying on this snapshot.

## Gameplay vocabulary

| Term | Meaning |
| --- | --- |
| **Parcel** | A deliverable item carried as a removable snake segment; consumes Capacity. |
| **Capacity** | Shared limit for carried Parcels and equipped Cargo Perks. |
| **Upgrade / Effect** | An acquirable Upgrade configures one or more gameplay Effects. Its grant source does not determine Effect lifetime. |
| **Upgrade Tier** | A configured strength or behavior stage of an Upgrade, including repeatable improvements. |
| **Passive Perk / Cargo Perk** | Equipment classifications of Perks (equippable Upgrades). Passive Perks use passive slots; Cargo Perks reserve Capacity and remain visible, non-deliverable segments. |
| **Event** | A world occurrence such as wind, an earthquake, or a boss encounter. |
| **PostShift** | Separate 30-second sorting minigame; a shift is one attempt. |
| **Career XP / Career Level / Postal Rank** | Persistent experience, numerical level, and named rank; separate from Coins. |

Use these terms consistently, but **do not create separate class hierarchies, Effect pipelines, or state models just to mirror UI labels**. For example, acquisition from the shop and acquisition during a run can use the same Upgrade definition.

## Engineering approach

- Follow Ponytail's practical YAGNI discipline: understand the real workflow, reuse existing code, prefer C# and Unity's built-in facilities, then implement the smallest complete solution. Simplicity must never remove requested behavior, validation, or a necessary test.
- Prefer cohesive, domain-oriented code for snake movement, Parcels/deliveries, Upgrades/Effects, events, progression/economy, shop, and PostShift. Avoid generic technical layers and classes whose only purpose is forwarding calls.
- Unity-native by default: GameObjects, MonoBehaviours, Prefabs, ScriptableObjects, and appropriate Unity physics APIs. A data-oriented design does not require ECS/DOTS. Adopt Jobs, Burst, custom simulation, or additional packages only for a demonstrated need.
- Make content resource-driven. Prefer one authoritative authored definition for each Parcel, Upgrade, Tier, Event, etc. ScriptableObjects are a good default for reusable content, with behavior composed from configured Effects where that is actually useful.
- Keep **one owner for each mutable state**. Treat authored definitions as read-only during play; run state belongs to the run and resets between runs; persistent progression belongs to the save/profile. Do not mirror Unity transforms, duplicate inventories, or invent registries and caches without a reason.
- Separate domains through small, explicit APIs. Use direct references and calls for ordinary dependencies. Add events, interfaces, or shared coordination only when concrete interactions require them; avoid premature event buses, service locators, global managers, and redundant initialization frameworks.
- Keep shared rules shared, but do not build a universal Effect language or configurable framework before actual content needs it. New content with existing behavior should generally be authorable without new code.
- Prefer composition and ordinary C# types. Use MonoBehaviours for Unity-facing state and lifecycle; plain C# for rules that benefit from independent testing. Split components when responsibilities genuinely differ, not to enforce a component count.
- Design for mobile responsiveness and maintainability. Avoid repeated searches, `GetComponent`, and avoidable allocations in hot loops; cache references or inject them at setup. Profile representative Android devices before adding complex optimizations. LINQ and allocations outside hot paths are fine when clear.
- Treat Addressables, backend services, IAP, ads, and cloud features as integration choices to make when implementing those features. Do not install SDKs or commit to a provider based only on a design draft.

## Gameplay behavior and correctness

- Preserve the intended pickup/delivery loop: collecting a Parcel adds a carried segment when Capacity permits; delivering it removes the appropriate segment and awards the delivery reward. Cargo Perks reserve Capacity but are not delivered.
- Enforce equipment and Capacity limits according to the current game rules and authored configuration. Do not hard-code speculative caps or require exact GDD arithmetic for balance curves, payouts, or progression.
- Effects activate and expire according to their configured behavior and lifetime, independent of where the Upgrade came from.
- A new run starts with fresh run-specific state. Do not rely on scene loading to reset gameplay or leave active effects/subscriptions behind.
- Avoid duplicate rewards and repeated purchase fulfillment. When real-money purchasing or online balances are implemented, validate transactions with the authoritative service and never trust client-submitted balances.
- Do not interrupt active steering with unsolicited shop or ad modals; pause gameplay appropriately when a blocking UI is shown.
- Write focused tests for consequential rules and regressions. Test observable behavior and edge cases, rather than reproducing illustrative GDD calculations verbatim.

## C# and Unity conventions

- Follow conventions already established in code. Until then: PascalCase for types/methods/properties; `IName` for interfaces; `_camelCase` for private fields; camelCase for locals and parameters; Allman braces.
- **Namespace consistency is mandatory.** All first-party C# code uses `SnakePost` as its root namespace, never `SnakeMail` or the global namespace. Place each gameplay domain consistently under `SnakePost.<Domain>` (e.g. `SnakePost.Delivery`, `SnakePost.PostShift`); reserve `SnakePost` itself for genuinely shared types. Keep editor and test code under the same root. Namespace boundaries follow cohesive domains, not every folder level; do not mix root and domain namespaces arbitrarily. Name public MonoBehaviour/ScriptableObject files after their types; keep small related helpers together when clearer.
- Prefer `[SerializeField] private` for Inspector configuration. Expose only the API other code actually needs. Do not add `[FormerlySerializedAs]` routinely; use it only when a field rename must preserve important existing scene, prefab, or asset values.
- Respect Unity lifecycle and ownership: initialize required references once, pair subscriptions with unsubscriptions, and avoid hidden initialization order assumptions. Use `[RequireComponent]` or validation when they prevent real errors.
- ScriptableObject definitions are assets, not per-run mutable containers. Keep cooldowns, charges, and active instances in runtime state.
- Use named configuration for values designers will tune; simple mathematical literals and self-evident constants do not need ceremonial wrappers. Use supported C# and Unity APIs for the actual installed editor version.
- Comments explain intent, non-obvious constraints, and pitfalls. Remove dead/commented-out code. Log actionable failures with context; avoid per-frame log noise.
- Preserve asset GUIDs and `.meta` files when adding, renaming, or moving Unity assets. Use the Editor where useful, but safe file operations are allowed if references and metadata remain valid. Do not make the user perform Editor steps that available tools can do.

## Working practices and tools

- Use `@Ponytail` for implementation and architectural reviews when available; apply its simplicity-first principles even when the plugin cannot be invoked. Prefer built-in Unity features over a new dependency or bespoke abstraction.
- Use `the-writing-whip` when available for substantial documentation or project communication. Keep prose direct and do not let stylistic tooling override technical accuracy.
- Before edits, inspect relevant code paths, assets, tests, and Git changes. Preserve unrelated work and keep changes reviewable. Fix root causes rather than patching symptoms in multiple places.
- Do not change packages, project settings, build profiles, or backend configuration incidentally. Change them when the feature needs it, and explain the reason.
- Run the smallest meaningful verification available (Edit Mode/Play Mode tests, scene check, or device profiling as appropriate). Never claim a Unity test, Editor validation, or Android build ran unless it did; report any remaining manual steps precisely.
- Never commit secrets or generated Unity caches. Keep `.meta` files alongside their assets. Do not stage, commit, or discard the user's work unless requested.
