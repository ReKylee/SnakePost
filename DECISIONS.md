# Decision log

Add an entry every time the team makes a real choice: a tool, a design, a trade-off.
About 1–2 entries a week. Newest at the top. Write the reasoning yourselves; AI may fix the wording.

## Template

```text
## YYYY-MM-DD — [The decision, in a few words]
Options: [A] / [B] / [C]
Chose: [what] — [why]
AI: [what the AI suggested, and what you did with it — or "not used"]
Who: [name(s)]
```

---

<!-- Your entries start here -->
## 2026-11-03 Snake length limit

Options: Restrict / Unrestricted  

Chose: Restrict - Unlimited length will disrupt controls, gameplay choices(such
as what's my next package) and visuals  

AI: Not Used.
Who: Kylee & Peleg

## 2026-11-03 Number of perk slots and which are permanent

Chose: The player has a starting passive perk slot, but stronger upgrades
require taking up a precious package slot that could've been used to deliver
more packages.  

AI: Not Used.
Who: Kylee & Peleg

## 2026-11-03 Daily mail sorting minigame

Chose: Once a day the player can take the mail sorting minigame where they throw
envelopes into one of six matching bins. Depending on various factors they get
rewarded. They can take the challenge again with an IAP.  

AI: Not Used.
Who: Kylee & Peleg

## 2026-11-03 Upgrade your upgrades in the shop

Chose: In the shop you can choose to invest and upgrade the various random
upgrades that appear throughout the run, as well as the passive perks. rewarded.

AI: Not Used.
Who: Kylee & Peleg

## 2026-11-03 Introduce a tier/level system in the game

Chose: Player will earn XP, compete on the leaderboard and climb up the ranks by
doing various in game challenges, daily challenges, playing session  

AI: Not Used.
Who: Kylee & Peleg

## 2026-11-11 Initial art style for UI

Options: Regular 2D / Pre-rendered 3D models / 3D Models

Chose: Pre-rendered 3D Models - We wanted a sketchy, toon artstyle to make it
feel like the snake is living in a diorama, but regular 3D scenes aren't
responsive for different screen sizes so we compromised on pre-rendering using
an editor tool to have control over how the models look but to still use uGUI.
We also tried putting the meshes in unity's Canvas and that technically worked
but wasn't properly responsive.

AI: Used to create the editor tool for faster design iterations.
Who: Kylee & Peleg

## 2026-11-11 Screen transition

Options: Different scenes(which means some inter-scene transition) / One screen
slide in / One scene dive in

Chose: We initially took inspiration from Jetpack
Joyride's "tap to play" and the game screen always being visible with you
jumping into action, so we wanted a similar experience and a snake diving in to
deliver mail is hilarious. We use Kylee's preferred tweening library LitMotion for our coded animations.

AI: Not Used.
Who: Kylee & Peleg

## 2026-11-11 uGUI vs UI Toolkit

Chose: uGUI - We're familiar with it and our UI is nothing too crazy. We still
plan to work in a UI Toolkit-like way regarding data binding and such(Model View
and all that data driven goodness)

AI: Not Used.
Who: Kylee & Peleg
