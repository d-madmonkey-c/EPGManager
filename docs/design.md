📦 EPG Manager – Project Summary for GitHub Copilot
🎯 Project Goal
A Raspberry Pi–hosted internal web service that:
•  Downloads a primary M3U playlist + multiple XMLTV EPG sources
•  Maps channels across sources
•  Lets the user curate, reorder, and recategorize channels
•  Outputs:
•  A trimmed, ordered, recategorized M3U
•  A merged XMLTV EPG

🧱 Current Architecture
Backend
•  .NET 8 Minimal API
•  BackgroundService refreshes data daily
•  Config stored in
•  Outputs stored in memory ()
•  M3U + XMLTV parsing and merging logic implemented
Frontend
•  Pure HTML + CSS + JS (no frameworks)
•  Three‑panel UI:
•  Left: Available channels (grouped, collapsible)
•  Middle: Selected channels (ordered, drag‑and‑drop)
•  Right: Channel settings (per‑channel rules)

📁 Current File Structure

📄 Models

🧠 Key Features Implemented
✔ M3U Parsing
Extracts:
•  tvg-id
•  tvg-name
•  tvg-logo
•  group-title
•  display name
✔ Channel Mapping
Maps M3U → UTC XML → EPG_CA XML using:
•  tvg-id
•  display-name normalization
✔ Recategorization
Per‑channel overrides:
•  group-title
•  tvg-name
•  tvg-logo
•  hidden flag
✔ Ordered Output
Primary output builder respects:
•   ordering
•  per‑channel rules
✔ Secondary Output
Merged XMLTV from:
•  UTC XML
•  EPG_CA XML
Filtered by selected channels.
✔ UI
Three‑panel layout with:
•  Collapsible groups
•  Click‑to‑add channels
•  Click‑to-edit settings
•  Drag‑and‑drop channel ordering

🧩 JavaScript Features Implemented
✔ Add channel to selected list
✔ Show settings panel
✔ Save per‑channel rules
✔ Drag‑and‑drop reordering
✔ Collapsible groups
✔ Serialize everything before submit

🚧 Next Planned Steps (for Copilot to help with)
These are the next logical enhancements — perfect tasks for GitHub Copilot to assist with:

1. Group Ordering Panel
•  A new panel (or modal) listing all groups
•  Drag‑and‑drop to reorder groups
•  Save to
•  Use group order when generating M3U
2. Group‑Level Rules
•  Rename group
•  Hide group
•  Apply default logo/name rules to all channels in group
3. Channel Removal
•  Add “remove” button to selected list
4. Auto‑sync SelectedChannels with AvailableChannels
•  Remove channels that no longer exist in the M3U
•  Add new channels to the left panel automatically
5. Preview Mode
•  Show a preview of the generated M3U and XMLTV before saving
6. Time‑Shift Support
•  Per‑channel or global EPG time offset
7. Persist UI State
•  Remember collapsed groups
•  Remember last selected channel
