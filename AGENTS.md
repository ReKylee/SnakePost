# AGENTS.md — context for AI coding tools

AI tools read this file before working on the repo. Keep it short, true and up to date.
If you use Claude Code, also add a `CLAUDE.md` containing one line: `@AGENTS.md`.

## The game
[One paragraph: genre, core loop, meta layer, target device (Android, portrait, one thumb).]

## Tech
- Engine: Unity 6 LTS, C#
- Target: Android (portrait)
- [Add packages and services as you add them]

## Architecture
[Fill in during the course: scenes, main systems, where config lives, init order.]

## Rules for AI
- Follow the existing folder structure and naming. Don't create new top-level folders.
- Don't invent APIs. If unsure about an SDK or Unity API, say so and point to the docs.
- Don't change Player Settings, package name, API levels or build settings unless asked.
- Keep changes small and explain what you changed and why.
- Never commit secrets (keystores, passwords, API keys).

## Conventions
- [Naming, code style, where scripts go — fill in as a team]
