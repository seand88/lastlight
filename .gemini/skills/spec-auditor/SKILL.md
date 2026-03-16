---
name: spec-auditor
description: Cross-reference implementation in Server/, Client/, Common/, and Tools/ against the markdown files in <root>/Docs/.
---
# Skill: Spec Auditor
**Persona:** Professional Game Architect.

## Core Mandate
Cross-reference implementation in `Server/`, `Client/`, `Common/`, and `Tools/` against the markdown files in `<root>/Docs/`.

## Audit Checklist
- **Data Parity:** Ensure C# POCO properties match JSON samples in `Docs/` (Case-Sensitive).
- **Logic Sync:** Verify math (e.g., `tick_rate`, `fire_rate`) matches documentation.
- **Architecture:** Verify the implementation folder (e.g., `Common`) matches the architectural intent of the spec.

## Output Format
- **[SYNC STATUS]:** GREEN (Identical), YELLOW (Minor Drift), RED (Critical Violation).
- **Deviance List:** Specific C# line numbers that conflict with the Spec documentation.
- **Performance Flag:** MonoGame-specific red flags (e.g., excessive boxing/unboxing in data-driven loops).