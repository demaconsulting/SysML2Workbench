### ElementFilter

![ElementPickerSubsystem Structure](ElementPickerSubsystemView.svg)

#### Purpose

ElementFilter is the shared, reusable Avalonia control and view model that
lets any caller present a filter-only view of a candidate list of workspace
elements: a chip-row of type-label filters (OR semantics) and a
case-insensitive substring search over qualified names (AND-combined with
the chip filter). It has NO selection concept whatsoever. It is documented
as one unit covering both `ElementFilterViewModel` (the Avalonia-free
filtering state and logic) and `ElementFilterView` (the Avalonia
`UserControl` hosting the chip row / add-flyout / search box), matching the
pairing convention used elsewhere in this codebase.

`ElementPickerViewModel` composes one `ElementFilterViewModel` instance
(`Filter`) to reuse this exact filtering logic while adding its own
`SelectedQualifiedName` selection concept on top - see
`element-picker.md`. Callers that have no target-element concept at all -
such as the Query dialog's "List" Query Type, which is a purely
client-side filter over the workspace with no notion of a "selected"
element - use a standalone `ElementFilterViewModel`/`ElementFilterView`
instance directly instead, avoiding a selectable candidate list whose
selection would otherwise be silently ignored.

#### Data Model

**Candidates**: `IReadOnlyList<(string QualifiedName, string TypeLabel)>` —
the master unfiltered list, replaced whole by every `SetCandidates` call.
Held privately; callers observe only the derived properties below.

**AvailableTypeLabels**: `IReadOnlyList<string>` — every distinct human-readable
"type label" present among the current candidates, sorted ordinally. Used to
populate the "+" add-filter flyout's list of addable labels.

**ActiveTypeFilters**: `ObservableCollection<string>` — the type-label chips
currently applied over the filter, combined with OR semantics; empty means
no type restriction (every candidate's type is shown). Pre-populated by
`SetCandidates` to the caller-requested `defaultTypeFilterLabel` when
present in the new candidates, otherwise empty. Mutated only through
`AddTypeFilter`/`RemoveTypeFilter` (or, defensively, any other direct
mutation, which is also observed by the internal collection-changed handler
that re-runs the filter recompute), so a chip-row `ItemsControl` can bind
directly.

**SearchText**: `string?` — the filter's free-text search box value, two-way
bound from the view. A non-empty value narrows `DisplayedItems` to
qualified names containing it as a case-insensitive substring, applied with
AND semantics against whatever the active type filter already narrowed to.

**DisplayedItems**: `IReadOnlyList<string>` — the filtered, view-facing
result: candidates narrowed first by `ActiveTypeFilters` (OR semantics) and
then by `SearchText` (AND semantics), preserving the master list's order.
Recomputed whenever any of the three inputs changes.

#### Key Methods

**SetCandidates**: Replaces the master candidate list and recomputes
derived state. Unlike `ElementPickerViewModel.SetCandidates`, there is no
selection to clear - this view model has no selection concept.

- *Parameters*: `IReadOnlyList<(string QualifiedName, string TypeLabel)>
  candidates` — the caller-built candidate set (order preserved verbatim);
  `string? defaultTypeFilterLabel` — optional label to pre-populate
  `ActiveTypeFilters` with when present in the new candidates.
- *Returns*: `void` — `AvailableTypeLabels`, `ActiveTypeFilters`, and
  `DisplayedItems` all update in place.
- *Preconditions*: `candidates` is not `null` (throws
  `ArgumentNullException` on `null`).
- *Postconditions*: `AvailableTypeLabels` is distinct, ordinal-sorted;
  `ActiveTypeFilters` contains at most the requested default label.

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

The filter never itself talks to a workspace or file system, so it has no
"external I/O failure" surface to handle. Its only guarded input is the
`candidates` argument to `SetCandidates`, which is validated with a null
guard because a `null` candidate list is a programming error the caller must
fix rather than a runtime failure to recover from.

#### Dependencies

- **CommunityToolkit.Mvvm** — `ObservableObject` and `[ObservableProperty]`
  source generators back the derived state and change notification.
- **Avalonia** — `ElementFilterView` is a `UserControl` hosted directly by
  callers or embedded inside `ElementPickerView`; nothing else in the unit
  depends on Avalonia.

#### Callers

- **ElementPicker** (Unit) — composes one `ElementFilterViewModel` instance
  (`Filter`) to reuse the filtering logic for every selection-bearing
  caller (Custom View Builder, Query dialog's element-scoped Query Types).
- **QueryDialog** — uses one standalone `ElementFilterViewModel` instance
  (`FilterOnly`) directly for its "List" Query Type, which has no
  target-element concept and would otherwise present a selectable list
  whose selection is silently ignored.
