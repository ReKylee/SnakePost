#import "@preview/catppuccin:1.1.0": catppuccin, flavors
#import "@preview/merman:0.5.0": show-mermaid-blocks

#let ctp = flavors.mocha.colors
#let horizontalrule = [
  #line(start: (25%, 0%), end: (75%, 0%))
]

#let endnote(num, contents) = [
  #stack(dir: ltr, spacing: 3pt, super[#num], contents)
]
#show terms: it => {
  it
    .children
    .map(child => [
      #strong[#child.term]
      #block(inset: (left: 1.5em, top: -0.4em))[#child.description]
    ])
    .join()
}

#let conf(
  title: none,
  authors: (),
  keywords: (),
  date: none,
  abstract: none,
  cols: 1,
  margin: (x: 0.72in, y: 0.76in),
  paper: "a4",
  lang: "en",
  region: "US",
  font: ("Literata",),
  fontsize: 9.5pt,
  sectionnumbering: none,
  doc,
) = {
  set document(
    title: title,
    author: authors.map(author => author.name),
    keywords: keywords,
  )
  set page(
    paper: paper,
    margin: margin,
    numbering: "1",
  )
  set par(justify: true)
  set text(lang: lang, region: region, font: font, size: fontsize)
  set heading(numbering: sectionnumbering)

  if title != none {
    align(center)[#block(inset: 2em)[
      #text(weight: "bold", size: 1.5em)[#title]
    ]]
  }

  if authors != none and authors != [] {
    let count = authors.len()
    let ncols = calc.min(count, 3)
    grid(
      columns: (1fr,) * ncols,
      row-gutter: 1.5em,
      ..authors.map(author => align(center)[
        #author.name \
        #author.affiliation \
        #author.email
      ])
    )
  }

  if date != none {
    align(center)[#block(inset: 1em)[
      #date
    ]]
  }

  if abstract != none {
    block(inset: 2em)[
      #text(weight: "semibold")[Abstract] #h(1em) #abstract
    ]
  }

  if cols == 1 {
    doc
  } else {
    columns(cols, doc)
  }
}
#show: catppuccin.with(flavors.mocha)
#show: doc => conf(
  title: [Parcel Snake: complete game design specification],
  authors: (
    (name: "Design working document", affiliation: "", email: ""),
  ),
  date: "3 November 2026",
  lang: "en",
  cols: 1,
  doc,
)


#show raw.where(lang: "mermaid"): show-mermaid-blocks(
  width: 100%,
  site-config: (layout: "dagre", theme: "dark", look: "classic"),
  error-mode: "panic",
)

#show heading.where(level: 1): set text(fill: ctp.mauve.rgb, size: 17pt)
#show heading.where(level: 2): set text(fill: ctp.blue.rgb, size: 12pt)
// Improve contrast and row tracking on Mocha's dark page background.
#show table: set table(
  fill: (col, row) => if row == 0 { ctp.surface0.rgb } else if calc.rem(row, 2) == 0 { ctp.mantle.rgb } else {
    ctp.base.rgb
  },
  stroke: (paint: ctp.surface2.rgb, thickness: 0.5pt),
)

#let cap-tile(kind) = {
  let tint = if kind == "MODULE" { ctp.peach.rgb } else { ctp.green.rgb }
  rect(
    width: 100%,
    fill: tint,
    stroke: (paint: ctp.surface1.rgb, thickness: 0.6pt),
    radius: 3pt,
    inset: (x: 1pt, y: 8pt),
    align(center)[#text(fill: ctp.base.rgb, size: 7.5pt, weight: "bold")[#kind]],
  )
}
#let sorting-bin(name) = rect(
  width: 100%,
  fill: ctp.surface0.rgb,
  stroke: (paint: ctp.blue.rgb, thickness: 0.75pt),
  radius: 4pt,
  inset: (x: 3pt, y: 11pt),
  align(center)[#text(fill: ctp.text.rgb, size: 9pt, weight: "semibold")[#name]],
)

= Design baseline
<design-baseline>
#strong[Core premise.] A mobile Snake-style delivery game. The snake
collects packages from residents, grows one segment per package, and
delivers each package to an address identified by the corresponding
shape. Successful deliveries remove their package segments, grant coins,
and advance the run. A finite carrying capacity keeps the snake
controllable on a phone.

#strong[Decisions already established]

- Packages make the snake longer; correctly delivered packages make it
  shorter.
- Package capacity is bounded. Longer snakes are harder to steer on a
  touchscreen.
- The meta loop includes a shop, permanent upgrades, a starting loadout,
  daily challenges, career XP/levels, and daily mail sorting with replay tickets.
- Daily mail sorting is a 30-second swipe-to-bin activity outside
  the Snake run. One shift is free per day; additional reward-bearing
  shifts require an earned, ad-granted, or purchased shift ticket.
  There is no practice mode and no fixed daily coin cap.
- Players initially have one #strong[passive perk slot] and can unlock a second.
- Passive perks each consume one passive slot and no cargo capacity.
  Cargo perks #strong[only] reserve package slots, one per module; they
  do not consume passive perk slots. Every cargo module appears as a
  visible, persistent segment of the snake.
- Selected perks and modules can be upgraded to higher tiers in the shop,
  using coins. Which abilities merit tiers and how many remains a balance
  decision; stronger tiers do not consume additional slots by default.
- Random events can last briefly or persist through a run. Boss threats
  persist until defeated.

#strong[Proposed details to test:] exact coin payouts, upgrade prices,
number of starting lives, maximum safe snake length, random-event
cadence, boss frequency, ad frequency, and daily sorting score thresholds. The established sources for the
30-second / six-symbol mail task and the XP curve are identified below;
our payout and monetization parameters are #emph[illustrative];, not
empirically validated balance targets.

#figure(
  [
    ```mermaid
    flowchart TB
      subgraph META["Between runs"]
        HOME["Home + career level"] --> SHOP["Shop and PowerUps"]
        HOME --> MAIL["Daily free mail shift / ticket replays"]
        MAIL --> WALLET["Earn coins every shift"]
        MAIL --> XP["Gain postal career XP"]
        XP --> RANK["Career level + salary band"]
        RANK --> MAIL
        WALLET --> SHOP
        SHOP --> LOAD["Equip starter perks"]
      end
      subgraph RUN["During a run"]
        START["Spawn"] --> COLLECT["Collect parcels; grow"]
        COLLECT --> DELIVER["Deliver to shape; shrink"]
        DELIVER --> PROGRESS["Coins, upgrade milestones, events, bosses"]
        PROGRESS --> COLLECT
      end
      LOAD --> START
      PROGRESS --> END["Run ends"]
      END --> HOME
      END --> WALLET
      END --> XP
      classDef meta fill:#313244,stroke:#89B4FA,color:#CDD6F4
      classDef run fill:#313244,stroke:#A6E3A1,color:#CDD6F4
      class HOME,SHOP,MAIL,WALLET,XP,RANK,LOAD,END meta
      class START,COLLECT,DELIVER,PROGRESS run
    ```
  ],
  caption: [Core and meta loops, with a daily sorting branch],
)

= Core loop
<core-loop>
== Flow of an active run
<flow-of-an-active-run>
+ #strong[Start.] Spawn at a random valid location with purchased
  permanent bonuses and the selected starting perks. The initial body
  plus attached cargo modules determines starting length.
+ #strong[Collect.] Pick up a package from a resident if there is free
  cargo capacity. The package has a destination shape/address and adds
  one removable snake segment.
+ #strong[Route and deliver.] Find the matching address. Deliver one or
  more compatible packages, shorten the snake by the number delivered,
  credit coins, and increment delivery progress.
+ #strong[Progress.] A delivery-count milestone may offer a choice of
  randomized run upgrades. Each eligible delivery may also roll for a
  random event; a boss begins after a specified delivery threshold.
+ #strong[Survive.] Continue deliveries and boss encounters while lives
  remain. A boss persists until defeated. When no lives remain, enter
  the results screen and return to the meta loop.

#figure(
  [
    ```mermaid
    flowchart TD
      SPAWN["Start with selected loadout"] --> CHOICE{"Collect or deliver?"}
      CHOICE -->|Collect| PICKUP["Pickup adds one parcel segment if space remains"]
      PICKUP --> CHOICE
      CHOICE -->|Deliver| DELIVER["Match address; remove corresponding parcels"]
      DELIVER --> PAY["Credit coins and delivery progress"]
      PAY --> CHECK["Check milestone, event roll, boss threshold"]
      CHECK --> UP["Optional: choose one of three run upgrades"]
      CHECK --> EVENT["Optional: telegraphed environment hazard"]
      CHECK --> BOSS["Optional: boss until defeated"]
      UP --> CHOICE
      EVENT --> CHOICE
      BOSS --> CHOICE
      PICKUP -.-> DAMAGE["Collision or hazard: lose a life"]
      EVENT -.-> DAMAGE
      BOSS -.-> DAMAGE
      DAMAGE --> ALIVE{"Lives remain?"}
      ALIVE -->|Yes| CHOICE
      ALIVE -->|No| RESULTS["End run; keep banked coins"]
      classDef action fill:#313244,stroke:#89B4FA,color:#CDD6F4
      classDef danger fill:#45475A,stroke:#F38BA8,color:#CDD6F4
      class SPAWN,PICKUP,DELIVER,PAY,UP,RESULTS action
      class EVENT,BOSS,DAMAGE danger
    ```
  ],
  caption: [Core delivery, upgrade, hazard, and boss loop],
)

#strong[Destination rules \(proposal).] Each parcel stores its own
destination identity; a delivery consumes only matching parcels. A
destination may consume several matching parcels at once. UI must make
the current cargo contents and their destinations readable without
forcing precision taps while steering. A full snake cannot collect more
parcels, but can still deliver and pursue objectives.

#strong[Delivery versus failure.] Coins are earned when a delivery
succeeds, not when a package is picked up. Already earned coins remain
yours even if the run ends in failure. Undelivered packages are
discarded on death. Damage consumes a life and grants a short recovery
window; the exact collision behavior should be playtested.

== Run progression and rewards
<run-progression-and-rewards>
#figure(
  align(center)[#table(
    columns: 3,
    align: (col, row) => (auto, auto, auto).at(col),
    inset: 6pt,
    [Trigger], [Player reward or consequence], [Lifetime],
    [Package collected], [One segment added; capacity used], [Until delivered or run ends],
    [Correct delivery], [Segment removed; coins awarded; progression advanced], [Coins persist across runs],
    [Upgrade milestone], [Pick one of three random upgrades], [Timed or until run end],
    [Event roll], [Hole, earthquake, fan, or another environment modifier], [Timed or run-long],
    [Boss threshold], [New persistent challenge; boss-specific reward on victory], [Until defeated or run ends],
    [Lives depleted], [Results, banked earnings, return to home], [Ends current run],
  )],
)

#strong[Terminology rule:] the sketch’s "permanent" #emph[random]
upgrades mean #strong[persistent for the current run];. Only shop
purchases are permanent across runs. The UI should use "Until end of
run" instead of "Permanent" in the in-run upgrade picker.

= Starting loadout and package capacity
<starting-loadout-and-package-capacity>
== Independent passive and cargo slots
<two-different-slot-types>
#strong[Passive perk slots] limit the number of small, always-on bonuses
that may be equipped. #strong[Package slots] represent physical capacity
for parcels #emph[or] cargo modules. A cargo module reserves capacity
without using a passive slot; a passive perk uses a passive slot without
reserving capacity.

#figure(
  align(center)[#table(
    columns: 5,
    align: (col, row) => (auto, right, right, auto, auto).at(col),
    inset: 6pt,
    [Category],
    [Passive slots used],
    [Package slots reserved],
    [Physical
      segment],
    [Purpose],

    [Passive perk], [1], [0], [No], [Small, always-on utility or economy effect],
    [Cargo perk / module], [0], [1], [Yes; cannot be delivered], [Strong ability paid for with delivery throughput],
  )],
)

Players start with #strong[one passive perk slot];, with the option to
#strong[permanently unlock a second] from the shop. They may also equip
owned cargo modules directly into their package capacity. Owned perks
and modules can be changed freely between runs; equipping them never
spends coins again. To preserve the delivery loop, the initial rule
proposal leaves #strong[at least two slots for parcels].

#figure(
  [
    #text(weight: "semibold")[Passive: Tip Jar; cargo module: Package Magnet]
    #v(5pt)
    #grid(
      columns: (1fr, 1fr),
      gutter: 7pt,
      rect(fill: ctp.surface0.rgb, stroke: ctp.blue.rgb, radius: 4pt, inset: 9pt)[
        #strong[Tip Jar] \ No cargo cost
      ],
      rect(fill: ctp.surface0.rgb, stroke: ctp.peach.rgb, radius: 4pt, inset: 9pt)[
        #strong[Package Magnet] \ Reserves 1 cargo slot
      ],
    )
    #v(6pt)
    #text(size: 9pt)[Starting capacity budget: 6; max deliverable parcels: 5; one module segment remains attached.]
  ],
  caption: [Independent selections: the passive occupies one passive slot, while the module reserves one package slot],
)

#figure(
  [
    ```mermaid
    flowchart TD
      SHOP["Unlock perks and modules using coins"] --> PASS["Equip up to two passive perks"]
      SHOP --> CARGO["Equip cargo modules into package slots"]
      PASS --> PASSRULE["Each passive uses 1 passive slot; no capacity cost"]
      CARGO --> CARGORULE["Each module reserves 1 package slot; no passive slot"]
      CARGORULE --> SEGMENT["Permanent visible segment; never delivered"]
      PASSRULE --> RUN["Start run"]
      SEGMENT --> RUN
      RUN --> RULES["Compute free capacity and current snake length"]
      classDef regular fill:#313244,stroke:#89B4FA,color:#CDD6F4
      classDef module fill:#45475A,stroke:#F9E2AF,color:#CDD6F4
      class SHOP,PASS,PASSRULE,RUN,RULES regular
      class CARGO,CARGORULE,SEGMENT module
    ```
  ],
  caption: [Independent passive-perk and cargo-module selections],
)

== Capacity and length invariants
<capacity-and-length-invariants>
Let $B$ be the base body length; $C$ the current cargo-capacity
budget; $R$ the number of equipped cargo modules; $P$ the number of
carried packages; $S$ the unlocked passive-slot count; and $A$ the
number of equipped passive perks.

- #strong[Free package capacity at any moment:] $C - R - P$.
- #strong[Maximum deliverable cargo:] $C - R$.
- #strong[Snake length during play:] $B + R + P$.
- #strong[Passive limit:] $0 <= A <= S <= 2$.
- #strong[Cargo limit (initial proposal):] $0 <= R <= C - 2$ to keep at least
  two slots usable for deliveries; $0 <= P <= C - R$.
- #strong[At full cargo:] $B + R + (C - R) = B + C$.

Thus, equipping modules reduces delivery throughput, while the
#emph[maximum possible physical length stays the same] at a given
capacity budget. Modules also make the snake longer before it collects
its first package. Deliveries remove packages, not modules. This avoids
the unintended advantage of powerful perks making maximum-length
navigation easier.

#figure(
  [
    #text(
      size: 9pt,
      weight: "semibold",
    )[Capacity budget $C = 6$, body length $B = 3$; all rows at full package capacity.]
    #v(6pt)
    #text(size: 8pt)[No cargo modules: $R = 0$, $P = 6$, length $B + R + P = 9$]
    #grid(
      columns: (1fr,) * 6,
      gutter: 3pt,
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
    )
    #v(5pt)
    #text(size: 8pt)[One cargo module: $R = 1$, $P = 5$, length $B + R + P = 9$]
    #grid(
      columns: (1fr,) * 6,
      gutter: 3pt,
      cap-tile("MODULE"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
    )
    #v(5pt)
    #text(size: 8pt)[Two cargo modules: $R = 2$, $P = 4$, length $B + R + P = 9$]
    #grid(
      columns: (1fr,) * 6,
      gutter: 3pt,
      cap-tile("MODULE"),
      cap-tile("MODULE"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
    )
  ],
  caption: [Strong perks displace packages but do not change the maximum snake length],
)

#strong[Hard cap for mobile.] Set an upper limit on $C$ using device
playtests: readable segments, thumb occlusion, turning radius, and
typical obstacle density. Permanent and in-run capacity upgrades must
respect that same cap. Illustrative progression: 4 starting
package-capacity budget, eventually 6–7; this is not a validated value.

#strong[Avoid self-cancelling perks.] A module that "grants +1 capacity"
but reserves one package slot has no net cargo benefit. A coin-earning
cargo perk must increase #emph[coins per minute];, not merely coins per
parcel, enough to justify more trips. It is acceptable for some loadouts
to specialize in survival rather than throughput, as long as the
trade-off is explicit.

== Example starter perk pool
<example-starter-perk-pool>
#figure(
  align(center)[#table(
    columns: 4,
    align: (col, row) => (auto, auto, auto, auto).at(col),
    inset: 6pt,
    [Perk],
    [Category],
    [Effect under consideration],
    [Trade-off / balance
      concern],

    [Tip Jar],
    [Passive],
    [+10% base coins per successful delivery],
    [Economy bonus should remain modest; no runaway stacking],

    [Route Scanner],
    [Passive],
    [Highlights a useful matching destination],
    [Reduce confusion without fully solving routing],

    [Assisted Turning],
    [Passive],
    [Small steering assistance on difficult turns],
    [Never change expected input unpredictably],

    [Package Magnet],
    [Cargo],
    [Attracts nearby packages within a small radius],
    [Can pull in unwanted cargo; must respect capacity],

    [Safety Shield],
    [Cargo],
    [Negates one eligible collision, then recharges by delivery progress],
    [Avoid endless invulnerability and trivial boss fights],

    [Sorter Module],
    [Cargo],
    [Helps select or organize matching parcels for a stop],
    [Must save enough route time to offset lower capacity],

    [Express Engine],
    [Cargo],
    [Short speed burst after a successful delivery],
    [Speed may make turning harder; add clear control and limits],
  )],
)

#strong[Initial balancing rule:] every cargo perk reserves #strong[one]
package slot and uses #strong[zero passive perk slots]. Two-slot modules
are deferred until playtesting supports the additional cost. Cargo
modules remain attached throughout the run; at least two package slots
remain available for deliveries. The loadout UI separates "Passive
perks (1/2)" from "Cargo capacity (modules + parcels / total)", and
shows each module's effective delivery-capacity cost.

= Meta loop, shop, and progression
<meta-loop-shop-and-progression>
== Meta loop
<meta-loop>
After a run, the results screen shows successful deliveries, boss
progress, coin income, career XP earned, level progress, and completed
challenge objectives. The player can inspect daily challenges, use the
free Mail Room shift or a replay ticket, buy permanent PowerUps, unlock
perks or skins, modify the loadout, or start another Snake run. Mail
sorting is optional; career level affects its salary band and does not
block normal gameplay.

#figure(
  [
    ```mermaid
    flowchart TD
      HOME["Home + career XP / rank"] --> DAILY["Challenges + free mail shift"]
      HOME --> SHOP["Shop: PowerUps, perks, tiers, cosmetics"]
      HOME --> LOAD["Choose owned starter perks"]
      DAILY --> MAIL["Sorting + replay tickets"]
      DAILY --> COINS["Persistent coin wallet"]
      MAIL --> COINS
      MAIL --> XP["Career XP / level"]
      SHOP --> LOAD
      COINS --> SHOP
      LOAD --> RUN["Play delivery run"]
      RUN --> RESULTS["Results: deliveries, coins, XP"]
      RESULTS --> COINS
      RESULTS --> XP
      XP --> HOME
      RESULTS --> HOME
    ```
  ],
  caption: [Both play modes feed one coin wallet and one career XP track],
)

#figure(
  align(center)[#table(
    columns: 3,
    align: (col, row) => (auto, auto, auto).at(col),
    inset: 6pt,
    [Shop category], [Purchase behavior], [Example],
    [Permanent PowerUps],
    [Repeatable ranks; always active unless explicitly disabled],
    [Starting capacity budget; starting lives],

    [Starter perk and module unlocks], [One-time unlock; then equip for free], [Package Magnet],
    [Eligible perk / module tiers],
    [Optional higher ranks for an owned ability; buy with coins],
    [Package Magnet Tier II, Tip Jar Tier II],

    [Second passive perk slot], [One-time meta unlock], [Equip up to two passive perks],
    [Cosmetics], [One-time, no gameplay advantage], [Snake and parcel skins],
    [Extra lives],
    [Prefer a permanent starting-lives upgrade],
    [Optional run consumables only if they add a clear choice],

    [Daily challenges], [Earned reward source; not a paid purchase], [Complete deliveries under a cargo restriction],
    [Mail sorting + replay tickets],
    [One free shift daily; earned, ad, or optional IAP ticket for more],
    [Coins and small career XP earned each completed shift],

    [Career level and postal rank],
    [Persistent XP progression; no coin spending to level up],
    [Promotions change mail salary; level awards may include cosmetics/tickets],
  )],
)

#strong[Shop rules \(proposal).] Rank prices increase with rank, visibly
and predictably. A simple model is
$p(r) = p_0 (1 + d(r - 1))$, rounded to an
appropriate increment, where $p_0$ is the base price, $d$ the rank-price
step and $r$ the rank being purchased. Allow free redistribution/refunding of stat ranks
between runs; perk and cosmetic unlocks remain owned. Avoid a hidden
order-dependent surcharge. Keep all gameplay PowerUps earnable with
ordinary play.

#strong[Permanent upgrade examples:] starting capacity, starting lives,
optional small starting shield, or a modest delivery-income upgrade.
Starting capacity should have few ranks and a hard mobile-friendly cap.
Do not sell upgrades that make navigation or visibility worse by
default.

== Upgrade tiers for selected perks and modules
<upgrade-tiers-for-selected-perks-and-modules>
The shop can sell #strong[higher tiers of some already-unlocked passive
  perks and cargo modules]; with the same persistent coins used for
PowerUps. Ownership and strength are separate: unlock the ability once,
then optionally purchase its next tier. The highest owned tier applies
automatically when equipped. #strong[Equipping an owned tier is free];,
and an unequipped perk or module provides no bonus.

Tier upgrades preserve the existing loadout economy:

- A passive perk uses #strong[one passive perk slot] at every tier.
- A cargo module reserves #strong[one package slot] at every tier and
  remains a visible, non-deliverable snake segment.
- Higher tiers improve the ability itself; they do not grant additional
  loadout slots, increase the cargo cap, or reduce a module's slot cost
  unless a distinct mechanic is explicitly designed and tested.
- No perk or module is required to have a tier ladder. Start with short,
  finite ladders and evaluate each rank on its own merit.
- Tier purchases remain owned between runs. Refundability of these
  purchases is still a design choice, separate from refundable stat
  PowerUp ranks. Players can always unequip without a fee.

#figure(
  [
    ```mermaid
    flowchart LR
      EARN["Deliveries and daily rewards"] --> WALLET["Coin wallet"]
      WALLET --> UNLOCK["Unlock chosen perk / module"]
      UNLOCK --> T1["Tier I owned"]
      T1 --> CHOOSE{"Useful upgrade ladder?"}
      CHOOSE -->|Yes| T2["Buy Tier II"]
      T2 --> T3["Optional Tier III"]
      CHOOSE -->|No| KEEP["Keep Tier I only"]
      T1 --> EQUIP["Equip between runs"]
      T2 --> EQUIP
      T3 --> EQUIP
      KEEP --> EQUIP
      EQUIP --> PASS["Passive: 1 perk slot"]
      EQUIP --> CARGO["Cargo: 1 package slot"]
      classDef coin fill:#313244,stroke:#F9E2AF,color:#CDD6F4
      classDef shop fill:#313244,stroke:#89B4FA,color:#CDD6F4
      classDef cost fill:#313244,stroke:#A6E3A1,color:#CDD6F4
      class EARN,WALLET coin
      class UNLOCK,T1,CHOOSE,T2,T3,KEEP,EQUIP shop
      class PASS,CARGO cost
    ```
  ],
  caption: [Optional tier purchases change ability strength, not its equipped slot type],
)

#strong[Candidate tier ladders, not approved balance values]

#figure(
  table(
    columns: (1.1fr, 0.6fr, 2.5fr, 1.5fr),
    inset: 5pt,
    [Ability], [Type], [Illustrative Tier I / II / III], [Initial assessment],
    [Package Magnet],
    [Cargo],
    [Pickup radius: base / +15% / +30%; no collection at full capacity],
    [Good candidate; noticeable effect with a clear cap],

    [Tip Jar], [Passive], [Base delivery coins: +10% / +12% / +15%], [Possible; avoid compounding income inflation],
    [Safety Shield],
    [Cargo],
    [Recharge after 5 / 4 / 3 successful deliveries],
    [Conditional; test boss and hazard survivability],

    [Express Engine],
    [Cargo],
    [Same speed; slightly longer boost or shorter cooldown],
    [Conditional; don't make steering harder],

    [Route Scanner],
    [Passive],
    [Useful destination highlight at Tier I; no tiers specified],
    [Weak candidate; clarity may belong in the base UI],

    [Assisted Turning],
    [Passive],
    [One tested, accessible steering behavior; no tier ladder],
    [Avoid; input reliability should not be monetized],

    [Sorter Module], [Cargo], [One clear sorting utility; tiers undecided], [Weak until sorting has meaningful depth],
  ),
  caption: [Candidate progression and ability-by-ability triage; numbers are illustrative],
)

#strong[Tier quality test.] A rank should offer a difference players can
notice, without removing the interesting decision created by the
ability. Compare each candidate against a same-price PowerUp or an
entirely new perk. An upgrade is weak when it merely fixes poor
baseline usability, gives an invisible statistical increment, or
trivializes package collection, routing, steering, or collisions. In
particular, stronger cargo modules must still justify the package slot
they reserve; test delivered packages per minute, coin income per
minute, and survival by tier. Keep UI affordances and accessibility
features fully usable at Tier I.

#strong[Shop presentation.] For an owned upgradable ability, show its
current tier, exact next-tier effect, coin price, and maximum tier.
Show "Max tier" at the cap; show "No upgrades" for single-tier
abilities. Tier prices should increase predictably and be set per
ability rather than using a universal hidden surcharge. All gameplay
tiers remain obtainable through ordinary play; paid currency and
rewarded ads are optional ways to acquire coins, not exclusive access
to stronger perks.

#strong[Daily challenges \(proposal).] Use clear gameplay goals: finish
a run with no cargo perk, deliver packages to three shape types, defeat
a boss, or deliver a set number without taking damage. Give a modest,
fixed coin payout or cosmetics. Do not require ad viewing or purchases.
Avoid challenges that need an event the player cannot reasonably
trigger. Daily mail sorting is a separate activity; an optional daily
challenge may ask for participation but should not require a top-tier
score.

= Career progression and postal ranks
<career-progression-and-postal-ranks>
== Global level and XP
<global-level-and-xp>
The game has one #strong[persistent career level] separate from the
current Snake run. Successful deliveries earn career XP, while coins
remain the spendable shop currency. In-run delivery milestones still
trigger temporary upgrades and bosses; XP never substitutes for those
milestones. Career level is shared with the Mail Room and determines its
salary band. Spending coins does not spend or reset XP.

#strong[Established curve, proposed application.] Use the exact
_Old School RuneScape_ cumulative experience curve as the prototype
level table:

#align(center)[#box(
  inset: 8pt,
  fill: ctp.surface0.rgb,
  radius: 4pt,
)[#text(size: 9pt)[
  $ X(L) = floor((sum_(k=1)^(L-1) floor(k + 300 dot 2^(k/7))) / 4) $
]]]

Here $X(L)$ is the total career XP needed to reach level $L$.

This is an established published formula, not a claim that RuneScape's
pacing is appropriate for this mobile game. The distribution of XP
#emph[per activity], level cap, and unlock milestones are new tuning
parameters. Retain the exact integer formula in one shared progression
function; avoid approximating levels independently in UI and server.

#figure(
  align(center)[#table(
    columns: (1fr, 1.3fr, 2.7fr),
    inset: 5pt,
    [Level], [Cumulative XP], [Progression significance],
    [1], [0], [Starting career],
    [2], [83], [Early onboarding],
    [5], [388], [First rank promotion],
    [10], [1,154], [Next salary band],
    [20], [4,470], [Established midgame],
    [35], [22,406], [Highest prototype salary band],
    [50], [101,333], [Proposed initial career cap],
  )],
  caption: [Verified RuneScape cumulative XP thresholds; our activity XP is provisional],
)

At 20 XP per delivered package, and #emph[ignoring all bonus XP],
level 5 requires about 20 packages, level 10 about 58, level 20 about
224, level 35 about 1,121, and level 50 about 5,067. These are
transparent pacing implications of the proposed XP award, not
measured retention or completion targets. Boss, daily quest and mail
XP will reduce the actual package counts.

#strong[Proposed XP sources.] Keep XP amounts independent of coin
multipliers, Tip Jar, and paid coin packs. A delivered package awards
20 career XP; an on-time package adds 5; a defeated boss awards 80;
a completed daily challenge awards 40; a mail shift awards
$M(S) = floor(min(S, 40)/5)$ XP, where $S$ is the number of
correctly sorted envelopes. Mail therefore awards at most
8 XP per 30-second shift, versus 20 XP for a successful Snake
delivery. Every scored shift, including ticket replays, earns this small
XP amount. No XP is granted for an unplayed shift or a purchase.

#strong[Career-level effects.] A promotion changes the displayed
rank, awards modest one-time milestone items such as a cosmetic or an
earnable shift ticket, and updates the mail salary band. Selected shop
inventory may become visible at milestone levels, but a level alone
does not grant paid perks, increase snake capacity, or strengthen an
equipped module. Shop purchases and tier upgrades still use coins.

== Career rank and sorting salary
<career-rank-and-sorting-salary>
These #strong[provisional] ranks and salary multipliers are deliberately
stepped: promotions are easy to read, older players receive more coins
per shift, and the sorting controls and score thresholds remain
identical across career levels.

#figure(
  align(center)[#table(
    columns: (1.3fr, 1fr, 1.4fr, 1.6fr),
    inset: 5pt,
    [Postal rank], [Career levels], [Mail multiplier], [Purpose],
    [Trainee], [1--4], [1.00x], [Early learning],
    [Junior Courier], [5--9], [1.15x], [First promotion],
    [Courier], [10--19], [1.30x], [Midgame salary],
    [Senior Courier], [20--34], [1.50x], [Long-term progression],
    [Postmaster], [35--50], [1.75x], [Initial maximum band],
  )],
  caption: [Proposed salary bands; the curve and XP thresholds are established, but these multipliers are not],
)

A rank is fixed at #strong[shift start] for both difficulty and
reward calculation. If a shift itself earns enough XP to promote the
player, the new salary applies starting with the following shift.
This prevents inconsistent results when the same score is awarded at
slightly different points in a level-up transition.

#figure(
  [
    ```mermaid
    flowchart LR
      DEL["Deliver parcels"] --> XP["Persistent career XP"]
      BOSS["Boss victory / daily quest"] --> XP
      MAIL["Completed mail shift"] --> XP
      XP --> LV["Career level"]
      LV --> RANK["Postal salary band"]
      RANK --> PAY["More coins per mail shift"]
      PAY --> SHOP["Shop unlocks / perk tiers"]
      SHOP --> DEL
      classDef grow fill:#313244,stroke:#A6E3A1,color:#CDD6F4
      classDef econ fill:#313244,stroke:#F9E2AF,color:#CDD6F4
      class DEL,BOSS,MAIL,XP,LV,RANK grow
      class PAY,SHOP econ
    ```
  ],
  caption: [Career XP remains distinct from the spendable coin wallet],
)

= Daily mail-sorting minigame
<daily-mail-sorting-minigame>
== Game rules and mobile swipe input
<game-rules-and-mobile-swipe-input>
The Mail Room is a short, repeatable skill challenge accessible from
Home after the player knows the package destination symbols. Its
reference is the mail job on Dragon Roost Island in _The Legend of
Zelda: The Wind Waker_: #strong[30 seconds and six destination symbols],
with notable performance thresholds at 10, 20, and 25 correctly
sorted letters. This game's own characters, artwork and package stamps
should remain distinct. The documented Wind Waker rules are a
reference for the task and thresholds, #emph[not] proof of comfortable
swipe performance on phones.

- Show one envelope with a visible destination shape. Six large,
  labeled bins use the same visual language as the Snake deliveries.
- To sort, #strong[touch the envelope, swipe toward a bin, and release
    to throw]. The gesture previews the target bin before release.
  The next envelope appears immediately after the throw resolves.
- Wrong bin: an unmistakable rejection animation and haptic response;
  the envelope is discarded, with no coin fine or additional point.
  Time spent on the error is the primary cost.
- A 30-second timer continues between throws. The round ends cleanly
  at zero and credits coins and career XP for that shift. Do not place
  forced advertisements, store prompts, or random hazards in the
  timed activity.
- There is #strong[no practice mode]. Every started shift is either
  the player's daily free shift or uses one shift ticket, and every
  finished shift is eligible for its ordinary skill-based payout.

#figure(
  [
    #align(center)[#text(size: 11pt, weight: "bold", fill: ctp.mauve.rgb)[MAIL ROOM] #h(16pt) #text(
        size: 9pt,
      )[00:21 left  |  14 correct]]
    #v(5pt)
    #grid(
      columns: (1fr,) * 3,
      gutter: 5pt,
      sorting-bin("Circle"), sorting-bin("Triangle"), sorting-bin("Square"),
      sorting-bin("Star"), sorting-bin("Diamond"), sorting-bin("Cross"),
    )
    #v(8pt)
    #align(center)[#text(fill: ctp.yellow.rgb, size: 8.5pt)[SWIPE UP TOWARD THE MATCHING BIN]]
    #v(4pt)
    #rect(width: 100%, fill: ctp.surface0.rgb, stroke: ctp.yellow.rgb, radius: 5pt, inset: 10pt)[
      #align(center)[#text(weight: "semibold")[ENVELOPE: TRIANGLE] #text(
          fill: ctp.subtext1.rgb,
          size: 8pt,
        )[  (drag / flick to throw)]]
    ]
    #v(5pt)
    #align(center)[#text(
      size: 8pt,
      fill: ctp.subtext1.rgb,
    )[Active throw shows the candidate bin before finger release.]]
  ],
  caption: [Six-bin mobile wireframe; all throws are swipe gestures, not bin taps],
)

#strong[Targeting implementation.] Start with a short directional flick
from the envelope; the gesture's displacement maps to a previewed bin
in a responsive six-target aim zone. Release commits the currently
highlighted bin. Use the full two-dimensional swipe direction and
magnitude, rather than an overlapping one-dimensional ray that would
make upper and lower bins ambiguous. Preserve the same aim mapping
throughout a shift and never change bins unpredictably on release.
The 3-by-2 rack above is only a layout prototype: if it creates
ambiguous or uncomfortable flicks, space bins around the envelope
with distinct directions without altering the six-symbol rules.

Allow a brief, skippable instructions card #emph[before] the first
shift, but no playable practice round. Keep a pause/resume policy for
interruptions, so a phone call does not consume a paid ticket without
an opportunity to resume. Use large hit areas as a starting point:
Apple recommends at least 44-by-44 pt controls, and Android recommends
at least 48-by-48 dp with adequate spacing. These minimums do not
automatically guarantee fast gesture accuracy; test on small devices.

== Scoring and level-adjusted rewards
<scoring-and-level-adjusted-rewards>
#strong[Performance score:] $S$ is the number of correctly sorted
envelopes during the 30-second shift. A wrong throw does not increment
$S$. Keep the raw score for personal best and leaderboards. For economy
purposes, use $Q = min(S, 40)$ as a #emph[per-shift scoring ceiling], not a daily
earnings cap. Forty correct throws in 30 seconds would already require
more than one correct throw per second; validate this ceiling against
high-skill player data before locking it.

The currency function is a tunable adaptation of Wind Waker's
milestone-based compensation. It retains 10, 20 and 25 as reference
thresholds and introduces a 30-sort stretch goal. All numbers in this
formula are #strong[proposed, not proven] for this game:

#align(center)[#box(inset: 8pt, fill: ctp.surface0.rgb, radius: 4pt)[
  #text(size: 9pt)[
    $
      g(S) & = floor(Q/2) + 5 I(S >= 10) + 10 I(S >= 20) \
           & + 10 I(S >= 25) + 10 I(S >= 30)
    $
  ]
  #v(3pt)
  #text(size: 9pt)[$ c(S,L) = floor(g(S) dot m(r(L))) $]
]]

Here $I(A)$ equals 1 when condition $A$ is true and 0 otherwise;
$g(S)$ is the base coin payout, $r(L)$ is the postal rank
snapshotted at the start of the shift, $m(r(L))$ is its salary multiplier,
and $c(S,L)$ is the shift's total coin payout. Every completed
shift earns its own coins. There is #strong[no daily maximum, no
  best-score-only award, and no practice payout category]. A score of
zero yields zero coins. Career XP also accrues on each shift according
to $M(S)$, defined in the career progression section. A daily leaderboard and personal
best may track performance but do not suppress repeat rewards.

#figure(
  align(center)[#table(
    columns: (1fr, 1.2fr, 1fr, 1fr, 1fr),
    inset: 5pt,
    [Correct sorts], [Score label], [Trainee coins], [Courier coins], [Postmaster coins],
    [10], [Bronze], [10], [13], [17],
    [20], [Silver], [25], [32], [43],
    [25], [Gold], [37], [48], [64],
    [30], [Platinum], [50], [65], [87],
    [40], [Exceptional], [55], [71], [96],
  )],
  caption: [Illustrative paid-per-shift salary at 1.00x, 1.30x and 1.75x; no daily ceiling],
)

#figure(
  [
    #text(fill: ctp.subtext1.rgb, size: 8.5pt)[Coins for 30 correctly sorted envelopes in one shift]
    #v(5pt)
    #grid(
      columns: (9em, 1fr, 2.2em),
      column-gutter: 7pt,
      [Trainee], rect(width: 50%, height: 9pt, radius: 2pt, fill: ctp.blue.rgb), [50],
    )
    #v(4pt)
    #grid(
      columns: (9em, 1fr, 2.2em),
      column-gutter: 7pt,
      [Junior Courier], rect(width: 57%, height: 9pt, radius: 2pt, fill: ctp.sapphire.rgb), [57],
    )
    #v(4pt)
    #grid(
      columns: (9em, 1fr, 2.2em),
      column-gutter: 7pt,
      [Courier], rect(width: 65%, height: 9pt, radius: 2pt, fill: ctp.teal.rgb), [65],
    )
    #v(4pt)
    #grid(
      columns: (9em, 1fr, 2.2em),
      column-gutter: 7pt,
      [Senior Courier], rect(width: 75%, height: 9pt, radius: 2pt, fill: ctp.green.rgb), [75],
    )
    #v(4pt)
    #grid(
      columns: (9em, 1fr, 2.2em),
      column-gutter: 7pt,
      [Postmaster], rect(width: 87%, height: 9pt, radius: 2pt, fill: ctp.yellow.rgb), [87],
    )
  ],
  caption: [Career progression increases each completed shift's salary without changing its scoring rules],
)

== Daily shift, replay tickets and reward flow
<daily-shift-replay-tickets-and-reward-flow>
Grant #strong[one free reward-bearing shift per day]. Further shifts
require a #strong[shift ticket], acquired through playing the main
Snake game (for example a daily challenge or a level milestone),
optionally through a voluntarily watched rewarded ad, or through an
optional IAP ticket bundle. Tickets are consumables, #emph[not]
practice passes. There is no cap on currency earned across shifts in
a day; daily availability restricts only the free grant and the
frequency of ad-sourced tickets. Purchased and earned tickets can be
used on the same day without devaluing their rewards.

#strong[Do not sell tickets for soft coins at launch.] If a shift could
cost fewer coins than it reliably pays back, purchasing another shift
would create a repeatable wallet-minting loop. Keeping tickets as
play-earned items, ad rewards or transparent IAP consumables avoids
that particular currency arbitrage. It still requires comparison with
IAP coin packs; see below.

#figure(
  [
    ```mermaid
    flowchart TD
      HOME["Open Mail Room"] --> FREE{"Today's free shift unused?"}
      FREE -->|Yes| START["Reserve free shift"]
      FREE -->|No| TICKET{"Shift ticket available?"}
      TICKET -->|Yes| SPEND["Reserve one ticket"]
      TICKET -->|No| GET["Earn ticket from gameplay / opt-in ad / optional IAP"]
      GET --> TICKET
      SPEND --> START
      START --> SWIPE["30-second swipe-to-bin sorting"]
      SWIPE --> SCORE["Record correct sorts and mistakes"]
      SCORE --> COIN["Credit coins for this shift"]
      SCORE --> XP["Credit small amount of career XP"]
      XP --> LEVEL["Update career level and next-shift salary"]
      COIN --> RESULT["Results: score, salary, coin credit, XP"]
      LEVEL --> RESULT
      RESULT --> HOME
      classDef play fill:#313244,stroke:#89B4FA,color:#CDD6F4
      classDef reward fill:#313244,stroke:#A6E3A1,color:#CDD6F4
      classDef opt fill:#45475A,stroke:#F9E2AF,color:#CDD6F4
      class HOME,START,SWIPE,SCORE,RESULT play
      class COIN,XP,LEVEL reward
      class FREE,TICKET,SPEND,GET opt
    ```
  ],
  caption: [Every shift is scored and rewarded; no practice branch or daily gold cap],
)

#strong[Consumption and interruption.] The free claim or ticket is
reserved when the shift actually starts, not when the Mail Room opens.
An OS/app interruption should resume the same in-progress shift when
possible; an unrecoverable interruption has a defined refund policy.
An attempt has a unique identifier, snapshots career salary at start,
and credits its payout only once. Persist free-day use and ticket
balances across devices using server/account timestamps when online.
Offline-first play needs locally persisted pending claims and explicit
reconciliation rules; client clock changes cannot mint free shifts.

== Paid replay economics and anti-arbitrage
<paid-replay-economics-and-anti-arbitrage>
The previous 50-coins-per-day cap is removed. For real-money replays,
price tickets against #strong[the largest possible payout at the
  highest salary band], not only the average player's score. Otherwise
a highly skilled player can acquire coins through replay bundles more
cheaply than through coin packs. Set the values together rather than
independently.

#align(center)[#box(inset: 8pt, fill: ctp.surface0.rgb, radius: 4pt)[
  #text(size: 9pt)[
    $
      (N dot G_"max"(L)) / P_"tickets"
      <= alpha (G_"pack" / P_"pack")
    $
  ]
  #v(3pt)
  #text(
    size: 8pt,
    fill: ctp.subtext1.rgb,
  )[$alpha = 0.80$ is a proposed economy safety margin, not an industry standard.]
]]

$N$ is the number of replay tickets, $G_"max"(L)$ the maximum coins
from one shift at level $L$, and $P_"tickets"$ the price of that ticket
bundle. $G_"pack"$ and $P_"pack"$ are the currency quantity and price
of the direct coin pack.

This is a guaranteed-value comparison, not a prediction of revenue.
For example, #emph[if both bundles cost the same] and the direct coin
pack contains 500 coins, a bundle of four tickets must produce at most
400 coins at the highest rank under this prototype rule. The highest
mail payout is 96 per shift at the 40-sort scoring ceiling, so
$4 dot 96 = 384 <= 400$. At 30 correct sorts it pays
$4 dot 87 = 348$. Thus the ticket bundle adds a short skill activity
without systematically offering better coin conversion than buying
the equivalent coin pack. #emph[500 coins, 4 tickets and both prices
  being equal are illustrative comparisons, not live store offers.]

#figure(
  align(center)[#table(
    columns: (2fr, 1fr, 2.3fr),
    inset: 5pt,
    [Provisional equivalent-price offer], [Maximum coin value], [Interpretation],
    [Direct coin pack], [500], [Straight currency purchase],
    [4 ticket replays at top salary], [384], [At most 76.8% of coin-pack value],
    [4 replays at 30 sorts each], [348], [69.6% of coin-pack value],
    [First free shift], [Up to 96], [No IAP, contributes to daily retention],
  )],
  caption: [Bounded per-shift output lets paid replay remain rewarding without undercutting a coin pack],
)

#strong[Guardrails:] if coin packs, prices, the rank salary table or
score ceilings change, rerun the inequality across #emph[all] actual
regional store prices and offers, not just the example. Avoid a
level-gated paid ticket that delivers an inferior reward to a player
at the same skill. Clearly display the exact ticket quantity and
coin rewards to expect from the score. Never sell 'unlimited replays'
without an economy model for the resulting coin supply.

A #strong[rewarded ad] may optionally award one ticket before the
next round, instead of adding a flat +10 coins after mail results.
Limit the #emph[ad offer's frequency] for user experience and provider
availability, not by secretly limiting coins from purchased or earned
shifts. The player can always refuse an ad; no watched ad is required
to collect shift earnings. Keep banners and interstitials out of
sorting gameplay and out of its result-to-replay tap sequence.

== Cross-system balance and telemetry
<cross-system-balance-and-telemetry>
Career XP and mail coins use separate functions. Tip Jar and cargo
modules modify Snake only; paid perks do not improve sorting accuracy,
round time or salary. This keeps the Mail Room's score skill-based.
Mail rewards share the persistent wallet and may purchase regular
shop items, including tier upgrades. They never advance the run's
package counter or trigger bosses and randomized upgrade selections.

Measure correct-sorts distribution, missed throws, per-bin error
rates, sorted-envelope rate, interruption frequency, salary-band
income, tickets earned and spent, ad-origin tickets, replay purchase
frequency, coins per minute in Snake versus Mail Room, and the share
of each career rank's total coins supplied by mail. Test the low-
percentile player as well as the skilled tail. The score ceiling and
salary bands should be adjusted using real observed distributions;
the 30-second and six-symbol reference alone cannot establish a
balanced mobile payout.

= Random run upgrades and random events
<random-run-upgrades-and-random-events>
== In-run upgrade selection
<in-run-upgrade-selection>
Milestones based on #strong[successful deliveries] give one of several
random upgrade choices. These cost no persistent coins. Offer three
distinct options when possible and prevent duplicate choices that have
already reached their cap.

#figure(
  align(center)[#table(
    columns: 4,
    align: (col, row) => (auto, auto, auto, auto).at(col),
    inset: 6pt,
    [Upgrade], [Category], [Example effect], [Rules],
    [Extra Heart], [Run-persistent], [+1 life or maximum health], [Persists until run ends; capped],
    [Cargo Capacity], [Run-persistent], [+1 package slot within hard cap], [Cannot exceed mobile maximum],
    [Speed Surge], [Timed], [Faster movement briefly], [A risk/reward choice; clearly show timer],
    [Shield], [Timed], [Short invulnerable window or one absorbed hit], [Cannot make bosses indefinitely safe],
    [Wind Grip], [Timed], [Reduces wind displacement], [Useful counter when fan events exist],
    [Delivery Bonus], [Run-persistent], [Minor coin or scoring gain], [Limited stacking; watch economy inflation],
  )],
)

#strong[Upgrade cadence \(proposal).] Early choices should appear
quickly enough to teach the system. Later intervals can stretch to keep
decisions meaningful. The run-upgrade selection should pause the game,
with generous touch targets and no ads.

== Environmental random events
<environmental-random-events>
A successful delivery can trigger an event roll. In an endless run,
interpret "after a percentage of deliveries" as a #strong[chance per
  eligible delivery];, not as a percentage of a nonexistent fixed total.
If the game later has fixed-length levels, use progress checkpoints
instead. Event selection, probability, cooldown, and active-event cap
are tunable data.

#figure(
  align(center)[#table(
    columns: 4,
    align: (col, row) => (auto, auto, auto, auto).at(col),
    inset: 6pt,
    [Event from the sketch], [Intended effect], [Example duration], [Counterplay and mobile fairness],
    [Hole / coffee-mug stain],
    [A new dangerous area or inaccessible region],
    [Run-long until cleaned or run end],
    [Telegraph footprint; preserve a viable route],

    [Earthquake / bumped table],
    [Briefly shifts the playable field or obstacles],
    [Short, timed],
    [Warning; prevent immediate unavoidable deaths],

    [Fan / extreme wind],
    [Displaces or biases snake movement],
    [Timed],
    [Visible wind direction; limit strength near tight corridors],

    [Boss threat],
    [Persistent hazard with a defeat condition],
    [Until boss is defeated],
    [Clear health/progress, safe telegraphs, meaningful victory reward],
  )],
)

#figure(
  [
    ```mermaid
    stateDiagram-v2
      [*] --> Normal
      Normal --> EventRoll: Successful delivery
      EventRoll --> Normal: No event
      EventRoll --> Warning: Event selected
      Warning --> Timed: Fan / earthquake (short)
      Warning --> Persistent: Hole / changed terrain (run-long)
      Timed --> Normal: Timer expires
      Persistent --> Normal: Cleanup condition
      Normal --> Boss: Delivery threshold reached
      Timed --> Boss: Threshold reached, defer hazard if needed
      Persistent --> Boss: Threshold reached
      Boss --> Normal: Boss defeated
      Normal --> End: No lives left
      Timed --> End: No lives left
      Persistent --> End: No lives left
      Boss --> End: No lives left
      End --> [*]
    ```
  ],
  caption: [Timed hazards, persistent obstacles, and boss lifetime],
)

#strong[Fairness constraints.] Telegraph before applying
control-disrupting effects; cap simultaneous severe hazards; pause/defer
a disruptive event during menus or an unskippable tutorial. Do not allow
a random event to permanently block all delivery destinations. At a boss
threshold, start or queue the boss so an unrelated extreme event cannot
create an unwinnable situation. The boss is a persistent challenge and
can be defeated, rather than a randomly expiring hazard.

= Currency economy
<currency-economy>
== Coins earned from deliveries
<coins-earned-from-deliveries>
Use #strong[one soft currency: coins];. It persists across runs and
funds permanent PowerUps, perk/module unlocks and eligible tier upgrades,
the second passive slot, and
earnable cosmetics. Some shift tickets can be earned by playing and
may also be offered through an opt-in ad or optional IAP; they are not
bought with coins under the prototype model. Coins and career XP are
separate. The run itself uses a #strong[delivery counter] to award
randomized upgrades and trigger bosses; players never spend coins
to choose normal in-run upgrades.

#strong[Coin sources:] each successful package delivery; optional
on-time bonus; a modest delivery streak; boss victory; daily challenge
completion; and each completed mail-sorting shift (with career-rank salary).
Package pickup alone pays nothing. Ordinary score can track
survival/performance without being spendable currency.

#strong[Illustrative run payout]

#figure(
  align(center)[#table(
    columns: 3,
    align: (col, row) => (auto, right, right).at(col),
    inset: 6pt,
    [Source], [Example calculation], [Coins],
    [Delivered packages], [6 packages x 10], [60],
    [On-time bonuses], [2 x 5], [10],
    [Delivery streak], [1 completed streak], [10],
    [Boss victory], [1 victory], [50],
    [#strong[Total before optional rewards];], [], [#strong[130];],
  )],
)

The Tip Jar’s +10% example could apply only to the #strong[60
  base-delivery coins];, adding 6. This limits multipliers from stacking
with boss and challenge payouts. Values should be recalibrated against
desired time to first purchase and per-run earnings distributions.

#figure(
  [
    ```mermaid
    flowchart LR
      DEL["Successful deliveries"] --> PAY["Persistent coin wallet"]
      BONUS["On-time / streak / boss / challenge bonuses"] --> PAY
      MAIL["Mail sorting - rewarded each shift"] --> PAY
      PAY --> POWER["Permanent PowerUp ranks"]
      XP["Deliveries / mail sorting: career XP"] --> LEVEL["Career level + mail salary"]
      LEVEL --> MAIL
      PAY --> PERKS["Perk unlocks, tiers, and second passive slot"]
      PAY --> SKINS["Earnable skins; optional lives consumables"]
      POWER --> BETTER["Better next run"]
      PERKS --> BETTER
      SKINS --> BETTER
      BETTER --> DEL
      RESULTS["Optional rewarded ad at results"] -->|"Explicit claim only"| PAY
      IAP["Optional IAP: cosmetics, no-ads, coin packs"] -->|"Coin packs only"| PAY
      classDef src fill:#313244,stroke:#A6E3A1,color:#CDD6F4
      classDef sink fill:#313244,stroke:#89B4FA,color:#CDD6F4
      classDef external fill:#45475A,stroke:#F9E2AF,color:#CDD6F4
      class DEL,BONUS,BETTER src
      class PAY,POWER,PERKS,SKINS,LEVEL sink
      class XP src
      class RESULTS,IAP external
    ```
  ],
  caption: [Wallet income, shop spending, and the daily sorting reward],
)

== Wallet, earning, and spending rules
<wallet-earning-and-spending-rules>
- Credit successful delivery earnings to the persistent wallet
  immediately \(with a durable save/ledger), while also displaying a
  running "earned this run" tally. Death does not remove credited coins.
- A single transaction must be credited once even if the app suspends,
  restarts, retries a callback, or shows the same results screen twice.
- Validate purchases against wallet balance; deduct coins and grant the
  item atomically. Permanent purchases remain after death or reinstall
  if account/cloud saving supports restoration.
- Completing a challenge grants its reward once; store a claimed flag.
  Sorting awards the completed shift exactly once, with a
  distinct attempt ID and snapshot salary band. One free shift is
  granted each day; tickets authorize additional fully rewarded shifts.
  Avoid unlimited daily-quest rerolls that manufacture currency.
- Show the #strong[next rank’s price];, current balance, and effect
  before purchase. Show the current and next tier for each eligible
  perk or module. Once unlocked or upgraded, equipping it never
  incurs another fee.

#strong[Pricing approach.] Keep the first meaningful purchase reachable
after a handful of successful deliveries. Price later capacity/life
ranks higher than simple utility perks, and treat the second passive slot as a
visible progression milestone. Prices and payouts should be tuned
together using median and lower-percentile performance, not top-player
earnings alone.

== Economy and cargo trade-off
<economy-and-cargo-trade-off>
Bigger deliveries need a reason to be attractive. A possible
#emph[bulk-routing bonus] rewards several correctly delivered parcels in
one stop, but must not outweigh the danger of controlling a long snake.
For example, reward completing a route without collision or delivering
multiple matching packages to one address rather than paying for
collecting/hoarding alone.

Track #strong[coins per minute] by loadout, not only coins per parcel.
Measure mail-sorting coins and XP separately because the short
paid or earned-ticket shifts can distort progression and coin income
if analyzed together with Snake runs. A
one-slot Cargo Magnet might increase pickup speed while also increasing
delivery frequency because it can carry less. If its measured income is
lower, improve its utility or reduce the perk’s cost. A survival module
may earn fewer coins per minute while increasing average run duration;
that is a valid strategic trade-off.

= Monetization placement
<monetization-placement>
#strong[General principle.] Active Snake controls require uninterrupted
attention. Gameplay-affecting ads, storefront pop-ups, and banners
should stay out of an active run. Free players must retain a viable
progression path; paid offers and advertisements supplement rather than
replace play-earned coins.

#figure(
  [
    ```mermaid
    flowchart TD
      HOME["Home / shop / loadout"] -->|"IAP storefront; optional banner in safe zone"| PLAY["Start run"]
      HOME --> MAIL["Daily free sorting shift / ticket replay"]
      MAIL --> MAILRESULT["Mail result: coins + career XP"]
      MAILRESULT --> HOME
      MAILRESULT --> TICKET["Optional rewarded ad: gain a shift ticket"]
      TICKET --> MAIL
      PLAY --> GAME["Active snake controls, deliveries, events, boss"]
      GAME --> DEATH["Run finished: results screen"]
      DEATH --> OPT{"Player selects optional reward?"}
      OPT -->|Yes| REWARD["Rewarded ad: disclosed coin bonus or revive"]
      OPT -->|No| BREAK["Natural transition to home"]
      REWARD --> BREAK
      BREAK --> GATE{"Eligible for interstitial?"}
      GATE -->|"No: cooldown / premium / new player"| HOME
      GATE -->|"Yes: limited frequency"| AD["Dismissible interstitial"]
      AD --> HOME
      GAME -.-> NEVER["No banners, forced ads, or shops during active control"]
      classDef good fill:#313244,stroke:#A6E3A1,color:#CDD6F4
      classDef warn fill:#45475A,stroke:#F9E2AF,color:#CDD6F4
      classDef bad fill:#45475A,stroke:#F38BA8,color:#CDD6F4
      class HOME,PLAY,GAME,DEATH,OPT,BREAK,GATE good
      class REWARD,AD,TICKET warn
      class NEVER bad
    ```
  ],
  caption: [Ads and purchases at opt-in or natural transitions],
)

#figure(
  align(center)[#table(
    columns: 4,
    align: (col, row) => (auto, auto, auto, auto).at(col),
    inset: 6pt,
    [Format], [Recommended placement], [Proposal], [Avoid],
    [In-app purchases \(IAP)],
    [Home shop, cosmetic tab, Mail Room ticket purchase, optional post-run shop],
    [Cosmetic bundles, no-ads, optional coin packs and mail shift tickets],
    [Forced offers on death; direct purchase inside active steering],

    [Rewarded video],
    [Run results bonus/revive; opt-in shift ticket offer outside sorting],
    [Disclosed coin bonus or revive; mail offer grants a replay ticket],
    [Auto-play, undisclosed rewards, forcing an ad to keep earned coins],

    [Interstitial \("pop-up") ad],
    [After the results screen, at a natural transition to home],
    [Controlled, dismissible frequency; skip if player just watched
      rewarded ad],
    [Mid-run, at spawn, during upgrades, immediately before control
      returns],

    [Banner ad],
    [Home / non-interactive menu area only; never in daily sorting],
    [Optional anchored banner with non-clickable separation],
    [Gameplay HUD, steering zones, near Start/Shop buttons],

    [In-game offer pop-up],
    [Optional store tab or infrequent post-run prompt],
    [Inform about cosmetics or sale],
    [Surprise purchase dialog on every loss],
  )],
)

== Rewarded ad offers
<rewarded-ad-offers>
- #strong[Coins:] At run results, "Watch an ad for +25% of coins earned
  this run", capped at a tunable fixed amount, claimed at most once per run.
  Example (rounding to the nearest whole coin):
  $min(floor(0.25 dot 130 + 0.5), 50) = 33$. Preserve the initial 130
  coins whether the offer is accepted or skipped.
- #strong[Revive:] At the point of defeat, optionally offer one revive
  per run with clear conditions. The game is frozen while the choice/ad
  is shown. A revive should restore a predictable safe state, not
  respawn into unavoidable death.
- Show the exact reward before opt-in. If no ad is available, continue
  normally; never leave the player stuck. Reward grants must be
  idempotent; ad-unavailable or network-failure paths cannot erase
  earnings.
- Mail Room rewarded advertising, if enabled, grants one replay
  ticket after an explicit opt-in, not a flat coin bonus. A spent ticket
  permits a fully rewarded shift. Frequency-limit the ad offer for
  usability without applying a coin cap to other shift sources.
- Consider offering only one of these rewards in a session transition to
  reduce ad pressure. A purchased "no forced ads" entitlement should
  still allow optional rewarded ads if explicitly disclosed.

== Interstitial and banner restrictions
<interstitial-and-banner-restrictions>
#strong[Conservative initial cadence \(proposal):] no forced
interstitials during the tutorial or first several runs; after that, at
most one after a completed results screen when #strong[both] a multi-run
counter and a multi-minute cooldown are satisfied. Suggested test
parameters: at least three runs and four minutes since the last forced
interstitial, with neither value treated as a platform-mandated rule.
Suppress a forced interstitial after an opted-in rewarded ad and when
the player owns the no-ads entitlement.

Google Play prohibits unexpected full-screen ads during gameplay or at
the beginning of a gameplay segment. Google AdMob describes
interstitials as suitable at natural transitions, and warns against
banners near continuous interaction. Apple’s App Review Guidelines
require ads to be identifiable, dismissible, and appropriate for the
app’s audience. These are platform constraints, not justification to
fill every permitted placement. \[1–4\]

#strong[IAP compliance.] Digital items, game currency, and ad-removal
purchases generally use the platform’s in-app purchase system under
applicable rules and regional exceptions. Purchased coins should not
expire on iOS; restoring non-consumable entitlements needs a clear path.
\[3, 5\]

#strong[Audience warning.] The monetization design assumes a general
audience. A children-directed game or an App Store Kids Category release
can materially change whether third-party ads are suitable or allowed;
review age-rating, privacy, consent, and platform requirements before
introducing ad SDKs. \[3\]

= Mobile interface and pacing
<mobile-interface-and-pacing>
#figure(
  align(center)[#table(
    columns: 3,
    align: (col, row) => (auto, auto, auto).at(col),
    inset: 6pt,
    [Screen], [Primary actions], [UX requirement],
    [Home],
    [Start, Mail Room, Career Level, Daily Challenges, Shop],
    [Career XP, rank and wallet visible without intrusive overlays],

    [Shop],
    [Buy PowerUp ranks, unlock or upgrade perks/modules, buy skins, open IAP],
    [Show owned and next tier, effect, cost, and separate career rank requirements],

    [Loadout], [Equip passive perks and cargo modules], [Separate passive-slot and package-capacity indicators],
    [Mail Room],
    [Swipe an envelope toward a destination bin; 30-second round],
    [Six legible targets, previewed throw aim, no in-round ads],

    [Mail results],
    [View correct sorts, career salary, earned coins/XP, ticket balance],
    [Every shift pays; no daily coin cap or practice mode],

    [Gameplay], [Steer, read destinations and cargo, deliver], [No ad overlays; legible targets and collision states],
    [Upgrade picker], [Choose one of three randomized upgrades], [Pause gameplay; large touch targets; durations shown],
    [Events / boss HUD], [Hazard warning, timer, boss progress], [Communicate risk before affecting steering],
    [Results],
    [Deliveries, coins, milestones, optional ad reward],
    [Earned coins already safe; ad reward clearly optional],
  )],
)

#strong[Onboarding order \(proposal).] First teach pick-up and growth;
then show matching addresses and shrinking; then reveal capacity and the
reason to deliver; then award the first coins and show a starter
purchase. Unlock the Mail Room after the player recognizes at least
two delivery shapes. Show the career XP bar and postal rank after the
first package delivery, then explain the daily free shift and replay
tickets once the Mail Room becomes available. Introduce random upgrades after the core delivery concept is
understood. Initially keep the loadout simple, then unlock the second passive
slot after the player understands how cargo modules reduce delivery capacity.

#strong[Touch control considerations.] The safe capacity limit must be
tested on small devices with one-handed play. Hazard displacement,
automatic magnetic pickups, and speed boosts need special attention
because they affect precise steering. Warnings should use spatial cues
and readable timers, not text-only alerts.

= Balance, testing, and implementation invariants
<balance-testing-and-implementation-invariants>
== Prototype parameters
<prototype-parameters>
#figure(
  align(center)[#table(
    columns: 3,
    align: (col, row) => (auto, right, auto).at(col),
    inset: 6pt,
    [Parameter], [Initial illustrative value], [Test criterion],
    [Base body length ($B$)], [3 segments], [Visibility and responsiveness],
    [Starting capacity budget ($C$)], [4 packages], [Can new players route without gridlock?],
    [Upper capacity budget], [6–7 packages], [Still controllable on small screens?],
    [Passive perk slots], [1; unlockable 2], [Does the second passive slot create interesting builds?],
    [Module cost], [1 package slot; no passive slot], [Is throughput reduction meaningful?],
    [Minimum parcel slots], [2 remaining after modules], [Can every loadout complete deliveries?],
    [Base delivery payout], [10 coins / delivered package], [Time to first permanent upgrade],
    [Boss reward], [50 coins \(example)], [Risk and completion time],
    [Rewarded results bonus], [+25%, cap 50], [Avoid ad dominating progression],
    [Mail sorting duration / bins], [30 seconds / 6], [Wind Waker reference; mobile swipes must be tested],
    [Free mail shift / day], [1], [Every extra shift uses a ticket; no practice],
    [Mail payout ceiling / shift], [55 base; 96 at Postmaster], [Per-shift anti-arbitrage, no daily coin cap],
    [Career XP / delivery], [20], [Compare level pacing across player cohorts],
    [Career XP / mail shift], [$M(S)$; see career XP formula], [Sorting supplements the Snake career level],
  )],
)

== Measurement plan
<measurement-plan>
Measure deliveries per minute, banked coins per minute, snake length
distribution, collision rate by length, survivability by loadout,
career-level pacing, mail sorting score and wrong-swipe distributions,
skipped upgrade choices, boss outcomes, hazard-related losses, session
length, and the share of runs ending at full cargo. For monetization,
also measure ad opt-in, interstitial exposure, abandonment after ads,
purchase conversion, and progression for non-paying players. Compare
lower-percentile, median, and skilled player cohorts.

#strong[Key invariants for engineering and QA]

- Package capacity cannot exceed the mobile safety cap. Enforce
  $R + P <= C$, $R <= C - 2$, and $A <= S <= 2$ after loadout or upgrade
  changes. Reject any mid-run capacity reduction that would break these
  limits; do not detach an equipped module mid-run.
- Cargo modules are fixed segments; delivered parcels are removable.
  Equipping cargo modules never creates a shorter maximum-length snake
  than equipping none.
- Random run upgrades are removed at run end; owned perks and modules,
  their purchased tiers, stat ranks, skins, and wallet balance persist.
  Tier strength does not change an equipped perk's or module's slot cost.
- Coins never double-award on duplicated delivery events, daily
  shift completion, replay tickets, reward callbacks, ad callbacks,
  or results re-entry. Each shift earns the full performance payout
  exactly once; no daily coin ceiling applies. Under current prototype
  values, the per-shift currency maximum is 96 at the top career rank.
- Damage, upgrades, event selection, and ads cannot run concurrently in
  ways that leave the snake moving behind a full-screen modal.
- Every random event maintains a valid path to an attainable destination
  or explicitly offers a safe fallback.
- Offline play and ad load failure do not block core gameplay, normal
  earnings, or navigation back to home.

== Remaining design questions for playtests
<remaining-design-questions-for-playtests>
- How do multiple simultaneously held destination shapes display cleanly
  on a small screen, and does the player choose which parcel to deliver?
- Should the boss victory conclude a level or allow an endless delivery
  run to continue? The sketch currently suggests continuing.
- Which hazards create enjoyable dexterity challenges without generating
  unfair touchscreen deaths?
- How quickly should the second passive slot unlock, and when does
  reserving multiple package slots for modules outperform carrying more parcels?
- Which perks/modules genuinely benefit from tiers? Test Package Magnet
  and Tip Jar first, watch Safety Shield and Express Engine for balance
  problems, and leave unclear steering/sorting upgrades single-tier
  until a useful progression exists.
- Does the economy need consumable lives at all? A permanent
  starting-lives PowerUp may cover the intended need more cleanly.
- Can players reliably swipe to six targets on small phones at
  20+ correct sorts per shift? Test target layout, direction mapping
  and 30-second threshold distribution.
- Do career salary multipliers and ticket-bundle prices remain viable
  across all levels and regions without making paid replays a cheaper
  substitute for coin packs? Recalculate when prices change.

= Policy and technical sources
<policy-and-technical-sources>
The monetization placement rules and gesture-target recommendations
are grounded in the following documentation consulted on
#strong[3 November 2026];. Rules can change before release.

+ Google Play, #emph[Ads policy];:
  #link("https://support.google.com/googleplay/android-developer/answer/9857753")
+ Google AdMob, #emph[Interstitial ads for Unity];:
  #link("https://developers.google.com/admob/unity/interstitial")
+ Apple, #emph[App Review Guidelines];:
  #link("https://developer.apple.com/app-store/review/guidelines/")
+ Google AdMob, #emph[Banner ad guidance];:
  #link("https://support.google.com/admob/answer/6128877")
+ Google Play, #emph[Payments policy];:
  #link("https://support.google.com/googleplay/android-developer/answer/9858738")
+ Google AdMob, #emph[Rewarded ads for Unity];:
  #link("https://developers.google.com/admob/unity/rewarded")
+ Apple Developer, #emph[UI Design Dos and Don'ts];:
  #link("https://developer.apple.com/design/tips/")
+ Android Accessibility Help, #emph[Touch target size];:
  #link("https://support.google.com/accessibility/android/answer/7101858")
+ Old School RuneScape Wiki, #emph[Experience formula and level table];:
  #link("https://oldschool.runescape.wiki/w/Experience")

#emph[Compilation:] This file has no local image, diagram, or data-file
imports. The official `@preview/catppuccin:1.1.0` package supplies the
Mocha page, typography and accent palette. Mermaid flowcharts render directly
through `@preview/merman:0.5.0`; Typst 0.15.0 or newer is required.
Run `typst compile Parcel_Snake_Catppuccin_Mocha_Typst_Math.typ`
after the packages are available in your Typst environment.

#emph[Inspiration and tooling references:]
- Zelda Dungeon, _The Wind Waker mail-sorting minigame_:
  #link("https://www.zeldadungeon.net/wiki/Mail_Sorting")
- Zelda Wiki, _Mail Center minigame_:
  #link("https://www.zeldawiki.wiki/wiki/Mail_Center_%28Mini-Game%29")
- Typst Universe, _merman package_:
  #link("https://typst.app/universe/package/merman/")
- Catppuccin, _official Typst theme package_:
  #link("https://typst.app/universe/package/catppuccin/")
