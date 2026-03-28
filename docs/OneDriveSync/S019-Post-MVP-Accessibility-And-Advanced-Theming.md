# S019 — [Post-MVP] Accessibility & Advanced Theming

**Phase:** Post-MVP  
**Area:** Infrastructure/Theming, all feature views  
**Spec refs:** TH-07, TH-08, TH-09, NF-11 (keyboard nav extended), NF-12 (screen readers), Section 15 (Accessibility)

---

## User Story

As a user who relies on assistive technology or has colour vision differences,  
I want high-contrast and colour-blind-friendly themes, and full keyboard navigation,  
So that the app is usable regardless of ability.

---

## Acceptance Criteria

- [ ] High-contrast theme resource dictionary added (TH-07)
- [ ] Colour-blind-friendly palette variant added (TH-07)
- [ ] All controls expose `AutomationProperties.Name` for screen reader compatibility (NF-12)
- [ ] Full keyboard-only operation for all interactions including folder tree and add-account wizard
- [ ] Custom accent colour selection UI (TH-09)
- [ ] Additional custom themes beyond Light/Dark/Auto (TH-08)

---

## Implementation Constraints

- `AutomationProperties.Name` is an Avalonia **attached property** — set it directly on each control element (e.g., `<Button AutomationProperties.Name="Close account"/>`). It cannot be applied reliably via a `Control.Styles` selector because the Avalonia styles scoping rules mean the property may not reach the intended element.
- Focus ring styling for custom controls (`IconRailButton` and any others introduced in this story) must **not** use `UserControl.Styles` selectors targeting the root control — they will be silently ignored (same scoping trap documented in S003 NF-15). Implement a `FocusAdornerTemplate` on the control template, or use `FocusAdorner` via `KeyboardNavigation` attached properties.

---

## Dependencies

- S004 (theming — extensible resource dictionary pattern)
- S003 (navigation shell — keyboard focus management)
