# Expert Memory Model

## Why Memory Matters

A Feature Expert or Domain Application Expert cannot be useful over time without memory.

Without memory, the agent can answer only from the current prompt or from static instructions. That is not enough for enterprise scenarios where knowledge evolves continuously across features, code, architecture, incidents, decisions, dependencies, and ownership.

The expert must be able to:
- remember important domain knowledge
- retain historical decisions and context
- update its knowledge when the system changes
- distinguish between fresh knowledge and outdated knowledge
- track confidence and open gaps

In short, the expert needs a **managed memory** and not just a prompt.

## Core Idea

Each expert should have a dedicated memory space that represents the current known understanding of its feature or domain.

This memory should evolve over time and act as the expert’s working knowledge base.

The memory is not only a dump of information. It should be structured so the expert can reason about:
- scope
- dependencies
- workflows
- affected areas
- decisions
- risks
- ownership
- change history
- confidence and unknowns

## Memory Principles

### 1. Memory must be scoped
Each expert should remember only the information relevant to its owned feature or domain, plus dependencies and related impacts.

### 2. Memory must be updateable
The expert must support adding, correcting, deprecating, and removing knowledge as the application evolves.

### 3. Memory must preserve source awareness
Whenever possible, remembered knowledge should be connected to where it came from:
- architecture doc
- code
- design review
- product requirement
- incident
- human feedback
- test result

### 4. Memory must separate fact from assumption
The expert should clearly distinguish between:
- confirmed knowledge
- inferred knowledge
- open questions
- stale or uncertain knowledge

### 5. Memory must support impact analysis
The memory should make it easier to answer: what depends on this, what can break, and what areas are affected by change.

## What the Expert Should Remember

### Domain Identity
- feature/domain name
- purpose
- ownership boundary
- out-of-scope areas

### Functional Knowledge
- key workflows
- business rules
- states and transitions
- user scenarios
- edge cases

### Technical Knowledge
- components
- services
- APIs
- data structures
- configurations
- integrations
- jobs/background processes

### Dependency Knowledge
- upstream dependencies
- downstream dependencies
- related features/domains
- shared infrastructure

### Change Knowledge
- major design decisions
- known refactors
- historical incidents
- breaking changes
- technical debt
- deprecated behavior

### Risk Knowledge
- sensitive areas
- performance concerns
- safety or compliance concerns
- consistency concerns
- backward compatibility concerns

### Knowledge Quality
- confidence level
- freshness
- missing information
- contradictory information
- pending validation

## Suggested Memory Structure

Each expert can maintain memory in structured sections like:

```md
# Expert Memory

## Identity
## Scope
## Out of Scope
## Workflows
## Rules and Constraints
## Components
## Dependencies
## Decisions and History
## Risks
## Impact Patterns
## Open Questions
## Confidence / Freshness
## Sources
```

## Memory Operations

The platform should support the following memory operations:

### Add Memory
Add new validated information.

Examples:
- new dependency discovered
- new workflow added
- ownership updated
- new incident teaches an important lesson

### Update Memory
Modify existing knowledge when behavior or structure changes.

Examples:
- API contract changed
- workflow behavior changed
- component moved to another service

### Deprecate Memory
Mark knowledge as outdated but still historically relevant.

Examples:
- old architecture path no longer active
- legacy dependency removed
- previous workaround replaced

### Remove Memory
Delete information only when it is clearly invalid and no longer useful even as historical context.

### Annotate Memory
Add confidence, freshness, source, or warnings to remembered knowledge.

## Memory Entry Attributes

A good memory item should ideally include:
- statement
- category
- source
- date or version context
- confidence level
- status: active / uncertain / deprecated
- related components or workflows

Example:

```md
- Statement: Multi-batch depends on volumetric cache metadata for origin series lookup.
- Category: Dependency
- Source: design review discussion
- Confidence: medium
- Status: active
- Related Areas: multi-batch, cache, metadata pipeline
```

## How Memory Gets Updated

Memory can be updated from several sources:
- explicit human updates
- design documents
- architecture documents
- code changes
- pull requests
- bugs and incidents
- test findings
- production learnings
- reviews with domain experts

## Memory Governance Questions

Later, the platform should define rules such as:
- who is allowed to update memory
- what sources are trusted enough for automatic updates
- when memory requires human approval
- how contradictions are handled
- how stale memory is detected
- how confidence is recalculated

## Important Distinction

There are two layers:

### 1. Expert Definition
This is the static identity of the expert.
It defines what the expert is supposed to own.

### 2. Expert Memory
This is the evolving knowledge of the expert.
It changes over time as the system and understanding evolve.

Both are required.
Without the definition, the expert has no boundary.
Without the memory, the expert has no continuity.

## Recommended Direction

For this project, we should treat memory as a first-class part of every expert.

That means every expert should eventually include:
- a fixed expert profile
- a structured memory area
- update rules
- confidence and freshness indicators
- links to sources of truth

## Future Work

Later we should define:
- memory storage options
- short-term vs long-term memory
- automatic memory extraction from artifacts
- validation workflows
- collaboration between expert memories
- conflict resolution between experts
- summarization and pruning strategies

## Current Conclusion

A domain expert agent is not complete without memory.
The memory is what allows the expert to become a persistent enterprise knowledge companion instead of a one-time assistant.
