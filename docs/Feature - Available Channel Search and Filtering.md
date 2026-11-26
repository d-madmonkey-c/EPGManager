# Feature: Available Channel Search and Filtering

## Status

Proposed

---

## Summary

Provide search and filtering capabilities for the Available Channels list to help users efficiently navigate and curate large channel collections.

This feature is strictly a UI convenience layer. Filtering affects only the channels currently displayed in the Available Channels view and must never modify persisted configuration or generated outputs.

No filtering operation shall affect:

- Channel selection state
- Channel ordering
- EPG mappings
- Channel overrides
- Stream selection
- Saved configuration
- Generated M3U output
- Generated EPG output

Filters represent temporary view state only.

---

## Problem Statement

Playlist sources may contain thousands of channels.

As source counts increase, locating channels for review, selection, or configuration becomes increasingly difficult.

Users need the ability to quickly:

- Find channels by name
- Review newly discovered channels
- Exclude unwanted groups from the current view
- Focus on a subset of channels during curation

These operations must not alter any curated data or saved configuration.

---

## Goals

- Support free-form channel search.
- Support attribute-based filtering.
- Support filtering for newly discovered channels.
- Support hiding selected channel groups.
- Support combining multiple filters.
- Provide immediate visual feedback.
- Preserve all user curation and configuration.
- Keep the user experience simple and intuitive.

---

## Non-Goals

This feature will not:

- Change channel selection state.
- Save filtered views.
- Modify generated outputs.
- Introduce channel group management.
- Automatically organize channels.
- Affect channel matching behavior.
- Persist filter preferences.

---

## User Stories

### Search Channels

As a user,

I want to search for channel names,

So that I can quickly locate channels within large playlists.

### Review New Channels

As a user,

I want to view only newly discovered channels,

So that I can efficiently review additions after a refresh.

### Hide Unwanted Groups

As a user,

I want to hide channels belonging to specific groups,

So that I can focus on channels relevant to my current task.

### Combine Filters

As a user,

I want to combine multiple filters,

So that I can rapidly narrow the available channel list.

---

## Functional Requirements

### Free-Form Search

The system shall provide a search box for the Available Channels list.

Search behavior:

- Case-insensitive
- Partial text matching
- Results update as the user types
- Search performed against the effective channel name

Example:

Input:

```text
sports
```

Results:

```text
Sportsnet East
Sportsnet Ontario
NBC Sports Boston
```

---

### New Channels Filter

The system shall provide a:

```text
New Channels
```

filter.

A channel is considered new when it was discovered during the most recent refresh and does not already exist within previously known channel data.

When enabled:

- Only channels matching the New status shall be displayed.

---

### Hidden Groups Filter

The system shall allow users to hide channels belonging to selected source groups.

Example:

```text
Hidden Groups:
- Adult
- Regional
- US Entertainment
```

Channels belonging to hidden groups shall not appear in the filtered results.

The hidden groups list is temporary and applies only to the current UI session.

---

### Combined Filtering

The system shall allow multiple filters to be active simultaneously.

Supported combinations include:

- Search Text + New Channels
- Search Text + Hidden Groups
- Search Text + New Channels + Hidden Groups

A channel must satisfy all active filters to be displayed.

---

### Result Counts

The UI shall display a filtered result count.

Example:

```text
Showing 42 of 3,128 channels
```

This count should update immediately as filters change.

---

### Clear Filters

The system shall provide a single action to clear all active filters.

When activated:

- Search text is cleared
- New Channel filter is disabled
- Hidden Groups are removed
- The complete Available Channel list is displayed

---

## UI Behavior

### Scope

Filtering applies only to the Available Channels list.

Other application views shall not be affected.

---

### Session Behavior

Initial implementation:

- Filters exist only in client-side UI state
- Filters are not persisted
- Page refresh resets all filters

This keeps the implementation simple and avoids introducing preference management.

---

### Empty Results

When no channels match the current filter set, the UI shall display:

```text
No channels match the current filters.
```

A clear action should be provided to reset filters.

---

## Data Requirements

No persistence changes are required.

Filter state should exist only within the UI.

Example model:

```csharp
public sealed class AvailableChannelFilter
{
    public string? SearchText { get; set; }

    public bool ShowNewOnly { get; set; }

    public HashSet<string> HiddenGroups { get; set; } = [];
}
```

This state must never be written to configuration storage.

---

## Acceptance Criteria

### AC1 - Search

**Given** channels exist matching the entered text

**When** the user types into the search box

**Then** matching channels shall be displayed immediately

---

### AC2 - New Channels Filter

**Given** newly discovered channels exist

**When** the user enables the New Channels filter

**Then** only newly discovered channels shall be displayed

---

### AC3 - Hidden Groups

**Given** one or more groups are hidden

**When** channels belong to those groups

**Then** those channels shall not appear in the Available Channels list

---

### AC4 - Combined Filters

**Given** multiple filters are active

**When** channels are displayed

**Then** only channels matching all active filters shall be shown

---

### AC5 - Configuration Preservation

**Given** any filtering operation

**When** filters are added, modified, cleared, or combined

**Then** no persisted configuration shall be changed

---

### AC6 - Output Independence

**Given** active filters exist

**When** M3U or EPG outputs are generated

**Then** generated outputs shall be identical to those produced with no filters active

---

## Technical Notes

- Filtering should occur client-side where practical.
- Filtering must be fast enough to support real-time typing.
- The implementation should be independent of channel selection logic.
- Filtering should be treated as presentation state rather than domain state.

---

## Future Enhancements

Potential future filters:

- Selected Channels Only
- Unselected Channels Only
- Missing EPG Mapping
- Missing Source Channel
- Needs Attention
- Possible Rename
- Recently Changed
- Source Playlist
- Duplicate Candidate Channels

These enhancements should remain out of scope for the initial implementation.
