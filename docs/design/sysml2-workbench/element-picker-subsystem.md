## ElementPickerSubsystem

![ElementPickerSubsystem Structure](ElementPickerSubsystemView.svg)

### Overview

ElementPickerSubsystem owns two related units that more than one modal
dialog in this application needs to reuse: `ElementFilter`, a selection-free
chip-row (OR semantics) + case-insensitive substring search (AND-combined
with the chip filter) over a caller-supplied candidate list, and
`ElementPicker`, which composes an `ElementFilter` instance and adds a
single-select candidate list on top. Their shared boundary starts at the
caller-supplied candidate list (qualified name + type label pairs) and ends
at the caller's observation of `DisplayedItems` (and, for `ElementPicker`,
`SelectedQualifiedName`) - neither unit knows nor cares which workspace,
node kind, or exclusion set produced the candidates.

The subsystem exists because the Custom View Builder dialog's expose-target
picker and the Query dialog's element-scoped Query Types all need the exact
same OR-then-AND filtering behavior with a selection on top, while the Query
dialog's "List" Query Type needs that same filtering behavior with NO
selection concept at all (a purely client-side filter with no target
element). Splitting the filtering logic (`ElementFilter`) from the
selection-bearing composition (`ElementPicker`) lets both kinds of caller
reuse the identical chip/search behavior without either duplicating it or
presenting a selectable list whose selection would be silently ignored.

### Interfaces

**Candidate Feed API**: In-process interface accepting the caller-built list
of candidate qualified names and their type labels.

- *Type*: In-process .NET API.
- *Role*: Provider.
- *Contract*: Accepts a candidate list plus an optional default type-filter
  label; recomputes the addable-labels set, the active-chip pre-population,
  and the displayed list. Exposed identically by `ElementFilterViewModel`
  and (as a pass-through) by `ElementPickerViewModel`.
- *Constraints*: Order-preserving (neither unit re-sorts what the caller
  handed it); dedupe-safe on type labels; `ElementPickerViewModel` must
  additionally clear any prior selection so a stale qualified name cannot
  linger after a refresh (`ElementFilterViewModel` has no selection to
  clear).

**Selection Observation API**: The observable properties (displayed list,
active chips, search text, and - for `ElementPicker` only - selected
qualified name).

- *Type*: `INotifyPropertyChanged` (via CommunityToolkit.Mvvm's
  `ObservableObject`) plus one `ObservableCollection<string>` chip row.
- *Role*: Provider.
- *Contract*: Every filter or search-text edit updates `DisplayedItems`
  live; the caller can bind directly (as the Query dialog's "List" Query
  Type does against `ElementFilter`) to regenerate a downstream result
  panel on every change.
- *Constraints*: Not thread-safe: all state must be mutated from a single
  (typically UI) thread.

### Design

1. Callers (the Custom View Builder dialog and the Query dialog) build the
   candidate list themselves, applying caller-owned exclusions (stdlib names,
   disallowed node kinds) and mapping each `SysmlNode` through the shared
   `ElementTypeLabeler` helper so every dialog produces the same label for
   the same node kind.
2. Callers hand the list to `SetCandidates` on whichever unit they use
   directly (`ElementFilterViewModel` for a filter-only caller, or
   `ElementPickerViewModel` for a selection-bearing caller, which forwards
   to its composed `Filter`), which recomputes `AvailableTypeLabels`, resets
   the active chip row (to the caller-requested default label when present,
   otherwise empty), and recomputes `DisplayedItems` (`ElementPickerViewModel`
   additionally clears any prior selection).
3. The filter view (`ElementFilterView`, an Avalonia `UserControl`) hosts a
   chip row (`ItemsControl` + `WrapPanel` + per-chip remove button), a "+"
   add-filter `Button.Flyout` populated on demand from
   `GetAddableTypeLabels`, and a search `TextBox` two-way-bound to
   `SearchText`. The picker view (`ElementPickerView`) embeds an
   `ElementFilterView` bound to its `Filter` property for that exact markup,
   then adds its own candidate `ListBox` two-way-bound to
   `SelectedQualifiedName`.
4. The `ElementTypeLabeler` helper is a plain static class - a pure data
   lookup with no external dependencies - because the mapping is identical
   for every caller and there is no reason to allow it to differ.
