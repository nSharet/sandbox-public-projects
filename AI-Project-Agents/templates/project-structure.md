# Project Structure Template

When using the AI Project Agents, your project should have this documentation structure:

## Recommended Structure

```
your-project/
├── AGENTS.md                          ← Copy from AI-Project-Agents
├── src/                               ← Your source code
│   └── ...
├── tests/                             ← Your tests
│   └── ...
└── docs/
    └── design/
        ├── STATUS.md                  ← Use templates/status-template.md
        ├── brainstorming.md           ← Phase 1 output
        ├── requirements.md            ← Phase 2 output
        ├── architecture.md            ← Phase 3 output
        ├── review-summary-light.md    ← Phase 5 output (executive)
        ├── review-summary-detailed.md ← Phase 5 output (technical)
        ├── implementation-plan.md     ← Phase 6 planning
        ├── adr/                        ← Architecture Decision Records
        │   ├── 000-template.md        ← Use templates/adr-template.md
        │   ├── 0001-decision-1.md
        │   └── 0002-decision-2.md
        └── diagrams/                   ← Phase 4 output
            ├── class-diagrams.md
            ├── sequence-diagrams.md
            ├── state-diagrams.md
            ├── data-model.md
            ├── api-contracts.md
            └── traceability.md
```

## File Purposes

| File | Phase | Purpose |
|------|-------|---------|
| `STATUS.md` | All | Track current phase and progress |
| `brainstorming.md` | 1 | Problem analysis, approaches, decision |
| `requirements.md` | 2 | User stories, acceptance criteria |
| `architecture.md` | 3 | System design, technology stack |
| `adr/*.md` | 3 | Architecture decision records |
| `diagrams/*.md` | 4 | Detailed technical diagrams |
| `review-summary-light.md` | 5 | Executive overview for design review |
| `review-summary-detailed.md` | 5 | Full technical documentation |
| `implementation-plan.md` | 6 | Task breakdown and tracking |

## Getting Started

1. **Copy AGENTS.md** to your project root
2. **Create `docs/design/` folder**
3. **Copy STATUS.md** template to `docs/design/STATUS.md`
4. **Start with Phase 1** — ask "What problem are you solving?"

## Phase Flow

```
┌─────────────────┐
│ 1. BRAINSTORM   │ ─── Explore options, pick approach
└────────┬────────┘
         ▼
┌─────────────────┐
│ 2. REQUIREMENTS │ ─── Define scope, user stories
└────────┬────────┘
         ▼
┌─────────────────┐
│ 3. ARCHITECTURE │ ─── System design, ADRs
└────────┬────────┘
         ▼
┌─────────────────┐
│ 4. DESIGN       │ ─── Detailed diagrams, contracts
└────────┬────────┘
         ▼
┌─────────────────┐
│ 5. REVIEW PREP  │ ─── Summaries for team review
└────────┬────────┘
         ▼
┌─────────────────┐
│ 6. IMPLEMENT    │ ─── Build, test, document
└─────────────────┘
```

## Tips

1. **Don't skip phases** — each builds on the previous
2. **Update STATUS.md** as you complete items
3. **Use Mermaid** for all diagrams (renders in GitHub/VS Code)
4. **Trace decisions** back to brainstorming for context
5. **Review before coding** — Phase 5 is for team alignment
