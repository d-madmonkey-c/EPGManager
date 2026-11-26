# EPG Manager Copilot Instructions

## Mission

EPG Manager combines multiple IPTV playlist and EPG sources into a single curated TiViMate experience.

The primary objective is preserving user curation while producing accurate guide data.

## Core Rules

### User Curation Is Sacred

Always preserve:

- Channel selections
- Channel ordering
- EPG mappings
- Overrides
- Offsets
- User-defined metadata

Source data is replaceable. User curation is not.

### Preserve Before Replace

When source data changes:

- Preserve existing configuration.
- Flag problems for review.
- Surface possible matches and renames.
- Never silently remove user work.

### Automation Assists

Automation may:

- Suggest
- Detect
- Recommend

Automation must not:

- Override user decisions
- Delete user configuration
- Replace manual mappings

### Generated Data Is Disposable

Treat these as generated artifacts:

- Cached source data
- Generated M3U files
- Generated XMLTV files

Treat these as authoritative:

- Configuration
- Channel selections
- Mappings
- Overrides
- Ordering

Outputs must always be reproducible from configuration.

## Architecture

Prefer:

- Simple solutions
- Existing repository patterns
- File-based persistence
- Explicit behavior

Avoid:

- Infrastructure complexity
- Enterprise patterns
- Databases without a demonstrated need
- Premature abstraction

This is a single-user, self-hosted application targeting Raspberry Pi hardware.

## Refresh Safety

Assume providers are unreliable.

Refresh operations must preserve:

- Selections
- Ordering
- Mappings
- Overrides

Refreshes should never destroy user work.

## Working Style

Before implementing:

1. Understand the request.
2. Review relevant repository documentation.
3. Inspect existing patterns.
4. Identify affected tests.
5. Recommend an approach when appropriate.

Prefer small, targeted changes over broad refactoring.

## Decision Filter

Prefer solutions that:

1. Preserve user curation.
2. Follow repository guidance.
3. Maintain existing behavior.
4. Reduce operational complexity.
5. Remain easy to understand and maintain.