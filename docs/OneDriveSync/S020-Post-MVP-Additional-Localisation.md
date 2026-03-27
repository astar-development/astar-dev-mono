# S020 — [Post-MVP] Additional Locale Translations

**Phase:** Post-MVP  
**Area:** Infrastructure/Localisation  
**Spec refs:** LO-08

---

## User Story

As a non-English-speaking user,  
I want the app available in my language,  
So that I can use it comfortably without relying on English.

---

## Acceptance Criteria

- [ ] At least one additional locale added (locale to be determined by product decision)
- [ ] All `.resx` resource files translated for the new locale
- [ ] Locale selector in Settings shows the new locale option
- [ ] Date/time formatting adapts to the new locale's conventions
- [ ] No structural code changes required to add the locale — only new `.resx` files and registration

---

## Dependencies

- S005 (localisation foundation — infrastructure must support adding locales without code changes)
