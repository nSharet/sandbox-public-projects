# AI Project Agents

A reusable collection of AI agents for **design-first software development**. These agents enforce a structured methodology where design documentation must be completed and approved before implementation begins.

## Philosophy

**NO CODE UNTIL DESIGN IS APPROVED.**

This framework follows a 6-phase approach:
1. **Brainstorming** — Explore options, compare tradeoffs, choose approach
2. **Requirements** — Define scope, user stories, acceptance criteria
3. **Architecture** — System design, technology decisions, ADRs
4. **Design** — Detailed specs, class diagrams, sequence diagrams
5. **Review Prep** — Organize summaries for team design review
6. **Implementation** — Build, test, document

## Quick Start

### 1. Copy to Your Project

Copy the `AGENTS.md` file to your project root. This enables the Project Planner Agent to orchestrate your workflow.

```
your-project/
├── AGENTS.md              ← Copy from here
├── docs/
│   └── design/            ← Will be created by agents
└── src/
```

### 2. Start Planning

Ask the agent: *"What problem are you solving?"*

The agent will guide you through each phase, creating documentation in `docs/design/`.

### 3. Use Phase Agents

Each phase has a specialized agent:

| Phase | Agent | Purpose |
|-------|-------|---------|
| 1 | `@brainstorming-agent` | Explore approaches, analyze tradeoffs |
| 2 | `@requirements-agent` | Write user stories, acceptance criteria |
| 3 | `@architecture-agent` | System design, ADRs, technology decisions |
| 4 | `@design-agent` | Class diagrams, sequence diagrams, API contracts |
| 5 | `@design-review-organizer-agent` | Create Light + Detailed review summaries |
| 6 | `@implementation-agent` | Task breakdown, coding, testing |

## Folder Structure

```
AI-Project-Agents/
├── README.md                 ← You are here
├── AGENTS.md                 ← Main orchestrator (copy to projects)
├── agents/                   ← Phase-specific agents
│   ├── brainstorming-agent.md
│   ├── requirements-agent.md
│   ├── architecture-agent.md
│   ├── design-agent.md
│   ├── design-review-organizer-agent.md
│   └── implementation-agent.md
└── templates/                ← Document templates
    ├── adr-template.md
    ├── status-template.md
    └── project-structure.md
```

## Project Documentation Structure

When using these agents, your project will generate:

```
your-project/
├── AGENTS.md                          ← Copy from AI-Project-Agents
└── docs/
    └── design/
        ├── STATUS.md                  ← Phase tracking
        ├── brainstorming.md           ← Phase 1 output
        ├── requirements.md            ← Phase 2 output
        ├── architecture.md            ← Phase 3 output
        ├── review-summary-light.md    ← Phase 5 output (executive)
        ├── review-summary-detailed.md ← Phase 5 output (technical)
        ├── adr/                        ← Architecture Decision Records
        │   ├── 000-template.md
        │   └── 0001-decision-title.md
        └── diagrams/                   ← Phase 4 output
            ├── class-diagrams.md
            ├── sequence-diagrams.md
            ├── state-diagrams.md
            ├── data-model.md
            ├── api-contracts.md
            └── traceability.md
```

## Customization

### Add Project-Specific Agents

Copy agent files to your project and modify as needed:

```
your-project/
├── AGENTS.md
└── agents/
    └── my-custom-agent.md
```

### Modify Templates

Copy templates and customize for your team's standards.

## Phase Gating Rules

| Phase | Required Before Proceeding |
|-------|----------------------------|
| Brainstorming | At least 3 approaches with pros/cons |
| Requirements | User stories with acceptance criteria |
| Architecture | ADRs for major decisions |
| Design | Mermaid diagrams complete |
| Review Prep | Light + Detailed summaries ready |
| Implementation | Design review approved |

## Best Practices

1. **Don't skip phases** — Each phase builds on the previous
2. **Document decisions** — Use ADRs for architecture choices
3. **Visual first** — Create diagrams before prose
4. **Trace requirements** — Link code to requirements
5. **Review before code** — Phase 5 exists for team alignment

## License

MIT — Use freely in your projects.
