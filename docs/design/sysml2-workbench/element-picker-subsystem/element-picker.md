### ElementPicker

![ElementPickerSubsystem Structure](ElementPickerSubsystemView.svg)

#### Purpose

ElementPicker is the shared, reusable Avalonia control and view model that
lets any dialog in this application present a filterable, selectable list of
workspace elements. It composes an
`ElementPickerSubsystem.ElementFilter` instance (`Filter`) to own the
chip-row of type-label filters (OR semantics) and the case-insensitive
substring search over qualified names (AND-combined with the chip filter),
and adds its own single-select `SelectedQualifiedName` concept on top. It is
documented as one unit covering both `ElementPickerViewModel` (the
Avalonia-free composition and selection state) and `ElementPickerView` (the
Avalonia `UserControl` embedding an `ElementFilterView` for the chip
row/add-flyout/search box, plus the candidate list). `ElementTypeLabeler` is
a static helper shipped alongside the unit so every caller derives the same
label from the same `SysmlNode`. Callers with no target-element concept at
all - such as the Query dialog's "List" Query Type - use a standalone
`ElementFilter` instance directly instead of this unit; see
`element-filter.md`.

#### Data Model

**Filter**: `ElementFilterViewModel` — the composed filter that actually
owns the master candidate list, chip management, and search-text filtering.
Every filtering-related public member on this class
(`AvailableTypeLabels`, `SearchText`, `DisplayedItems`, `ActiveTypeFilters`,
`GetAddableTypeLabels`, `AddTypeFilter`, `RemoveTypeFilter`) is a thin
pass-through to this instance, so existing callers and bindings see no
change to this class's public API.

**AvailableTypeLabels**: `IReadOnlyList<string>` — pass-through to
`Filter.AvailableTypeLabels`: every distinct human-readable "type label"
present among the current candidates, sorted ordinally. Used to populate
the "+" add-filter flyout's list of addable labels.

**ActiveTypeFilters**: `ObservableCollection<string>` — pass-through to
`Filter.ActiveTypeFilters` (the same collection instance): the type-label
chips currently applied over the picker, combined with OR semantics; empty
means no type restriction (every candidate's type is shown). Pre-populated
by `SetCandidates` to the caller-requested `defaultTypeFilterLabel` when
present in the new candidates, otherwise empty.

**SearchText**: `string?` — pass-through to `Filter.SearchText`: the
picker's free-text search box value, two-way bound from the view. A
non-empty value narrows `DisplayedItems` to qualified names containing it
as a case-insensitive substring, applied with AND semantics against
whatever the active type filter already narrowed to.

**DisplayedItems**: `IReadOnlyList<string>` — pass-through to
`Filter.DisplayedItems`: the candidate `ListBox`'s actual data source:
candidates narrowed first by `ActiveTypeFilters` (OR semantics) and then by
`SearchText` (AND semantics), preserving the master list's order.

**SelectedQualifiedName**: `string?` — owned directly by
`ElementPickerViewModel` (not delegated to `Filter`, which has no selection
concept): the currently-selected qualified name in the candidate `ListBox`;
two-way bound. Cleared to `null` by every `SetCandidates` call so a stale
prior selection cannot linger after a workspace-derived refresh.

#### Key Methods

**SetCandidates**: Forwards to `Filter.SetCandidates`, then clears any
prior selection.

- *Parameters*: `IReadOnlyList<(string QualifiedName, string TypeLabel)>
  candidates` — the caller-built candidate set (order preserved verbatim);
  `string? defaultTypeFilterLabel` — optional label to pre-populate
  `ActiveTypeFilters` with when present in the new candidates.
- *Returns*: `void` — `AvailableTypeLabels`, `ActiveTypeFilters`,
  `DisplayedItems`, and `SelectedQualifiedName` all update in place.
- *Preconditions*: `candidates` is not `null` (throws
  `ArgumentNullException` on `null`, propagated from `Filter.SetCandidates`).
- *Postconditions*: `AvailableTypeLabels` is distinct, ordinal-sorted;
  `ActiveTypeFilters` contains at most the requested default label;
  `SelectedQualifiedName` is `null`.

**AddTypeFilter** / **RemoveTypeFilter**: Pass-through to
`Filter.AddTypeFilter`/`Filter.RemoveTypeFilter`.

- *Parameters*: the type label to add or remove.
- *Returns*: `void` — `ActiveTypeFilters` and `DisplayedItems` update in
  place.
- *Postconditions*: `AddTypeFilter` is dedupe-safe (adding an already-active
  label is a no-op beyond the recompute); `RemoveTypeFilter` no-ops when the
  label is not currently active, rather than throwing.

**GetAddableTypeLabels**: Pass-through to `Filter.GetAddableTypeLabels`:
returns the labels in `AvailableTypeLabels` that are not currently in
`ActiveTypeFilters`.

- *Parameters*: none.
- *Returns*: `IReadOnlyList<string>` — computed on demand rather than cached,
  so the "+" add-filter flyout always reflects the current addable set the
  moment it opens.

#### Error Handling

The picker never itself talks to a workspace or file system, so it has no
"external I/O failure" surface to handle. Its only guarded input is the
`candidates` argument to `SetCandidates`, forwarded to `Filter.SetCandidates`
which validates it with a null guard because a `null` candidate list is a
programming error the caller must fix rather than a runtime failure to
recover from.

#### Dependencies

- **ElementPickerSubsystem.ElementFilter** — composed as `Filter`; owns all
  filtering/chip-management logic and view markup.
- **CommunityToolkit.Mvvm** — `ObservableObject` and `[ObservableProperty]`
  source generators back `SelectedQualifiedName` and change notification.
- **Avalonia** — `ElementPickerView` is a `UserControl` hosted by its
  callers' dialog `Window`s; nothing else in the subsystem depends on
  Avalonia.
- **DemaConsulting.SysML2Tools.Semantic.Model** — consumed by the
  `ElementTypeLabeler` static helper only, to compute a stable human-readable
  label from any `SysmlNode` subtype.

#### Callers

- **ViewBuilderDialog** — hosts one picker instance (`ExposeTargetPicker`)
  in its "Expose Targets" tab, pre-populating a `"part"` default chip.
- **QueryDialog** — hosts one picker instance (`Picker`) used for the ten
  element-scoped Query Types (every `QueryVerb` other than the merged
  `List` entry), with no default chip so every candidate shows immediately.
  For its "List" Query Type, `QueryDialog` instead uses a standalone
  `ElementFilter` instance (`FilterOnly`) directly, since "List" has no
  target-element concept.
