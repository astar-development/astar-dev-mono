# Seed file format

Seed data ships as embedded JSON and is read through `IClassificationSeedSource`.
One file per vocabulary, all merged into one `ClassificationSeed`.

```json
{
  "version": "1",
  "categories": [ { "name": "Colour" } ],
  "terms": [ { "text": "red", "category": "Colour" } ]
}
```

- A term may carry `"kind": "word" | "extension" | "folder"`, default `"word"`. An extension term is the extension without the dot; a folder term is a folder name. An unknown kind makes the seed invalid. The same text may appear once per kind.
- `text` is lower case, single spaced; multi-word phrases are allowed ("red car").
- Every term names a category in the same or another seed file; terms with an unknown category are dropped.
- A header comment in each file's source repository entry records the list's source and licence.
- Files are embedded from `Seed/**/*.json`; the loader merges them in resource-name order and joins their versions with `+`.
- Ignored terms are never recreated by seeding.
