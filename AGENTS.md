# AGENTS.md — context for AI coding tools

AI tools read this file before working on the repo. Keep it short, true and up to date.
Claude Code reads it through `CLAUDE.md`, which contains one line: `@AGENTS.md`.

## The game

Snake-post is a roguelike, hybrid-casual take on Snake for Android, played in portrait with one thumb. The snake always moves forward on an open field (no grid), and the player steers it with a virtual joystick. It picks up parcels from residents and grows one segment per parcel, then delivers each parcel to the address with the matching shape, which removes that segment and pays coins. Delivery milestones offer a choice of three random run upgrades. Deliveries can also trigger random hazards (holes, earthquakes, wind) and, past a threshold, a boss. The run ends when lives run out. Between runs the player spends coins in the shop, picks a loadout of perks and modules, plays a daily 30-second mail-sorting minigame (the Mail Room), and earns career XP towards postal ranks.

## Terms

Use these words in code, UI and docs.

| Term                          | Meaning                                                                                              |
| ----------------------------- | ---------------------------------------------------------------------------------------------------- |
| Parcel                        | A package carried on the snake.                                                                      |
| Capacity                      | Package slots on the snake, shared by parcels and cargo modules. Hard-capped so the snake stays steerable on a phone. |
| Passive perk                  | Always-on bonus. Uses a passive slot (one, a second unlockable), no capacity.                        |
| Cargo module                  | Stronger ability that rides on the snake as a segment. Uses one package slot and is never delivered. |
| Tier                          | Upgrade level of an owned perk or module, bought in the shop with coins.                             |
| PowerUp                       | Permanent shop stat upgrade with repeatable ranks (starting capacity, starting lives).               |
| Run upgrade                   | Random pick-one-of-three upgrade at a delivery milestone. Timed, or lasts until the run ends. Never permanent. |
| Event                         | Random hazard rolled after a delivery. Timed or run-long. Bosses last until defeated.                |
| Shift                         | One 30-second Mail Room round. One free per day; more need a shift ticket.                           |
| Career XP, level, postal rank | Persistent progression, separate from coins. Postal rank sets the Mail Room salary.                  |

"Rank" alone is ambiguous: write "PowerUp rank" or "postal rank".

## Docs

1. `Parcel_Snake_GDD.typ`: the full design spec (Typst source, read it as text). Its numbers are illustrative, not balanced. The section "Key invariants for engineering and QA" lists rules the code must keep.
2. `DECISIONS.md`: decisions the team has made, newest first.
3. `ArchitectureResearch.md`: research comparing architecture options.
4. `pitch.md`: the one-page pitch.

## Tech

- Engine: Unity 6.7 (`6000.7.x`). It compiles C# 9 against .NET Standard 2.1.
- Target: Android only, portrait, one thumb. The GDD cites Apple guidelines, but there is no iOS build.
- Physics: Physics2D (assumed in `ArchitectureResearch.md`).
- Packages: none added yet.
- Services: the GDD plans ads (it cites AdMob), IAP, cloud saves and leaderboards. None chosen yet.

## Architecture

No Unity project or code yet. The Unity project will live at the repo root (`.gitignore` expects `Library/` there). Nothing in `ArchitectureResearch.md` is final. The direction so far:

- Unity-native first: GameObjects, MonoBehaviours, Physics2D, Prefabs. No second simulation that keeps its own copy of what Unity already tracks (positions, colliders).
- Content is data. Parcels, perks, modules, run upgrades, events and bosses are ScriptableObject definitions. A new item of an existing kind should need no new code.
- Composition over inheritance. Perks, modules, run upgrades and events share one effect system.
- Every piece of state has one owner. Three kinds, kept apart:
    - Definitions (ScriptableObjects): authored in the Editor, read-only at runtime.
    - Run state: created when a run starts, discarded when it ends (carried parcels, run upgrades, lives, active events).
    - Save data: kept between runs (wallet, career XP, owned perks and tiers, PowerUp ranks, tickets, daily flags).
- Domains: snake and movement; parcels and deliveries; abilities and effects; world events and bosses; career and currency; shop and unlocks; Mail Room. Each owns its data. Add events or interfaces between domains only when a real need shows up.

Still open (ask before deciding): ECS/DOTS or GameObjects for the snake body; the movement algorithm (trajectory following was discussed); how effects are expressed (stat modifiers, triggers and actions, `[SerializeReference]`); Addressables and downloadable content; save system and backend; ad and IAP SDKs; scenes and init order.

Rules the code must keep (from the GDD):

- With capacity `C`, equipped modules `R`, carried parcels `P`, passive slots `S` and equipped passive perks `A`: `R + P <= C`, `R <= C - 2`, `A <= S <= 2`, and `C` never exceeds the mobile cap. Modules are never delivered or detached mid-run.
- Coins are paid on delivery, never on pickup. Every reward (delivery, shift, challenge, ad, results screen) is credited exactly once, even after a retry, an app suspend or a second visit to the screen.
- Run upgrades end with the run. Their UI says "Until end of run", never "Permanent".
- No ads, shop prompts or full-screen modals during active steering. The snake never keeps moving behind a modal.
- Perks and modules never affect the Mail Room. A shift's postal rank is fixed when the shift starts.
- The XP curve and each payout formula live in one function each, using the GDD's exact integer math.

## Rules for AI

- Follow the existing folder structure and naming. Don't create new top-level folders.
- Don't invent APIs. If unsure about an SDK or Unity API, say so and point to the docs.
- Don't change Player Settings, package name, API levels or build settings unless asked.
- Keep changes small and explain what you changed and why.
- Never commit secrets (keystores, passwords, API keys).
- Read the GDD section for the system you are touching before writing code. Where the docs leave something open or disagree with each other, say so and ask instead of deciding.
- Every number in the GDD is a tuning placeholder. Put it in a config asset or a named constant, never inline.
- Don't add packages (Entities, Burst, Addressables, tweening libraries) or SDKs, and don't build frameworks the team hasn't chosen (event bus, service locator, effect scripting language), unless asked.
- Create, move and rename assets only in the Unity Editor: Unity tracks assets by the GUID in their `.meta` files. Creating new `.cs` files is fine.
- You can't use the Unity Editor. When a change needs Editor work (assign a serialized field, add a component, create a ScriptableObject asset), end the reply with numbered steps under "In Unity": which GameObject or asset, which component, which field, what value.

## Conventions

### C# code

- Unity compiles C# 9 against .NET Standard 2.1. Don't use `record struct`, file-scoped namespaces, `global using`, `required` members or raw string literals. `record` classes and `init` setters need an `IsExternalInit` stub; skip them unless the team adds it.
- Naming:

  | Thing                                                           | Style            | Example                                   |
  | --------------------------------------------------------------- | ---------------- | ----------------------------------------- |
  | Types, methods, properties, public fields, events, constants    | PascalCase       | `CargoModule`, `Deliver()`, `MaxCapacity` |
  | Interfaces                                                      | `I` + PascalCase | `IEffect`                                 |
  | Private fields, including `[SerializeField] private` and static | `_camelCase`     | `_turnRate`                               |
  | Locals and parameters                                           | camelCase        | `targetAddress`                           |
- Names are full words: `_parcelSpawner`, not `_ps`.
- One top-level type per file, named after the type.
- Namespaces are `SnakeMail.<Area>`, after the repo, so they don't change if the game is renamed. They match the folder under `Assets/_Game/Scripts/` (`SnakeMail.Snake`, `SnakeMail.MailRoom`).
- Braces on their own line, and always use braces, even for one-line `if` bodies.
- Order inside a class: constants, serialized fields, private fields, properties, Unity messages (`Awake`, `OnEnable`, `Update`...), public methods, private methods.
- No magic numbers. A number that means something gets a name: a `const`, a serialized field or a value in a config asset.

### Unity

- One responsibility per component. A snake that moves, collects and takes damage is three components.
- Serialized fields are `[SerializeField] private`. Other scripts read them through a property. When renaming one, keep the Inspector values with `[FormerlySerializedAs("oldName")]`.
- Fail loudly and early. Check required references in `Awake`, log a clear error and disable the component, or use `[RequireComponent]`. Don't scatter null checks through the code.
- Wire references in the Inspector. No `GameObject.Find` or `FindObjectOfType` for setup. Cache `GetComponent` results in `Awake`; never call it every frame.
- Never write to a ScriptableObject definition at runtime. In the Editor, Play Mode changes to an asset are saved to disk. Cooldowns, charges and other live values go on a runtime object.
- Keep game rules and formulas (capacity math, XP curve, payouts) in plain C# classes, so they can have Edit Mode tests.
- Don't assume a run starts when the scene loads. Anything holding run state must reset cleanly for the next run.
- Mobile: no allocations in per-frame code during a run (no LINQ, string building or `new` collections in `Update`). Profile on a phone before reaching for Jobs or Burst.

### Assets and scenes

- `Assets/_Game/`: our code and assets, in `Scripts/<Area>/`, `Prefabs/`, `Scenes/` (test scenes in `Scenes/Test/`), `Config/`, `Art/`, `Audio/`, and `Tests/EditMode/<Area>/`. Create each folder in Unity when its first file arrives; Git doesn't keep empty folders.
- `Assets/ThirdParty/`: copied third-party code, each with its licence file.
- One owner per scene. Everyone else works in their own test scene: `Scenes/Test/Test_<Name>.unity`.

### Comments and logging

- Comments explain why, not what: a reason, a constraint or a trap the code can't show.
- No commented-out code; Git keeps the old version.
- `TODO` comments name a person: `// TODO(Kylee): move to config`.
- Pass the object as context: `Debug.LogError(message, this)`. Error messages name the GameObject, the component and what is missing: `"Snake: SnakeMovement has no MovementConfig assigned."`
- Log state changes, not every frame.

### Git

- Commit messages are plain imperative sentences, a few words, one or two sentences at most: `Add parcel pickup to the snake head`.
- Always commit `.meta` files together with their assets.
- Images, audio, fonts and models go through Git LFS (set up in `.gitattributes`). Run `git lfs install` once on each machine before cloning.
- Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, builds or keystores. `.gitignore` covers them.