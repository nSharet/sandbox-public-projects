# Guidelines for Creating Feature Experts and Domain Application Experts

## Purpose

This document defines the initial guidelines for creating agent experts inside the **Feature Expert Agents** project.

The goal is to build agent experts that act similarly to strong human domain experts inside enterprise applications. Each expert should become a reliable source of knowledge for a specific feature, concept, business capability, or application domain.

This is the first foundation. Later, we will extend it with methods for training, validating, evolving, and governing those experts over time.

## Core Idea

A domain expert agent should not be a generic assistant.
It should have a clear ownership boundary.
It should know what it is responsible for, what it is connected to, what can be affected by changes, and where its knowledge begins and ends.

## Types of Experts

### 1. Feature Expert
A Feature Expert focuses on a specific feature, epic, or capability inside the application.

Examples:
- Reporting workflow expert
- Auto-recovery expert
- Segmentation workflow expert
- User permissions expert
- Multi-batch expert

### 2. Domain Application Expert
A Domain Application Expert focuses on a broader concept or domain area that may span multiple features.

Examples:
- Imaging workflow expert
- Patient data management expert
- Notification domain expert
- Study lifecycle expert
- Authorization and security domain expert

## Main Responsibilities of an Expert

Each expert should be able to help with:
- explaining the purpose of the feature or domain
- describing the main flows and use cases
- identifying key components involved
- surfacing dependencies and integrations
- identifying affected areas when changes are proposed
- highlighting risks, assumptions, and side effects
- clarifying boundaries and ownership
- supporting design and implementation discussions

## Required Knowledge Areas

Before an expert can be considered useful, it should contain or be connected to knowledge about:

### Business Understanding
- Why the feature or domain exists
- Who uses it
- What user or business problem it solves
- What outcomes it is expected to achieve

### Functional Understanding
- Main scenarios and workflows
- Inputs, outputs, and state transitions
- Rules, constraints, and edge cases

### Technical Understanding
- Relevant services, modules, APIs, UI areas, jobs, and data structures
- Events, contracts, storage, and external dependencies
- Performance, reliability, and security considerations

### Change Impact Understanding
- What upstream systems affect it
- What downstream systems depend on it
- What could break if behavior changes
- Which teams or owners may need to be involved

## Minimum Definition of an Expert

Every expert definition should include at least:
- expert name
- expert type
- owned scope
- out-of-scope areas
- key workflows
- dependencies
- impacted components
- important risks
- primary questions the expert should answer
- known limitations or confidence gaps

## Suggested Expert Template

```md
# Expert Name

## Type
Feature Expert / Domain Application Expert

## Scope
What this expert owns and explains.

## Out of Scope
What this expert should not claim ownership over.

## Purpose
Why this feature or domain exists.

## Main Workflows
List the main workflows or scenarios.

## Key Components
UI, services, APIs, jobs, data stores, modules, external systems.

## Dependencies
Upstream and downstream dependencies.

## Impact Analysis
What areas are commonly affected by change.

## Risks and Sensitive Areas
Performance, safety, consistency, backward compatibility, security, etc.

## Key Questions This Expert Should Answer
- ...
- ...
- ...

## Known Gaps
What is still missing or uncertain.
```

## Principles for Creating Good Experts

### 1. Keep ownership clear
An expert should have a defined responsibility boundary.
Avoid creating experts that are too vague or too broad at the beginning.

### 2. Prefer real application concepts
Experts should map to how people actually think about the system: features, workflows, domains, business capabilities, or technical concepts.

### 3. Capture dependencies explicitly
A strong expert is not only about what it owns, but also about what it touches.
Dependencies should be part of the expert definition from the start.

### 4. Separate confidence from assumption
The expert should distinguish between known facts, inferred knowledge, and open questions.

### 5. Support impact analysis
A useful expert should help answer: “If we change this, what else is affected?”

### 6. Evolve incrementally
Do not wait for complete knowledge before creating an expert.
Start with a useful baseline and refine over time.

## Initial Creation Process

1. Choose the target feature or domain.
2. Define why it deserves its own expert.
3. Define the ownership boundary.
4. List the main workflows and use cases.
5. Identify major components and integrations.
6. Identify likely dependencies and affected areas.
7. Record known risks and open questions.
8. Create the first expert profile.
9. Review the profile with humans who know the area.
10. Refine it over time based on usage.

## What We Will Define Later

This document focuses on the initial creation guidelines.
Later, we should define:
- how experts learn and improve over time
- what sources of truth they can use
- how to validate expert quality
- how to measure confidence and freshness
- how to handle conflicting knowledge
- how experts collaborate with each other
- governance rules for enterprise usage

## Current Intent

For now, the purpose is simple:
create a clear and reusable way to define **feature experts** and **domain application experts** in a structured, enterprise-oriented manner.
