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

- A term may carry `"kind": "word" | "extension" | "folder"`, default `"word"`. An extension term is the extension without the dot; a folder term is a folder name. An unknown kind makes the seed invalid. The same text may appear once per kind.
- `text` is lower case, single spaced; multi-word phrases are allowed ("red car").
- Every term names a category in the same or another seed file; terms with an unknown category are dropped.
- `source` records where the list came from and its licence. The loader ignores it.
- Files are embedded from `Seed/**/*.json`; the loader merges them in resource-name order and joins their versions with `+`.
- Shipped files live in `Seed/` (`colour.json`, `clothing.json`, `object.json`, `car.json`, plus the default categories, extensions and folders). `Colour` is the single British-spelling root; `Color` is never a category.
- A term may sit in two categories only when the test allow list in `GivenTheEmbeddedSeedVocabularies` names it.
- Ignored terms are never recreated by seeding.
