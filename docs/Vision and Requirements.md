# EPG Manager - Vision and Requirements

## Purpose

This document captures the vision, business drivers, requirements, workflows, assumptions, and domain knowledge for EPG Manager.

This artefact describes **what** the project should accomplish and **why** it exists.

Implementation standards, development practices, and governance are managed through the Development Operating System (DOS) and the Project Notebook Instructions.

---

## Vision

EPG Manager is a self-hosted IPTV curation platform that consolidates multiple playlist and EPG sources into a single curated viewing experience optimized for TiViMate.

The platform exists because no single provider consistently offers complete, accurate, and reliable channel listings and program guide data.

The system should preserve user curation decisions while continuously adapting to changing source data.

The ultimate goal is to provide consistently accurate television program information for the channels that matter.

---

## Problem Statement

Current IPTV providers frequently suffer from one or more of the following problems:

- Missing channels
- Incorrect guide data
- Incomplete guide data
- Poor EPG coverage
- Channel naming inconsistencies
- Provider-specific data quality issues
- Channels randomly disappearing from source playlists
- Channels being renamed without warning

While streams are usually available somewhere, maintaining an accurate and reliable guide experience requires constant manual effort.

EPG Manager exists to centralize this effort and preserve the resulting curation.

---

## Project Goals

### Primary Goal

Provide consistently accurate program guide information for the curated channel lineup consumed by TiViMate.

### Secondary Goals

- Consolidate multiple M3U sources.
- Consolidate multiple EPG sources.
- Preserve user-maintained curation.
- Minimize repetitive maintenance.
- Reduce dependency on any single provider.
- Simplify TiViMate configuration.
- Provide controlled refresh and reconciliation workflows.

---

## Success Criteria

The project is considered successful when:

- TiViMate can consume generated outputs without issue.
- Generated guide data is more accurate than any individual source.
- User-maintained mappings survive source refreshes.
- Missing or renamed channels can be managed without losing configuration.
- Scheduled EPG refreshes update guide data reliably.
- Ongoing maintenance effort decreases after initial setup.

### Primary Success Metric

> Consistently accurate program data for the channels being watched.

---

## Core Principles

### User Knowledge Is The Asset

The most valuable information in the system is not the source data.

The most valuable information is:

- Channel selections
- Channel ordering
- Channel naming decisions
- Logo selections
- EPG mappings
- Stream preferences
- Offset corrections
- Operational knowledge

Source data can always be refreshed.

User curation must be preserved.

---

### Refreshes Must Preserve User Work

Source refreshes should:

- Inform users of changes.
- Flag problems.
- Identify possible renames.
- Preserve existing configuration.

Refreshes must not automatically destroy user-maintained mappings and overrides.

---

### Automation Must Be Explainable

Automation may:

- Suggest
- Recommend
- Detect
- Prioritize

Automation should not silently override user decisions.

Manual decisions always take precedence.

---

### Cached Data Is Preferred

The system should operate primarily from cached source data rather than repeatedly downloading information from providers.

Refreshing playlist source data should be an explicit operation.

EPG source data may be refreshed automatically through approved scheduled refresh operations.

---

## Stakeholders

### Primary User

The project owner.

The application is designed as a single-user self-hosted solution.

### System Consumers

- TiViMate
- IPTV playlist providers
- EPG providers

---

## Functional Requirements

## Source Management

### Source Types

The system shall support:

- M3U Playlist Sources
- EPG Sources

### Source Definition

A source consists of:

- Name
- Type
- URL
- Priority

### Source Priorities

Playlist priorities and EPG priorities are independent.

Examples:

```text
Provider A
M3U Priority = 10
EPG Priority = 50

Provider B
M3U Priority = 20
EPG Priority = 20
```

Lower numbers represent higher priority.

---

## Source Refresh

### Full Refresh

A manual full refresh operation shall be supported.

A full refresh performs:

- Refresh All M3U Sources
- Refresh All EPG Sources
- Refresh All Outputs

Scheduled refreshes shall not perform a full refresh.

### Manual Refresh Operations

The following actions should be supported:

- Refresh All
- Refresh All Sources
- Refresh All Outputs
- Refresh All M3U Sources
- Refresh All EPG Sources
- Refresh Individual Source
- Refresh M3U Output
- Refresh EPG Output

### Scheduled Refresh

Version 1 supports:

- One nightly EPG refresh schedule.
- Scheduled refreshes apply only to EPG sources.
- Successful scheduled EPG refreshes automatically regenerate the EPG output.

Playlist source refreshes remain manual operations.

M3U output generation remains a manual operation.

Advanced scheduling is deferred

### Automation Boundaries

Automatic refresh operations are intentionally limited.

The system may automatically:

- Refresh EPG sources.
- Update cached EPG data.
- Generate EPG outputs.
- Record refresh history.

The system shall not automatically:

- Refresh M3U sources.
- Trigger channel discovery workflows.
- Trigger playlist reconciliation workflows.
- Generate M3U outputs.
- Modify user-maintained channel configuration.

Changes affecting channel curation require explicit user action.

---

## Source Caching

### Cached Inputs

Downloaded M3U and EPG files should be stored locally.

Configuration and management workflows should operate against cached data.

### Cached Outputs

Generated outputs should be stored on disk and served directly.

TiViMate requests must never trigger regeneration.

---

## Channel Discovery

### Discovery Model

The application should display all discovered channels from all configured playlist sources.

### Unique Channels

The exact definition of channel uniqueness remains an evolving area.

Future matching strategies may include:

- Channel names
- Call signs
- Geographic indicators
- Logos
- Existing mappings

Version 1 should prioritize correctness over automation.

---

## Channel Curation

### Channel Selection

Users should be able to:

- Include channels
- Exclude channels

### Channel Editing

Users should be able to:

- Rename channels
- Override logos
- Configure EPG mappings
- Configure offsets

### Channel Ordering

Users should be able to explicitly define channel order.

Channel numbers are not required.

### Channel Groups

Not required initially.

Generated output may place all channels in a single group:

```text
Favourites
```

---

## Original Versus Effective Values

The application must preserve original source values.

Examples:

- Original Name
- Original Logo
- Original tvg-id

User modifications should be stored separately as overrides.

Generated outputs should use effective values.

---

## Stream Selection

### Version 1

When multiple streams exist for the same selected channel:

```text
Lowest Priority Value Wins
```

### Future Capability

Users may be allowed to explicitly override stream selection.

Alternative candidate streams should remain visible where practical.

---

## EPG Management

### Automatic Mapping

When possible:

```text
tvg-id
```

should be used to establish initial mappings.

### Manual Mapping

Users should be able to:

- Search EPG channels.
- Select mappings manually.
- Override automatic mappings.

### Suggested Mapping

Future versions may provide likely matches.

User confirmation remains authoritative.

---

## Multiple EPG Mappings

A selected channel may contain multiple EPG mappings.

Example:

```text
ABC Buffalo

EPG A (Priority 10)
EPG B (Priority 20)
EPG C (Priority 30)
```

---

## EPG Priority Rules

### Higher Priority Wins

When multiple sources supply program information:

```text
Lowest Numeric Priority Value Wins
```

### Gap Filling

If a higher-priority EPG lacks data for a period:

```text
Use lower-priority EPG data.
```

### Non-Replacement Rule

Lower-priority program data cannot replace valid higher-priority program data.

Lower-priority sources only fill gaps.

---

## EPG Offsets

Offsets are configured at:

```text
Channel + EPG Mapping
```

level.

Examples:

```text
ABC Buffalo
EPG A
Offset = -1 Hour
```

Applying offsets at only the channel or source level has proven insufficient.

---

## Output Generation

### M3U Output

Generate a single curated M3U output.

### EPG Output

Generate a single XMLTV-based EPG output.

Compress output where appropriate.

Example:

```text
guide.xml.gz
```

### Build Strategy

Outputs are always generated from scratch.

Generation pipeline:

```text
Cached Sources
     +
Configuration
     +
Mappings
     +
Priorities
     +
Offsets
     =
Generated Output
```

No incremental updates should occur.

### Output Regeneration Rules

Successful scheduled EPG refreshes should automatically regenerate the EPG output.

M3U outputs are regenerated only through explicit user actions.

Refreshing M3U source data does not automatically generate outputs.

TiViMate requests must never trigger regeneration

---

## Refresh Reporting

Every refresh operation should create a history entry.

### Suggested Metrics

- Timestamp
- Trigger Type
- Refresh Scope
- Status
- Sources Refreshed
- Sources Failed
- Outputs Generated
- New Channels
- Missing Channels
- Potential Renames
- Auto Mappings Created
- Channels Missing EPG
- Errors

Possible Refresh Scope values:

- EPG
- M3U
- Full
- Output Only

## Manual Refresh

Completion should provide immediate feedback.

## Scheduled Refresh

Results should be available through history.

---

## Missing Channel Handling

Channels should never be automatically removed.

When source channels disappear:

- Preserve configuration.
- Preserve mappings.
- Preserve overrides.
- Preserve ordering.
- Mark the channel for review.

Potential statuses:

```text
Missing
Needs Attention
Possible Rename
```

The application should assume source data may change unexpectedly and should protect user effort accordingly.

---

## Backup And Recovery

### Configuration Snapshots

Users should be able to create named snapshots.

Examples:

```text
Stable Setup
Movie Lineup
Super Bowl Configuration
```

Snapshots should capture:

- Sources
- Priorities
- Channel selections
- Ordering
- Mappings
- Offsets
- Overrides

Snapshots must be exportable and restorable.

---

## Future Roadmap

The following ideas are intentionally deferred:

- Automatic channel matching
- Match confidence scoring
- Rename detection
- Stream override selection
- Stream reliability annotations
- Multiple curated lineups
- Sports-specific outputs
- Movie-specific outputs
- Event-specific outputs
- Configurable guide time horizons
- Advanced scheduling
- Source relationships
- Enhanced reconciliation workflows

These should only be implemented when a demonstrated need exists.

---

## Mission Statement

EPG Manager is a channel and guide curation platform whose primary responsibility is preserving user knowledge while continuously transforming unstable IPTV source data into a reliable, accurate, and consumable viewing experience for TiViMate.
