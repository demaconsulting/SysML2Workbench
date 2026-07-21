### ElementPicker

![ElementPickerSubsystem Structure](ElementPickerSubsystemView.svg)

#### Purpose

ElementPicker is the shared, reusable Avalonia control and view model that
lets any dialog in this application present a filterable list of workspace
elements. It combines a chip-row of type-label filters (OR semantics), a
case-insensitive substring search over qualified names (AND-combined with the
chip filter), and a single-select list of the remaining candidates. It is
documented as one unit covering both `ElementPickerViewModel` (the
Avalonia-free filtering state and logic) and `ElementPickerView` (the
Avalonia `UserControl` hosting the chip row / add-flyout / search box /
candidate list), matching the pairing convention used elsewhere in this
codebase. `ElementTypeLabeler` is a static helper shipped alongside the unit
so every caller derives the same label from the same `SysmlNode`.

#### Data Model

**Candidates**: `IReadOnlyList<(string QualifiedName, string TypeLabel)>` —
the master unfiltered list, replaced whole by every `SetCandidates` call.
Held privately; callers observe only the derived properties below.

**AvailableTypeLabels**: `IReadOnlyList<string>` — every distinct human-readable
"type label" present among the current candidates, sorted ordinally. Used to
populate the "+" add-filter flyout's list of addable labels.

**ActiveTypeFilters**: `ObservableCollection<string>` — the type-label chips
currently applied over the picker, combined with OR semantics; empty means
no type restriction (every candidate's type is shown). Pre-populated by
`SetCandidates` to the caller-requested `defaultTypeFilterLabel` when
present in the new candidates, otherwise empty. Mutated only through
`AddTypeFilter`/`RemoveTypeFilter` (or, defensively, any other direct
mutation, which is also observed by the internal collection-changed handler
that re-runs the filter recompute), so a chip-row `ItemsControl` can bind
directly.

**SearchText**: `string?` — the picker's free-text search box value, two-way
bound from the view. A non-empty value narrows `DisplayedItems` to
qualified names containing it as a case-insensitive substring, applied with
AND semantics against whatever the active type filter already narrowed to.

**DisplayedItems**: `IReadOnlyList<string>` — the candidate `ListBox`'s
actual data source: candidates narrowed first by `ActiveTypeFilters` (OR
semantics) and then by `SearchText` (AND semantics), preserving the master
list's order. Recomputed whenever any of the three inputs changes.

**SelectedQualifiedName**: `string?` — the currently-selected qualified name
in the candidate `ListBox`; two-way bound. Cleared to `null` by every
`SetCandidates` call so a stale prior selection cannot linger after a
workspace-derived refresh.

#### Key Methods

**SetCandidates**: Replaces the master candidate list, recomputes derived
state, and clears any prior selection.

- *Parameters*: `IReadOnlyList<(string QualifiedName, string TypeLabel)>
  candidates` — the caller-built candidate set (order preserved verbatim);
  `string? defaultTypeFilterLabel` — optional label to pre-populate
  `ActiveTypeFilters` with when present in the new candidates.
- *Returns*: `void` — `AvailableTypeLabels`, `ActiveTypeFilters`,
  `DisplayedItems`, and `SelectedQualifiedName` all update in place.
- *Preconditions*: `candidates` is not `null` (throws
  `ArgumentNullException` on `null`).
- *Postconditions*: `AvailableTypeLabels` is distinct, ordinal-sorted;
  `ActiveTypeFilters` contains at most the requested default label;
  `SelectedQualifiedName` is `null`.

**AddTypeFilter** / **RemoveTypeFilter**: Add or remove a type label from
`ActiveTypeFilters` and recompute `DisplayedItems`.

- *Parameters*: the type label to add or remove.
- *Returns*: `void` — `ActiveTypeFilters` and `DisplayedItems` update in
  place.
- *Postconditions*: `AddTypeFilter` is dedupe-safe (adding an already-active
  label is a no-op beyond the recompute); `RemoveTypeFilter` no-ops when the
  label is not currently active, rather than throwing.

**GetAddableTypeLabels**: Returns the labels in `AvailableTypeLabels` that
are not currently in `ActiveTypeFilters`.

- *Parameters*: none.
- *Returns*: `IReadOnlyList<string>` — computed on demand rather than cached,
  so the "+" add-filter flyout always reflects the current addable set the
  moment it opens.

#### Error Handling

The picker never itself talks to a workspace or file system, so it has no
"external I/O failure" surface to handle. Its only guarded input is the
`candidates` argument to `SetCandidates`, which is validated with a null
guard because a `null` candidate list is a programming error the caller must
fix rather than a runtime failure to recover from.

#### Dependencies

- **CommunityToolkit.Mvvm** — `ObservableObject` and `[ObservableProperty]`
  source generators back the derived state and change notification.
- **Avalonia** — `ElementPickerView` is a `UserControl` hosted by its
  callers' dialog `Window`s; nothing else in the subsystem depends on
  Avalonia.
- **DemaConsulting.SysML2Tools.Semantic.Model** — consumed by the
  `ElementTypeLabeler` static helper only, to compute a stable human-readable
  label from any `SysmlNode` subtype.

#### Callers

- **ViewBuilderDialog** — hosts one picker instance (`ExposeTargetPicker`)
  in its "Expose Targets" tab, pre-populating a `"part"` default chip.
- **QueryDialog** — hosts two picker instances (`BrowsePicker` for the
  Browse tab, `ElementQueryPicker` for the Element Query tab), neither with
  a default chip so every candidate shows immediately.
