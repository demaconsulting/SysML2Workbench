## ElementPickerSubsystem

![ElementPickerSubsystem Structure](ElementPickerSubsystemView.svg)

### Overview

ElementPickerSubsystem owns the shared, dialog-agnostic "element picker"
control that more than one modal dialog in this application needs to reuse.
The picker presents a chip-row of type-label filters (OR semantics), a
case-insensitive substring search over qualified names (AND-combined with the
chip filter), and a single-select list of the remaining candidates. Its
boundary starts at the caller-supplied candidate list (qualified name + type
label pairs) and ends at the caller's selection observation - the picker
neither knows nor cares which workspace, node kind, or exclusion set produced
the candidates. Its documented unit is ElementPicker.

The subsystem exists because the Custom View Builder dialog's expose-target
picker and the Query dialog's Browse / Element Query pickers all needed the
exact same OR-then-AND filtering behavior. Keeping the picker embedded in one
of them would either force duplication of the filter logic into the other or
create an awkward coupling of the second dialog onto the first's
domain-specific view-definition model.

### Interfaces

**Candidate Feed API**: In-process interface accepting the caller-built list
of candidate qualified names and their type labels.

- *Type*: In-process .NET API.
- *Role*: Provider.
- *Contract*: Accepts a candidate list plus an optional default type-filter
  label; recomputes the addable-labels set, the active-chip pre-population,
  and the displayed list.
- *Constraints*: Order-preserving (the picker never re-sorts what the caller
  handed it); dedupe-safe on type labels; must clear any prior selection so a
  stale qualified name cannot linger after a refresh.

**Selection Observation API**: The picker's observable properties (displayed
list, active chips, selected qualified name, search text).

- *Type*: `INotifyPropertyChanged` (via CommunityToolkit.Mvvm's
  `ObservableObject`) plus one `ObservableCollection<string>` chip row.
- *Role*: Provider.
- *Contract*: Every filter or search-text edit updates `DisplayedItems`
  live; the caller can bind directly (as the Query dialog's Browse tab does)
  to regenerate a downstream result panel on every change.
- *Constraints*: Not thread-safe: all state must be mutated from a single
  (typically UI) thread.

### Design

1. Callers (the Custom View Builder dialog and the Query dialog) build the
   candidate list themselves, applying caller-owned exclusions (stdlib names,
   disallowed node kinds) and mapping each `SysmlNode` through the shared
   `ElementTypeLabeler` helper so every dialog produces the same label for
   the same node kind.
2. Callers hand the list to `ElementPickerViewModel.SetCandidates`, which
   recomputes `AvailableTypeLabels`, resets the active chip row (to the
   caller-requested default label when present, otherwise empty), clears any
   prior selection, and recomputes `DisplayedItems`.
3. The picker view (an Avalonia `UserControl`) hosts a chip row
   (`ItemsControl` + `WrapPanel` + per-chip remove button), a "+" add-filter
   `Button.Flyout` populated on demand from `GetAddableTypeLabels`, a search
   `TextBox` two-way-bound to `SearchText`, and a candidate `ListBox`
   two-way-bound to `SelectedQualifiedName`.
4. The `ElementTypeLabeler` helper is a plain static class - a pure data
   lookup with no external dependencies - because the mapping is identical
   for every caller and there is no reason to allow it to differ.
