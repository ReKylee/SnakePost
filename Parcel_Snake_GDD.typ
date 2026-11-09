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
  title: [Snake Post: complete game design specification],
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
  let tint = if kind == "PERK" { ctp.peach.rgb } else { ctp.green.rgb }
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
*Core premise.* A mobile Snake-style delivery game. The snake
collects parcels from residents, grows one segment per parcel, and
delivers each parcel to an address identified by the corresponding
shape. Successful deliveries remove their parcel segments, grant coins,
and advance the run. A finite carrying capacity keeps the snake
controllable on a phone.

*Decisions already established*

- Parcels make the snake longer; correctly delivered parcels make it
  shorter. Colliding with a hazard or the snake's own body removes one
  carried Parcel segment without awarding a delivery.
- When no deliverable Parcels remain, an empty-cargo countdown runs.
  Collecting a new Parcel resets the timer and stops the countdown;
  expiration ends the run. Having zero Parcels is not immediate defeat.
- Capacity is bounded. Longer snakes are harder to steer on a
  touchscreen.
- The meta loop includes a shop, upgrade ownership and Upgrade Tiers, a starting
  loadout, daily challenges, Career XP and Postal Ranks, and PostShift replay tickets.
- PostShift is a 30-second swipe-to-bin activity outside
  the Snake run. One shift is free per day; additional reward-bearing
  shifts require an earned, ad-granted, or purchased shift ticket.
  There is no practice mode and no fixed daily coin cap.
- Players initially have one *passive perk slot* and can unlock a second.
- Passive perks each consume one passive slot and no capacity.
  Cargo Perks *only* reserve capacity slots, one per equipped Perk; they
  do not consume passive perk slots. Every equipped Cargo Perk appears as a
  visible, non-deliverable segment throughout its run.
- Eligible Upgrades can have additional Upgrade Tiers, purchased with coins
  when offered in the shop. Which upgrades merit multiple tiers is a balance
  decision; higher tiers do not consume additional equipment slots by default.
- Random events can last briefly or persist through a run. Boss threats
  persist until defeated.

*Terminology and content model.* *Parcel* is a deliverable item;
*Capacity* is the bounded number of physical slots shared by carried
parcels and equipped Cargo Perks. An *Upgrade*
is acquirable content composed of one or more *Effects*, whose activation
and lifetime are defined by that content. An *Upgrade Tier* is a strength
or behavior stage of the same Upgrade, including repeatable stat upgrades.
A *Perk* is an equippable Upgrade; *Passive Perk* and *Cargo Perk* describe
only its equipment rules, not different Upgrade or Effect implementations.
The shop, a delivery milestone, or another reward source can grant
Upgrades through the same content model. Grant source does not determine
Effect lifetime. A purchased unlock or Upgrade Tier can persist between
runs while an activated Effect expires according to its configuration.
*PostShift* names the sorting minigame; a *shift* is one played attempt.
*Career XP* determines numerical *Career Level*; *Postal Rank* is the
named promotion band used for PostShift salary.

*Proposed details to test:* exact coin payouts, upgrade prices,
empty-cargo countdown duration, collision recovery window, maximum
safe snake length, random-event cadence, boss frequency, ad frequency,
and PostShift score thresholds.
The established sources for the
30-second / six-symbol sorting task and the XP curve are identified below;
our payout and monetization parameters are _illustrative_, not
empirically validated balance targets.

#figure(
  [
    ```mermaid
    flowchart TB
      subgraph META["Between runs"]
        HOME["Home + Career Level"] --> SHOP["Shop and upgrades"]
        HOME --> POSTSHIFT["Daily free PostShift / ticket replays"]
        POSTSHIFT --> WALLET["Earn coins every shift"]
        POSTSHIFT --> XP["Gain Career XP"]
        XP --> RANK["Career Level + salary band"]
        RANK --> POSTSHIFT
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
      PROGRESS --> END["Run ends if empty-cargo countdown expires"]
      END --> HOME
      END --> WALLET
      END --> XP
      classDef meta fill:#313244,stroke:#89B4FA,color:#CDD6F4
      classDef run fill:#313244,stroke:#A6E3A1,color:#CDD6F4
      class HOME,SHOP,POSTSHIFT,WALLET,XP,RANK,LOAD,END meta
      class START,COLLECT,DELIVER,PROGRESS run
    ```
  ],
  caption: [Core and meta loops, with a PostShift branch],
)

= Core loop
<core-loop>
== Flow of an active run
<flow-of-an-active-run>
+ *Start.* Spawn at a random valid location with owned starting Effects
  and selected Perks. The base body and attached Cargo Perks determine
  starting length. With no Parcel collected yet, the empty-cargo
  countdown starts immediately.
+ *Collect.* Pick up a Parcel from a resident if capacity remains.
  Its destination shape/address and removable body segment are added.
  Every valid pickup resets the countdown to its full duration and
  deactivates it while at least one Parcel is carried.
+ *Route and deliver.* Find matching addresses. Deliver compatible
  Parcels, remove their segments, award coins, and advance delivery
  progress. If the last Parcel is delivered, start a fresh countdown.
+ *Progress.* Delivery milestones may offer randomized Upgrades, roll
  for a telegraphed event, or trigger a persistent boss encounter.
+ *Survive.* Bumping into a hazard, boss attack, or the snake's own body
  removes one carried Parcel without a payout. If that was the last
  Parcel, start the countdown; if already empty, there is no further
  Parcel to lose. Collect a new Parcel before time expires or show
  results. There is no lives pool.

#figure(
  [
    ```mermaid
    flowchart TD
      SPAWN["Start empty: countdown running"] --> CHOICE{"Collect or deliver?"}
      CHOICE -->|Collect| PICKUP["Pickup adds Parcel; reset and stop countdown"]
      PICKUP --> CHOICE
      CHOICE -->|Deliver| DELIVER["Deliver matching Parcels; remove segments"]
      DELIVER --> PAY["Credit coins and delivery progress"]
      PAY --> CHECK["Milestone, event roll, boss threshold"]
      CHECK --> UP["Optional Upgrade choice"]
      CHECK --> EVENT["Optional telegraphed hazard"]
      CHECK --> BOSS["Optional persistent boss"]
      CHECK --> STATUS{"Any Parcels left?"}
      UP --> STATUS
      EVENT --> STATUS
      BOSS --> STATUS
      PICKUP -.-> HIT["Hazard, boss, or self-collision"]
      EVENT -.-> HIT
      BOSS -.-> HIT
      HIT --> HELD{"Parcel carried?"}
      HELD -->|Yes| DROP["Drop and lose one Parcel; no delivery reward"]
      DROP --> STATUS
      HELD -->|No| EMPTY["Empty-cargo countdown running"]
      STATUS -->|Yes| CHOICE
      STATUS -->|No| EMPTY
      EMPTY -->|Pickup before expiry| PICKUP
      EMPTY -->|Timer expires| RESULTS["End run; keep earned coins"]
      classDef action fill:#313244,stroke:#89B4FA,color:#CDD6F4
      classDef danger fill:#45475A,stroke:#F38BA8,color:#CDD6F4
      class SPAWN,PICKUP,DELIVER,PAY,UP,RESULTS action
      class EVENT,BOSS,HIT,DROP,EMPTY danger
    ```
  ],
  caption: [Core delivery, upgrade, hazard, and boss loop],
)

*Destination rules \(proposal).* Each parcel stores its own
destination identity; a delivery consumes only matching parcels. A
destination may consume several matching parcels at once. UI must make
the carried parcels and their destinations readable without
forcing precision taps while steering. A full snake cannot collect more
parcels, but can still deliver and pursue objectives.

== Collision loss and empty-cargo countdown
<collision-loss-and-empty-cargo-countdown>
*Collision consequence.* An eligible bump into a hazard, obstacle,
boss attack, or the snake's own body drops and removes exactly one
carried Parcel segment. That Parcel is no longer deliverable and
awards no coins or delivery progress. The permanent body and equipped
Cargo Perks cannot be lost this way. Which Parcel is selected for loss,
and whether dropped Parcels could be recovered, remain playtest
decisions; the baseline treats the Parcel as lost from the run.
Use a brief collision recovery window so sustained overlap cannot
remove multiple Parcels in successive physics frames.

*Empty-cargo state.* The countdown begins at full duration whenever
the carried Parcel count becomes zero: at initial spawn, after the
last successful delivery, or after a collision loses the last Parcel.
An empty snake remains controllable and can collect a new Parcel.
Every valid pickup refills the countdown to its full duration and
deactivates it while any Parcel is carried. Further collisions while
empty cannot instantly end the run: only countdown expiry causes
ordinary defeat. Pause the countdown with gameplay-pausing menus.
The duration and warnings are balance parameters, not fixed here.

*Delivery versus failure.* Coins are earned on successful delivery,
not pickup or Parcel loss. Already banked coins persist if the
countdown expires; any undelivered Parcels are discarded at run end.
Show the empty-cargo countdown clearly and make losing a Parcel
visibly distinct from delivering it.

== Run progression and rewards
<run-progression-and-rewards>
#figure(
  align(center)[#table(
    columns: 3,
    align: (col, row) => (auto, auto, auto).at(col),
    inset: 6pt,
    [Trigger], [Player reward or consequence], [Lifetime],
    [Parcel collected], [Segment added; reset and deactivate countdown], [Until delivered, lost, or run ends],
    [Correct delivery], [Segment removed; coins awarded; timer starts if empty], [Coins persist across runs],
    [Hazard or self-collision], [Lose one carried Parcel; no delivery reward], [One loss per eligible bump],
    [Zero carried Parcels], [Start full empty-cargo countdown], [Until pickup or expiry],
    [Upgrade milestone], [Pick one of three random upgrades], [Timed or until run end],
    [Event roll], [Hole, earthquake, fan, or another environment modifier], [Timed or run-long],
    [Boss threshold], [New persistent challenge; boss-specific reward on victory], [Until defeated or run ends],
    [Empty-cargo timer expires], [Results, banked earnings, return to home], [Ends current run],
  )],
)

*Terminology rule:* the sketch's "permanent" _random_ rewards
are currently configured to expire at the end of the run. Purchased
Upgrade ownership and Upgrade Tiers persist between runs. The same
Upgrade model handles both acquisition routes; each Effect's configured
lifetime determines when it ends. Show that lifetime in the picker
(e.g., "Until end of run") rather than inferring it from the source.

= Starting loadout and capacity
<starting-loadout-and-capacity>
== Independent passive and capacity slots
<two-different-slot-types>
*Passive perk slots* limit the number of small, always-on bonuses
that may be equipped. *Capacity slots* represent physical capacity
for parcels _or_ Cargo Perks. A Cargo Perk reserves capacity
without using a passive slot; a passive perk uses a passive slot without
reserving capacity.

#figure(
  align(center)[#table(
    columns: 5,
    align: (col, row) => (auto, right, right, auto, auto).at(col),
    inset: 6pt,
    [Category],
    [Passive slots used],
    [Capacity slots reserved],
    [Physical
      segment],
    [Purpose],

    [Passive perk], [1], [0], [No], [Small, always-on utility or economy effect],
    [Cargo Perk], [0], [1], [Yes; cannot be delivered], [Strong ability paid for with delivery throughput],
  )],
)

Players start with *one passive perk slot*, with the option to
*permanently unlock a second* from the shop. They may also equip
owned Cargo Perks directly into their capacity. Owned perks
can be changed freely between runs; equipping them never
spends coins again. To preserve the delivery loop, the initial rule
proposal leaves *at least two slots for parcels*.

#figure(
  [
    #text(weight: "semibold")[Passive Perk: Tip Jar; Cargo Perk: Parcel Magnet]
    #v(5pt)
    #grid(
      columns: (1fr, 1fr),
      gutter: 7pt,
      rect(fill: ctp.surface0.rgb, stroke: ctp.blue.rgb, radius: 4pt, inset: 9pt)[
        *Tip Jar* \ No capacity cost
      ],
      rect(fill: ctp.surface0.rgb, stroke: ctp.peach.rgb, radius: 4pt, inset: 9pt)[
        *Parcel Magnet* \ Reserves 1 capacity slot
      ],
    )
    #v(6pt)
    #text(size: 9pt)[Starting capacity budget: 6; max deliverable parcels: 5; one Cargo Perk segment remains attached.]
  ],
  caption: [Independent selections: the passive occupies one passive slot, while the Cargo Perk reserves one capacity slot],
)

#figure(
  [
    ```mermaid
    flowchart TD
      SHOP["Unlock perks using coins"] --> PASS["Equip up to two passive perks"]
      SHOP --> CARGO_PERK["Equip Cargo Perks into capacity slots"]
      PASS --> PASSRULE["Each passive uses 1 passive slot; no capacity cost"]
      CARGO_PERK --> CARGO_PERK_RULE["Each Cargo Perk reserves 1 capacity slot; no passive slot"]
      CARGO_PERK_RULE --> SEGMENT["Visible during the run; never delivered"]
      PASSRULE --> RUN["Start run"]
      SEGMENT --> RUN
      RUN --> RULES["Compute free capacity and current snake length"]
      classDef regular fill:#313244,stroke:#89B4FA,color:#CDD6F4
      classDef cargoPerk fill:#45475A,stroke:#F9E2AF,color:#CDD6F4
      class SHOP,PASS,PASSRULE,RUN,RULES regular
      class CARGO_PERK,CARGO_PERK_RULE,SEGMENT cargoPerk
    ```
  ],
  caption: [Independent Passive Perk and Cargo Perk selections],
)

== Capacity and length invariants
<capacity-and-length-invariants>
Let $B$ be the base body length; $C$ the current capacity
limit; $R$ the number of equipped Cargo Perks; $P$ the number of
carried parcels; $S$ the unlocked passive-slot count; and $A$ the
number of equipped passive perks.

- *Free capacity at any moment:* $C - R - P$.
- *Maximum deliverable parcels:* $C - R$.
- *Snake length during play:* $B + R + P$.
- *Passive limit:* $0 <= A <= S <= 2$.
- *Cargo Perk limit (initial proposal):* $0 <= R <= C - 2$ to keep at least
  two slots usable for deliveries; $0 <= P <= C - R$.
- *At full capacity:* $B + R + (C - R) = B + C$.

Thus, equipping Cargo Perks reduces delivery throughput, while the
_maximum possible physical length stays the same_ at a given
capacity budget. Cargo Perks also make the snake longer before it collects
its first parcel. Deliveries remove parcels, not Cargo Perks. This avoids
the unintended advantage of powerful perks making maximum-length
navigation easier.

#figure(
  [
    #text(
      size: 9pt,
      weight: "semibold",
    )[Capacity budget $C = 6$, body length $B = 3$; all rows at full capacity.]
    #v(6pt)
    #text(size: 8pt)[No Cargo Perks: $R = 0$, $P = 6$, length $B + R + P = 9$]
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
    #text(size: 8pt)[One Cargo Perk: $R = 1$, $P = 5$, length $B + R + P = 9$]
    #grid(
      columns: (1fr,) * 6,
      gutter: 3pt,
      cap-tile("PERK"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
    )
    #v(5pt)
    #text(size: 8pt)[Two Cargo Perks: $R = 2$, $P = 4$, length $B + R + P = 9$]
    #grid(
      columns: (1fr,) * 6,
      gutter: 3pt,
      cap-tile("PERK"),
      cap-tile("PERK"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
      cap-tile("PARCEL"),
    )
  ],
  caption: [Cargo Perks displace parcels but do not change the maximum snake length],
)

*Hard cap for mobile.* Set an upper limit on $C$ using device
playtests: readable segments, thumb occlusion, turning radius, and
typical obstacle density. Starting and milestone-granted capacity Effects must
respect that same cap. Illustrative progression: 4 starting
capacity limit, eventually 6–7; this is not a validated value.

*Avoid self-cancelling perks.* A Cargo Perk that "grants +1 capacity"
but reserves one capacity slot offers no additional parcel space. A coin-earning
Cargo Perk must increase _coins per minute_, not merely coins per
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

    [Parcel Magnet],
    [Cargo Perk],
    [Attracts nearby parcels within a small radius],
    [Can pull in unwanted parcels; must respect capacity],

    [Safety Shield],
    [Cargo Perk],
    [Negates one eligible collision, then recharges by delivery progress],
    [Avoid endless invulnerability and trivial boss fights],

    [Parcel Sorter],
    [Cargo Perk],
    [Helps select or organize matching parcels for a stop],
    [Must save enough route time to offset lower capacity],

    [Express Engine],
    [Cargo Perk],
    [Short speed burst after a successful delivery],
    [Speed may make turning harder; add clear control and limits],
  )],
)

*Initial balancing rule:* every Cargo Perk reserves *one*
capacity slot and uses *zero passive perk slots*. Two-slot Cargo Perks
are deferred until playtesting supports the additional cost. Cargo Perks remain attached throughout the run; at least two capacity slots
remain available for deliveries. The loadout UI separates "Passive
perks (1/2)" from "Capacity (Cargo Perks + parcels / total)", and
shows each Cargo Perk's effective delivery-capacity cost.

= Meta loop, shop, and progression
<meta-loop-shop-and-progression>
== Meta loop
<meta-loop>
After a run, the results screen shows successful deliveries, boss
progress, coin income, Career XP earned, level progress, and completed
challenge objectives. The player can inspect daily challenges, use the
free shift or a replay ticket, buy Upgrade unlocks or tiers, unlock
cosmetics, modify the loadout, or start another Snake run. PostShift is optional; Career Level determines the Postal Rank
salary band and does not block normal gameplay.

#figure(
  [
    ```mermaid
    flowchart TD
      HOME["Home + Career XP / rank"] --> DAILY["Challenges + free shift"]
      HOME --> SHOP["Shop: upgrade unlocks, Upgrade Tiers, cosmetics"]
      HOME --> LOAD["Choose owned starter perks"]
      DAILY --> POSTSHIFT["Sorting + replay tickets"]
      DAILY --> COINS["Persistent coin wallet"]
      POSTSHIFT --> COINS
      POSTSHIFT --> XP["Career XP / level"]
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
  caption: [Both play modes feed one coin wallet and one Career XP track],
)

#figure(
  align(center)[#table(
    columns: 3,
    align: (col, row) => (auto, auto, auto).at(col),
    inset: 6pt,
    [Shop category], [Purchase behavior], [Example],
    [Starting-stat Upgrades],
    [Purchase the next authored Upgrade Tier; retain ownership],
    [Starting capacity; optional collision protection],

    [Perk unlocks], [One-time unlock; then equip for free], [Parcel Magnet],
    [Eligible Upgrade Tiers],
    [Optional next tier for an eligible owned Upgrade, purchased with coins],
    [Parcel Magnet Tier II, Tip Jar Tier II],

    [Second passive perk slot], [One-time meta unlock], [Equip up to two passive perks],
    [Cosmetics], [One-time, no gameplay advantage], [Snake and parcel skins],
    [Daily challenges], [Earned reward source; not a paid purchase], [Complete deliveries under a capacity restriction],
    [PostShift + replay tickets],
    [One free shift daily; earned, ad, or optional IAP ticket for more],
    [Coins and small Career XP earned each completed shift],

    [Career Level and Postal Rank],
    [Persistent XP progression; no coin spending to level up],
    [Promotions change PostShift salary; level awards may include cosmetics/tickets],
  )],
)

*Shop rules \(proposal).* Upgrade Tier prices increase predictably.
A simple model is $p(t) = p_0 (1 + d(t - 1))$, rounded to an appropriate
increment, where $p_0$ is the base price, $d$ the tier-price step and
$t$ the numeric index of the Upgrade Tier being purchased. Allow free
redistribution/refunding of eligible starting-stat Upgrade Tiers
between runs; Perk and cosmetic unlocks remain owned. Avoid a hidden
order-dependent surcharge. Keep all gameplay Upgrades earnable through
ordinary play.

*Starting-stat Upgrade examples:* starting capacity, optional
collision protection, or a modest delivery-income upgrade.
Starting capacity should have few Upgrade Tiers and a hard mobile-friendly cap.
Do not sell upgrades that make navigation or visibility worse by
default.

== Upgrade tiers
<upgrade-tiers>
Each Upgrade defines its available Upgrade Tiers; a single-tier Upgrade needs no further progression. The shop can sell the next
*Upgrade Tier* for eligible owned Upgrades, including Passive and
Cargo Perks, using the same persistent coin wallet. Ownership and tier
strength are separate: unlock the Upgrade once, then optionally buy its
next tier. For an equippable Perk, the highest owned tier is selected
when equipped. Equipping an owned Perk is free; an unequipped
Perk contributes no while-equipped Effects. Other Upgrades may grant
non-equipment Effects with their own configured lifetimes. These
categories do not require different effect implementations.

For equippable Perks, Upgrade Tier purchases preserve the loadout economy:

- A passive perk uses *one passive perk slot* at every tier.
- A Cargo Perk reserves *one capacity slot* at every tier and
  remains a visible, non-deliverable snake segment.
- Higher tiers improve the ability itself; they do not grant additional
  loadout slots, increase the capacity cap, or reduce a Cargo Perk's slot cost
  unless a distinct mechanic is explicitly designed and tested.
- An Upgrade may have only Tier I or additional Upgrade Tiers. Start
  with short, finite progressions and evaluate each tier on its own merit.
- Upgrade Tier purchases remain owned between runs. Refundability remains a design choice. The proposal allows refunding
  eligible starting-stat Upgrade Tiers, while Perk unlocks remain owned.
  Both use the same Upgrade and Upgrade Tier definitions. Players can always unequip without a fee.

#figure(
  [
    ```mermaid
    flowchart LR
      EARN["Deliveries and daily rewards"] --> WALLET["Coin wallet"]
      WALLET --> UNLOCK["Unlock chosen perk"]
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
      EQUIP --> CARGO_PERK["Cargo Perk: 1 capacity slot"]
      classDef coin fill:#313244,stroke:#F9E2AF,color:#CDD6F4
      classDef shop fill:#313244,stroke:#89B4FA,color:#CDD6F4
      classDef cost fill:#313244,stroke:#A6E3A1,color:#CDD6F4
      class EARN,WALLET coin
      class UNLOCK,T1,CHOOSE,T2,T3,KEEP,EQUIP shop
      class PASS,CARGO_PERK cost
    ```
  ],
  caption: [Optional Upgrade Tier purchases change ability strength, not its equipped slot type],
)

*Candidate Upgrade Tier progressions, not approved balance values*

#figure(
  table(
    columns: (1.1fr, 0.6fr, 2.5fr, 1.5fr),
    inset: 5pt,
    [Ability], [Type], [Illustrative Tier I / II / III], [Initial assessment],
    [Parcel Magnet],
    [Cargo Perk],
    [Pickup radius: base / +15% / +30%; no collection at full capacity],
    [Good candidate; noticeable effect with a clear cap],

    [Tip Jar], [Passive], [Base delivery coins: +10% / +12% / +15%], [Possible; avoid compounding income inflation],
    [Safety Shield],
    [Cargo Perk],
    [Recharge after 5 / 4 / 3 successful deliveries],
    [Conditional; test boss and hazard survivability],

    [Express Engine],
    [Cargo Perk],
    [Same speed; slightly longer boost or shorter cooldown],
    [Conditional; don't make steering harder],

    [Route Scanner],
    [Passive],
    [Useful destination highlight at Tier I; no tiers specified],
    [Weak candidate; clarity may belong in the base UI],

    [Assisted Turning],
    [Passive],
    [One tested, accessible steering behavior; no Upgrade Tier progression],
    [Avoid; input reliability should not be monetized],

    [Parcel Sorter], [Cargo Perk], [One clear sorting utility; tiers undecided], [Weak until sorting has meaningful depth],
  ),
  caption: [Candidate progression and ability-by-ability triage; numbers are illustrative],
)

*Upgrade Tier quality test.* An Upgrade Tier should offer a difference players can
notice, without removing the interesting decision created by the
ability. Compare each candidate against a same-price alternative Upgrade or an
entirely new Perk. An Upgrade Tier is weak when it merely fixes poor
baseline usability, gives an invisible statistical increment, or
trivializes parcel collection, routing, steering, or collisions. In
particular, stronger Cargo Perks must still justify the capacity slot
they reserve; test delivered parcels per minute, coin income per
minute, and survival by tier. Keep UI affordances and accessibility
features fully usable at Tier I.

*Shop presentation.* For an owned upgradable ability, show its
current tier, exact next-tier effect, coin price, and maximum tier.
Show "Max tier" at the cap; show "Single tier" for single-tier
abilities. Upgrade Tier prices should increase predictably and be set per
ability rather than using a universal hidden surcharge. All gameplay
tiers remain obtainable through ordinary play; paid currency and
rewarded ads are optional ways to acquire coins, not exclusive access
to stronger perks.

*Daily challenges \(proposal).* Use clear gameplay goals: finish
a run with no Cargo Perk, deliver parcels to three shape types, defeat
a boss, or deliver a set number without losing a Parcel to a collision. Give a modest,
fixed coin payout or cosmetics. Do not require ad viewing or purchases.
Avoid challenges that need an event the player cannot reasonably
trigger. Daily PostShift is a separate activity; an optional daily
challenge may ask for participation but should not require a top-tier
score.

= Career progression and Postal Ranks
<career-progression-and-postal-ranks>
== Global level and XP
<global-level-and-xp>
The game has one *persistent Career Level* separate from the
current Snake run. Successful deliveries earn Career XP, while coins
remain the spendable shop currency. In-run delivery milestones still
trigger temporary upgrades and bosses; XP never substitutes for those
milestones. Career Level is shared with PostShift and determines its
salary band. Spending coins does not spend or reset XP.

*Established curve, proposed application.* Use the exact
_Old School RuneScape_ cumulative experience curve as the prototype
level table:

#align(center)[#box(
  inset: 8pt,
  fill: ctp.surface0.rgb,
  radius: 4pt,
)[#text(size: 9pt)[
  $ X(L) = floor((sum_(k=1)^(L-1) floor(k + 300 dot 2^(k/7))) / 4) $
]]]

Here $X(L)$ is the total Career XP needed to reach level $L$.

This is an established published formula, not a claim that RuneScape's
pacing is appropriate for this mobile game. The distribution of XP
_per activity_, level cap, and unlock milestones are new tuning
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

At 20 XP per delivered parcel, and _ignoring all bonus XP_,
level 5 requires about 20 parcels, level 10 about 58, level 20 about
224, level 35 about 1,121, and level 50 about 5,067. These are
transparent pacing implications of the proposed XP award, not
measured retention or completion targets. Boss, daily quest and mail
XP will reduce the actual parcel counts.

*Proposed XP sources.* Keep XP amounts independent of coin
multipliers, Tip Jar, and paid coin packs. A delivered parcel awards
20 Career XP; an on-time parcel adds 5; a defeated boss awards 80;
a completed daily challenge awards 40; a shift awards
$M(S) = floor(min(S, 40)/5)$ XP, where $S$ is the number of
correctly sorted envelopes. PostShift therefore awards at most
8 XP per 30-second shift, versus 20 XP for a successful Snake
delivery. Every scored shift, including ticket replays, earns this small
XP amount. No XP is granted for an unplayed shift or a purchase.

*Career-level effects.* A promotion changes the displayed
rank, awards modest one-time milestone items such as a cosmetic or an
earnable shift ticket, and updates the PostShift salary band. Selected shop
inventory may become visible at milestone levels, but a level alone
does not grant paid perks, increase snake capacity, or strengthen an
equipped Cargo Perk. Shop purchases and Upgrade Tier purchases still use coins.

== Postal Rank and PostShift salary
<postal-rank-and-postshift-salary>
These *provisional* ranks and salary multipliers are deliberately
stepped: promotions are easy to read, older players receive more coins
per shift, and the sorting controls and score thresholds remain
identical across Career Levels.

#figure(
  align(center)[#table(
    columns: (1.3fr, 1fr, 1.4fr, 1.6fr),
    inset: 5pt,
    [Postal Rank], [Career Levels], [PostShift multiplier], [Purpose],
    [Trainee], [1--4], [1.00x], [Early learning],
    [Junior Courier], [5--9], [1.15x], [First promotion],
    [Courier], [10--19], [1.30x], [Midgame salary],
    [Senior Courier], [20--34], [1.50x], [Long-term progression],
    [Postmaster], [35--50], [1.75x], [Initial maximum band],
  )],
  caption: [Proposed salary bands; the curve and XP thresholds are established, but these multipliers are not],
)

A rank is fixed at *shift start* for both difficulty and
reward calculation. If a shift itself earns enough XP to promote the
player, the new salary applies starting with the following shift.
This prevents inconsistent results when the same score is awarded at
slightly different points in a level-up transition.

#figure(
  [
    ```mermaid
    flowchart LR
      DEL["Deliver parcels"] --> XP["Persistent Career XP"]
      BOSS["Boss victory / daily quest"] --> XP
      POSTSHIFT["Completed shift"] --> XP
      XP --> LV["Career Level"]
      LV --> RANK["Postal salary band"]
      RANK --> PAY["More coins per shift"]
      PAY --> SHOP["Shop unlocks / perk tiers"]
      SHOP --> DEL
      classDef grow fill:#313244,stroke:#A6E3A1,color:#CDD6F4
      classDef econ fill:#313244,stroke:#F9E2AF,color:#CDD6F4
      class DEL,BOSS,POSTSHIFT,XP,LV,RANK grow
      class PAY,SHOP econ
    ```
  ],
  caption: [Career XP remains distinct from the spendable coin wallet],
)

= PostShift minigame
<postshift-minigame>
== Game rules and mobile swipe input
<game-rules-and-mobile-swipe-input>
PostShift is a short, repeatable skill challenge accessible from
Home after the player knows the parcel destination symbols. Its
reference is the mail job on Dragon Roost Island in _The Legend of
Zelda: The Wind Waker_: *30 seconds and six destination symbols*,
with notable performance thresholds at 10, 20, and 25 correctly
sorted letters. This game's own characters, artwork and parcel stamps
should remain distinct. The documented Wind Waker rules are a
reference for the task and thresholds, _not_ proof of comfortable
swipe performance on phones.

- Show one envelope with a visible destination shape. Six large,
  labeled bins use the same visual language as the Snake deliveries.
- To sort, *touch the envelope, swipe toward a bin, and release
    to throw*. The gesture previews the target bin before release.
  The next envelope appears immediately after the throw resolves.
- Wrong bin: an unmistakable rejection animation and haptic response;
  the envelope is discarded, with no coin fine or additional point.
  Time spent on the error is the primary cost.
- A 30-second timer continues between throws. The round ends cleanly
  at zero and credits coins and Career XP for that shift. Do not place
  forced advertisements, store prompts, or random hazards in the
  timed activity.
- There is *no practice mode*. Every started shift is either
  the player's daily free shift or uses one shift ticket, and every
  finished shift is eligible for its ordinary skill-based payout.

#figure(
  [
    #align(center)[#text(size: 11pt, weight: "bold", fill: ctp.mauve.rgb)[POSTSHIFT] #h(16pt) #text(
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

*Targeting implementation.* Start with a short directional flick
from the envelope; the gesture's displacement maps to a previewed bin
in a responsive six-target aim zone. Release commits the currently
highlighted bin. Use the full two-dimensional swipe direction and
magnitude, rather than an overlapping one-dimensional ray that would
make upper and lower bins ambiguous. Preserve the same aim mapping
throughout a shift and never change bins unpredictably on release.
The 3-by-2 rack above is only a layout prototype: if it creates
ambiguous or uncomfortable flicks, space bins around the envelope
with distinct directions without altering the six-symbol rules.

Allow a brief, skippable instructions card _before_ the first
shift, but no playable practice round. Keep a pause/resume policy for
interruptions, so a phone call does not consume a paid ticket without
an opportunity to resume. Use large hit areas as a starting point:
Apple recommends at least 44-by-44 pt controls, and Android recommends
at least 48-by-48 dp with adequate spacing. These minimums do not
automatically guarantee fast gesture accuracy; test on small devices.

== Scoring and level-adjusted rewards
<scoring-and-level-adjusted-rewards>
*Performance score:* $S$ is the number of correctly sorted
envelopes during the 30-second shift. A wrong throw does not increment
$S$. Keep the raw score for personal best and leaderboards. For economy
purposes, use $Q = min(S, 40)$ as a _per-shift scoring ceiling_, not a daily
earnings cap. Forty correct throws in 30 seconds would already require
more than one correct throw per second; validate this ceiling against
high-skill player data before locking it.

The currency function is a tunable adaptation of Wind Waker's
milestone-based compensation. It retains 10, 20 and 25 as reference
thresholds and introduces a 30-sort stretch goal. All numbers in this
formula are *proposed, not proven* for this game:

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
$g(S)$ is the base coin payout, $r(L)$ is the Postal Rank
snapshotted at the start of the shift, $m(r(L))$ is its salary multiplier,
and $c(S,L)$ is the shift's total coin payout. Every completed
shift earns its own coins. There is *no daily maximum, no
  best-score-only award, and no practice payout category*. A score of
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
Grant *one free reward-bearing shift per day*. Further shifts
require a *shift ticket*, acquired through playing the main
Snake game (for example a daily challenge or a level milestone),
optionally through a voluntarily watched rewarded ad, or through an
optional IAP ticket bundle. Tickets are consumables, _not_
practice passes. There is no cap on currency earned across shifts in
a day; daily availability restricts only the free grant and the
frequency of ad-sourced tickets. Purchased and earned tickets can be
used on the same day without devaluing their rewards.

*Do not sell tickets for soft coins at launch.* If a shift could
cost fewer coins than it reliably pays back, purchasing another shift
would create a repeatable wallet-minting loop. Keeping tickets as
play-earned items, ad rewards or transparent IAP consumables avoids
that particular currency arbitrage. It still requires comparison with
IAP coin packs; see below.

#figure(
  [
    ```mermaid
    flowchart TD
      HOME["Open PostShift"] --> FREE{"Today's free shift unused?"}
      FREE -->|Yes| START["Reserve free shift"]
      FREE -->|No| TICKET{"Shift ticket available?"}
      TICKET -->|Yes| SPEND["Reserve one ticket"]
      TICKET -->|No| GET["Earn ticket from gameplay / opt-in ad / optional IAP"]
      GET --> TICKET
      SPEND --> START
      START --> SWIPE["30-second swipe-to-bin sorting"]
      SWIPE --> SCORE["Record correct sorts and mistakes"]
      SCORE --> COIN["Credit coins for this shift"]
      SCORE --> XP["Credit small amount of Career XP"]
      XP --> LEVEL["Update Career Level and next-shift salary"]
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

*Consumption and interruption.* The free claim or ticket is
reserved when the shift actually starts, not when PostShift opens.
An OS/app interruption should resume the same in-progress shift when
possible; an unrecoverable interruption has a defined refund policy.
An attempt has a unique identifier, snapshots PostShift salary at start,
and credits its payout only once. Persist free-day use and ticket
balances across devices using server/account timestamps when online.
Offline-first play needs locally persisted pending claims and explicit
reconciliation rules; client clock changes cannot mint free shifts.

== Paid replay economics and anti-arbitrage
<paid-replay-economics-and-anti-arbitrage>
The previous 50-coins-per-day cap is removed. For real-money replays,
price tickets against *the largest possible payout at the
  highest salary band*, not only the average player's score. Otherwise
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
For example, _if both bundles cost the same_ and the direct coin
pack contains 500 coins, a bundle of four tickets must produce at most
400 coins at the highest rank under this prototype rule. The highest
PostShift payout is 96 per shift at the 40-sort scoring ceiling, so
$4 dot 96 = 384 <= 400$. At 30 correct sorts it pays
$4 dot 87 = 348$. Thus the ticket bundle adds a short skill activity
without systematically offering better coin conversion than buying
the equivalent coin pack. _500 coins, 4 tickets and both prices
  being equal are illustrative comparisons, not live store offers._

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

*Guardrails:* if coin packs, prices, the Postal Rank salary table or
score ceilings change, rerun the inequality across _all_ actual
regional store prices and offers, not just the example. Avoid a
level-gated paid ticket that delivers an inferior reward to a player
at the same skill. Clearly display the exact ticket quantity and
coin rewards to expect from the score. Never sell 'unlimited replays'
without an economy model for the resulting coin supply.

A *rewarded ad* may optionally award one ticket before the
next round, instead of adding a flat +10 coins after PostShift results.
Limit the _ad offer's frequency_ for user experience and provider
availability, not by secretly limiting coins from purchased or earned
shifts. The player can always refuse an ad; no watched ad is required
to collect shift earnings. Keep banners and interstitials out of
sorting gameplay and out of its result-to-replay tap sequence.

== Cross-system balance and telemetry
<cross-system-balance-and-telemetry>
Career XP and PostShift coins use separate functions. Tip Jar and Cargo Perks modify Snake only; paid perks do not improve sorting accuracy,
round time or salary. This keeps PostShift scoring skill-based.
PostShift rewards share the persistent wallet and may purchase regular
shop items, including Upgrade Tier purchases. They never advance the run's
parcel counter or trigger bosses and randomized upgrade selections.

Measure correct-sorts distribution, missed throws, per-bin error
rates, sorted-envelope rate, interruption frequency, salary-band
income, tickets earned and spent, ad-origin tickets, replay purchase
frequency, coins per minute in Snake versus PostShift, and the share
of each Postal Rank's total coins supplied by PostShift. Test the low-
percentile player as well as the skilled tail. The score ceiling and
salary bands should be adjusted using real observed distributions;
the 30-second and six-symbol reference alone cannot establish a
balanced mobile payout.

= Milestone upgrades and random events
<milestone-upgrades-and-random-events>
== Run milestone upgrade selection
<run-milestone-upgrade-selection>
Milestones based on *successful deliveries* grant one Upgrade
from a randomized choice of three. This uses the same Upgrade and Effect
definitions as shop offerings; it is only a different acquisition route.
The current milestone pool offers Effects configured to end during or
at the end of the run, and these choices cost no persistent coins.
Avoid offering choices that already reached their configured tier cap.

#figure(
  align(center)[#table(
    columns: 4,
    align: (col, row) => (auto, auto, auto, auto).at(col),
    inset: 6pt,
    [Upgrade], [Effect lifetime], [Example effect], [Rules],
    [Capacity Increase], [Until run ends], [+1 capacity slot within hard cap], [Cannot exceed mobile maximum],
    [Speed Surge], [Timed], [Faster movement briefly], [A risk/reward choice; clearly show timer],
    [Shield], [Timed], [Short invulnerable window or one absorbed hit], [Cannot make bosses indefinitely safe],
    [Wind Grip], [Timed], [Reduces wind displacement], [Useful counter when fan events exist],
    [Delivery Bonus], [Until run ends], [Minor coin or scoring gain], [Limited stacking; watch economy inflation],
  )],
)

*Upgrade cadence \(proposal).* Early choices should appear
quickly enough to teach the system. Later intervals can stretch to keep
decisions meaningful. The milestone upgrade picker should pause the game,
with generous touch targets and no ads.

== Environmental random events
<environmental-random-events>
A successful delivery can trigger an event roll. In an endless run,
interpret "after a percentage of deliveries" as a *chance per
  eligible delivery*, not as a percentage of a nonexistent fixed total.
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
    [Warning; prevent unavoidable Parcel losses and countdown expiry],

    [Fan / extreme wind],
    [Displaces or biases snake movement],
    [Timed],
    [Visible wind direction; limit strength near tight corridors],

    [Boss threat],
    [Persistent hazard with a defeat condition],
    [Until boss is defeated],
    [Clear boss health/progress, safe telegraphs, meaningful victory reward],
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
      Normal --> End: Empty-cargo countdown expires
      Timed --> End: Empty-cargo countdown expires
      Persistent --> End: Empty-cargo countdown expires
      Boss --> End: Empty-cargo countdown expires
      End --> [*]
    ```
  ],
  caption: [Timed hazards, persistent obstacles, and boss lifetime],
)

*Fairness constraints.* The empty-cargo countdown runs independently
of environmental and boss states; those states cannot reset it.
Telegraph before applying control-disrupting effects; cap simultaneous
severe hazards; pause or defer a disruptive event during menus or an
unskippable tutorial. Do not allow
a random event to permanently block all delivery destinations. At a boss
threshold, start or queue the boss so an unrelated extreme event cannot
create an unwinnable situation. The boss is a persistent challenge and
can be defeated, rather than a randomly expiring hazard.

= Currency economy
<currency-economy>
== Coins earned from deliveries
<coins-earned-from-deliveries>
Use *one soft currency: coins*. It persists across runs
and funds Upgrade unlocks, eligible Upgrade Tier purchases, the second
passive slot, and earnable cosmetics. Some shift tickets can be earned by playing and
may also be offered through an opt-in ad or optional IAP; they are not
bought with coins under the prototype model. Coins and Career XP are
separate. The run itself uses a *delivery counter* to offer
randomized Upgrade choices and trigger bosses; players never spend coins
to choose milestone-offered Upgrades.

*Coin sources:* each successful parcel delivery; optional
on-time bonus; a modest delivery streak; boss victory; daily challenge
completion; and each completed shift (with career-Postal Rank salary).
Parcel pickup alone pays nothing. Ordinary score can track
survival/performance without being spendable currency.

*Illustrative run payout*

#figure(
  align(center)[#table(
    columns: 3,
    align: (col, row) => (auto, right, right).at(col),
    inset: 6pt,
    [Source], [Example calculation], [Coins],
    [Delivered parcels], [6 parcels x 10], [60],
    [On-time bonuses], [2 x 5], [10],
    [Delivery streak], [1 completed streak], [10],
    [Boss victory], [1 victory], [50],
    [*Total before optional rewards*;], [], [*130*;],
  )],
)

The Tip Jar’s +10% example could apply only to the *60
  base-delivery coins*, adding 6. This limits multipliers from stacking
with boss and challenge payouts. Values should be recalibrated against
desired time to first purchase and per-run earnings distributions.

#figure(
  [
    ```mermaid
    flowchart LR
      DEL["Successful deliveries"] --> PAY["Persistent coin wallet"]
      BONUS["On-time / streak / boss / challenge bonuses"] --> PAY
      POSTSHIFT["PostShift - rewarded each shift"] --> PAY
      PAY --> UPGRADES["Upgrade unlocks and Upgrade Tiers"]
      XP["Deliveries / PostShift: Career XP"] --> LEVEL["Career Level + Postal Rank"]
      LEVEL --> POSTSHIFT
      PAY --> LOADOUT["Second passive slot"]
      PAY --> SKINS["Earnable skins and cosmetics"]
      UPGRADES --> BETTER["Better next run"]
      LOADOUT --> BETTER
      SKINS --> BETTER
      BETTER --> DEL
      RESULTS["Optional rewarded ad at results"] -->|"Explicit claim only"| PAY
      IAP["Optional IAP: cosmetics, no-ads, coin packs"] -->|"Coin packs only"| PAY
      classDef src fill:#313244,stroke:#A6E3A1,color:#CDD6F4
      classDef sink fill:#313244,stroke:#89B4FA,color:#CDD6F4
      classDef external fill:#45475A,stroke:#F9E2AF,color:#CDD6F4
      class DEL,BONUS,BETTER src
      class PAY,UPGRADES,LOADOUT,SKINS,LEVEL sink
      class XP src
      class RESULTS,IAP external
    ```
  ],
  caption: [Wallet income, shop spending, and the PostShift reward],
)

== Wallet, earning, and spending rules
<wallet-earning-and-spending-rules>
- Credit successful delivery earnings to the persistent wallet
  immediately \(with a durable save/ledger), while also displaying a
  running "earned this run" tally. Death does not remove credited coins.
- A single transaction must be credited once even if the app suspends,
  restarts, retries a callback, or shows the same results screen twice.
- Validate purchases against wallet balance; deduct coins and grant the
  item atomically. Owned purchases remain after death or reinstall
  if account/cloud saving supports restoration.
- Completing a challenge grants its reward once; store a claimed flag.
  Sorting awards the completed shift exactly once, with a
  distinct attempt ID and snapshot salary band. One free shift is
  granted each day; tickets authorize additional fully rewarded shifts.
  Avoid unlimited daily-quest rerolls that manufacture currency.
- Show the *next Upgrade Tier's price*, current balance, and effect
  before purchase. Show the current and next tier for each eligible
  Passive Perk or Cargo Perk. Once unlocked or upgraded, equipping it never
  incurs another fee.

*Pricing approach.* Keep the first meaningful purchase reachable
after a handful of successful deliveries. Price later capacity and collision-protection Upgrade Tiers higher than simple utility perks, and treat the second passive slot as a
visible progression milestone. Prices and payouts should be tuned
together using median and lower-percentile performance, not top-player
earnings alone.

== Economy and capacity trade-off
<economy-and-capacity-trade-off>
Bigger deliveries need a reason to be attractive. A possible
_bulk-routing bonus_ rewards several correctly delivered parcels in
one stop, but must not outweigh the danger of controlling a long snake.
For example, reward completing a route without collision or delivering
multiple matching parcels to one address rather than paying for
collecting/hoarding alone.

Track *coins per minute* by loadout, not only coins per parcel.
Measure PostShift coins and XP separately because the short
paid or earned-ticket shifts can distort progression and coin income
if analyzed together with Snake runs. A
one-slot Parcel Magnet might increase pickup speed while also increasing
delivery frequency because it can carry less. If its measured income is
lower, improve its utility or reduce the perk’s cost. A survival Cargo Perk
may earn fewer coins per minute while increasing average run duration;
that is a valid strategic trade-off.

= Monetization placement
<monetization-placement>
*General principle.* Active Snake controls require uninterrupted
attention. Gameplay-affecting ads, storefront pop-ups, and banners
should stay out of an active run. Free players must retain a viable
progression path; paid offers and advertisements supplement rather than
replace play-earned coins.

#figure(
  [
    ```mermaid
    flowchart TD
      HOME["Home / shop / loadout"] -->|"IAP storefront; optional banner in safe zone"| PLAY["Start run"]
      HOME --> POSTSHIFT["Daily free sorting shift / ticket replay"]
      POSTSHIFT --> MAILRESULT["PostShift result: coins + Career XP"]
      MAILRESULT --> HOME
      MAILRESULT --> TICKET["Optional rewarded ad: gain a shift ticket"]
      TICKET --> POSTSHIFT
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
    [Home shop, cosmetic tab, PostShift ticket purchase, optional post-run shop],
    [Cosmetic bundles, no-ads, optional coin packs and shift tickets],
    [Forced offers on death; direct purchase inside active steering],

    [Rewarded video],
    [Run results bonus/revive; opt-in shift ticket offer outside sorting],
    [Disclosed coin bonus or revive; PostShift offer grants a replay ticket],
    [Auto-play, undisclosed rewards, forcing an ad to keep earned coins],

    [Interstitial \("pop-up") ad],
    [After the results screen, at a natural transition to home],
    [Controlled, dismissible frequency; skip if player just watched
      rewarded ad],
    [Mid-run, at spawn, during upgrades, immediately before control
      returns],

    [Banner ad],
    [Home / non-interactive menu area only; never in PostShift],
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
- *Coins:* At run results, "Watch an ad for +25% of coins earned
  this run", capped at a tunable fixed amount, claimed at most once per run.
  Example (rounding to the nearest whole coin):
  $min(floor(0.25 dot 130 + 0.5), 50) = 33$. Preserve the initial 130
  coins whether the offer is accepted or skipped.
- *Revive:* Optionally offer one revive after empty-cargo countdown
  expiry. Pause gameplay while the choice/ad is shown. A revive must
  restore a safe position and a full countdown, providing a genuine
  chance to collect a Parcel, not a lives pool.
- Show the exact reward before opt-in. If no ad is available, continue
  normally; never leave the player stuck. Reward grants must be
  idempotent; ad-unavailable or network-failure paths cannot erase
  earnings.
- PostShift rewarded advertising, if enabled, grants one replay
  ticket after an explicit opt-in, not a flat coin bonus. A spent ticket
  permits a fully rewarded shift. Frequency-limit the ad offer for
  usability without applying a coin cap to other shift sources.
- Consider offering only one of these rewards in a session transition to
  reduce ad pressure. A purchased "no forced ads" entitlement should
  still allow optional rewarded ads if explicitly disclosed.

== Interstitial and banner restrictions
<interstitial-and-banner-restrictions>
*Conservative initial cadence \(proposal):* no forced
interstitials during the tutorial or first several runs; after that, at
most one after a completed results screen when *both* a multi-run
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

*IAP compliance.* Digital items, game currency, and ad-removal
purchases generally use the platform’s in-app purchase system under
applicable rules and regional exceptions. Purchased coins should not
expire on iOS; restoring non-consumable entitlements needs a clear path.
\[3, 5\]

*Audience warning.* The monetization design assumes a general
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
    [Start, PostShift, Career Level, Daily Challenges, Shop],
    [Career XP, rank and wallet visible without intrusive overlays],

    [Shop],
    [Buy Upgrade Tiers, unlock or upgrade perks, buy skins, open IAP],
    [Show owned and next tier, effect, cost, and separate Postal Rank requirements],

    [Loadout], [Equip Passive Perks and Cargo Perks], [Separate passive-slot and capacity indicators],
    [PostShift],
    [Swipe an envelope toward a destination bin; 30-second round],
    [Six legible targets, previewed throw aim, no in-round ads],

    [PostShift results],
    [View correct sorts, PostShift salary, earned coins/XP, ticket balance],
    [Every shift pays; no daily coin cap or practice mode],

    [Gameplay], [Steer, read destinations and carried Parcels, deliver], [Show Parcel loss and empty-cargo countdown; no ad overlays],
    [Upgrade picker], [Choose one of three randomized upgrades], [Pause gameplay; large touch targets; durations shown],
    [Events / boss HUD], [Hazard warning, timer, boss progress], [Communicate risk before affecting steering],
    [Results],
    [Deliveries, coins, milestones, optional ad reward],
    [Earned coins already safe; ad reward clearly optional],
  )],
)

*Onboarding order \(proposal).* First teach pick-up and growth;
then show matching addresses and shrinking; then explain that
collisions and deliveries can leave the snake empty, starting a
countdown reset by collecting another Parcel; then reveal capacity
and the reason to deliver; then award the first coins and show a
starter purchase. Unlock PostShift after the player recognizes at least
two delivery shapes. Show the Career XP bar and Postal Rank after the
first parcel delivery, then explain the daily free shift and replay
tickets once PostShift becomes available. Introduce random upgrades after the core delivery concept is
understood. Initially keep the loadout simple, then unlock the second passive
slot after the player understands how Cargo Perks reduce available parcel slots.

*Touch control considerations.* The safe capacity limit must be
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
    [Starting capacity budget ($C$)], [4 capacity slots], [Can new players route without gridlock?],
    [Upper capacity budget], [6–7 capacity slots], [Still controllable on small screens?],
    [Passive perk slots], [1; unlockable 2], [Does the second passive slot create interesting builds?],
    [Cargo Perk cost], [1 capacity slot; no passive slot], [Is throughput reduction meaningful?],
    [Minimum room for parcels], [2 remaining after Cargo Perks], [Can every loadout complete deliveries?],
    [Empty-cargo countdown duration], [TBD by playtest], [Enough time from spawn or last Parcel to get a pickup?],
    [Collision recovery window], [TBD by playtest], [Prevent repeated loss from sustained contact?],
    [Base delivery payout], [10 coins / delivered parcel], [Time to first owned Upgrade],
    [Boss reward], [50 coins \(example)], [Risk and completion time],
    [Rewarded results bonus], [+25%, cap 50], [Avoid ad dominating progression],
    [PostShift duration / bins], [30 seconds / 6], [Wind Waker reference; mobile swipes must be tested],
    [Free shift / day], [1], [Every extra shift uses a ticket; no practice],
    [PostShift payout ceiling / shift], [55 base; 96 at Postmaster], [Per-shift anti-arbitrage, no daily coin cap],
    [Career XP / delivery], [20], [Compare level pacing across player cohorts],
    [Career XP / shift], [$M(S)$; see Career XP formula], [Sorting supplements the Snake Career Level],
  )],
)

== Measurement plan
<measurement-plan>
Measure deliveries per minute, banked coins per minute, snake length
distribution, collisions by length and cause (hazard or self), Parcels
lost per collision, time spent empty, pickups during the countdown,
countdown expiry rate, and survivability by loadout. Also measure
career-level pacing, PostShift score and wrong-swipe distributions,
skipped upgrade choices, boss outcomes, session length, and the share
of runs ending at full capacity. For monetization,
also measure ad opt-in, interstitial exposure, abandonment after ads,
purchase conversion, and progression for non-paying players. Compare
lower-percentile, median, and skilled player cohorts.

*Key invariants for engineering and QA*

- Capacity cannot exceed the mobile safety cap. Enforce
  $R + P <= C$, $R <= C - 2$, and $A <= S <= 2$ after loadout or upgrade
  changes. Reject any mid-run capacity reduction that would break these
  limits; do not detach an equipped Cargo Perk mid-run.
- Cargo Perks and base-body segments are fixed; delivered or
  collision-lost Parcels are removable. A bump removes at most one
  carried Parcel and awards no delivery. Equipping Cargo Perks never
  creates a shorter maximum-length snake than equipping none.
- Only the empty-cargo countdown ends an ordinary run. It starts
  full whenever no Parcels are carried (including at spawn); any valid
  pickup resets and disables it; while carrying Parcels it cannot
  expire. Further collisions when empty never cause immediate defeat.
- The current milestone pool has Effects configured to expire during or
  at run end. Ownership of purchased Upgrade unlocks and Upgrade Tiers,
  skins and wallet balance persists. Effect lifetime follows the content
  definition, not acquisition method. Higher Upgrade Tiers do not
  change a Perk's equipment cost by default.
- Coins never double-award on duplicated delivery events, daily
  shift completion, replay tickets, reward callbacks, ad callbacks,
  or results re-entry. Each shift earns the full performance payout
  exactly once; no daily coin ceiling applies. Under current prototype
  values, the per-shift currency maximum is 96 at the top Postal Rank.
- Collision processing and the empty-cargo countdown must pause
  with gameplay behind full-screen modals. Sustained physics overlap
  cannot repeatedly remove multiple Parcels without recovery.
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
- Which hazards and self-collisions challenge steering without
  stripping Parcels unfairly? What countdown and collision-recovery
  durations give enough time to find a pickup?
- How quickly should the second passive slot unlock, and when does
  reserving multiple capacity slots for Cargo Perks outperform carrying more parcels?
- Which perks genuinely benefit from tiers? Test Parcel Magnet
  and Tip Jar first, watch Safety Shield and Express Engine for balance
  problems, and leave unclear steering/sorting upgrades single-tier
  until a useful progression exists.
- Which carried Parcel falls off on collision (tailmost, hit segment,
  or selected inventory item), and can a dropped Parcel be recovered?
- Can players reliably swipe to six targets on small phones at
  20+ correct sorts per shift? Test target layout, direction mapping
  and 30-second threshold distribution.
- Do PostShift salary multipliers and ticket-bundle prices remain viable
  across all levels and regions without making paid replays a cheaper
  substitute for coin packs? Recalculate when prices change.

= Policy and technical sources
<policy-and-technical-sources>
The monetization placement rules and gesture-target recommendations
are grounded in the following documentation consulted on
*3 November 2026*. Rules can change before release.

+ Google Play, _Ads policy_:
  #link("https://support.google.com/googleplay/android-developer/answer/9857753")
+ Google AdMob, _Interstitial ads for Unity_:
  #link("https://developers.google.com/admob/unity/interstitial")
+ Apple, _App Review Guidelines_:
  #link("https://developer.apple.com/app-store/review/guidelines/")
+ Google AdMob, _Banner ad guidance_:
  #link("https://support.google.com/admob/answer/6128877")
+ Google Play, _Payments policy_:
  #link("https://support.google.com/googleplay/android-developer/answer/9858738")
+ Google AdMob, _Rewarded ads for Unity_:
  #link("https://developers.google.com/admob/unity/rewarded")
+ Apple Developer, _UI Design Dos and Don'ts_:
  #link("https://developer.apple.com/design/tips/")
+ Android Accessibility Help, _Touch target size_:
  #link("https://support.google.com/accessibility/android/answer/7101858")
+ Old School RuneScape Wiki, _Experience formula and level table_:
  #link("https://oldschool.runescape.wiki/w/Experience")

_Compilation:_ This file has no local image, diagram, or data-file
imports. The official `@preview/catppuccin:1.1.0` package supplies the
Mocha page, typography and accent palette. Mermaid flowcharts render directly
through `@preview/merman:0.5.0`; Typst 0.15.0 or newer is required.
Run `typst compile Parcel_Snake_GDD.typ`
after the packages are available in your Typst environment.

_Inspiration and tooling references:_
- Zelda Dungeon, _The Wind Waker mail-sorting minigame_:
  #link("https://www.zeldadungeon.net/wiki/Mail_Sorting")
- Zelda Wiki, _Mail Center minigame_:
  #link("https://www.zeldawiki.wiki/wiki/Mail_Center_%28Mini-Game%29")
- Typst Universe, _merman package_:
  #link("https://typst.app/universe/package/merman/")
- Catppuccin, _official Typst theme package_:
  #link("https://typst.app/universe/package/catppuccin/")
