# Seed file format

Seed data ships as embedded JSON and is read through `IClassificationSeedSource`.
One file per vocabulary, all merged into one `ClassificationSeed`.

```json
{
  "version": "1",
  "source": "Where the list came from and its licence",
  "categories": [ { "name": "Colour" } ],
  "terms": [ { "text": "red", "category": "Colour" } ]
}
```

- `text` is lower case, single spaced; multi-word phrases are allowed ("red car").
- Every term names a category in the same or another seed file; terms with an unknown category are dropped.
- `source` records where the list came from and its licence. The loader ignores it.
- Shipped files live in `Seed/` (`colour.json`, `clothing.json`, `object.json`, `car.json`). `Colour` is the single British-spelling root; `Color` is never a category.
- A term may sit in two categories only when the test allow list in `GivenTheEmbeddedSeedVocabularies` names it.
- Ignored terms are never recreated by seeding.
