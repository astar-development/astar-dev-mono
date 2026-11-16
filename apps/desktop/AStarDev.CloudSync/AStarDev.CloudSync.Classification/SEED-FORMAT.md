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
- `kind` is a seed-time hint only. `ClassificationTerm` does not carry it and the schema does not store it. At match time an extension term is matched by `ExtensionSignal`, a folder term by `FolderSegmentSignal`, and a word signal can also match either, because every term is just text in a category.
- The same text must not be seeded under two kinds in one category: the kinds would collapse into one stored term. `GivenTheSeedTermKinds` reads each seed file and fails when any text and category pair appears under more than one kind.
- `text` is lower case, single spaced; multi-word phrases are allowed ("red car").
- Every term names a category in the same or another seed file; terms with an unknown category are dropped.
- `source` records where the list came from and its licence. The loader ignores it.
- Files are embedded from `Seed/**/*.json`; the loader merges them in resource-name order and joins their versions with `+`.
- Shipped files live in `Seed/` (`colour.json`, `clothing.json`, `object.json`, `car.json`, `place.json`, plus the default categories, extensions and folders). `Colour` is the single British-spelling root; `Color` is never a category.
- A term may sit in two categories only when the test allow list in `GivenTheEmbeddedSeedVocabularies` names it.
- Ignored terms are never recreated by seeding.

## Places (`place.json`)

- Category `Place`: country names from the GeoNames `countryInfo` dump plus every GeoNames city with population of at least 15,000 (`cities15000`). Data is CC BY 4.0, attributed in `THIRD-PARTY-NOTICES.md`.
- Names use the ASCII name, lower case, hyphens turned into spaces, single spaced, letters and spaces only, at least 3 characters. Names with digits, apostrophes or other characters are dropped. Names are de-duplicated.
- The file has no per-term confidence, so an ambiguous place name is skipped rather than seeded at low confidence. A skipped name is a common English word, a very common given name, or a name already seeded in another vocabulary (colour, clothing or car wins; `peru` is allow-listed in both Place and Colour). The full list is `PlaceSkipList` in the test project; the tests fail if any entry appears in `place.json`.
- Skipped: acre, adam, airport, alice, allen, alliance, alma, aloha, along, amos, ann, anna, annex, antelope, anthem, apex, arona, ascot, aurora, badger, bah, bake, bank, bar, barking, barry, bath, bay, bear, bell, bend, bentley, best, bishop, bogue, boo, boom, boulder, bountiful, bow, bra, brick, bridge, buffalo, bury, bush, butte, butterfly, buy, can, canton, carrefour, cat, cay, cayenne, centennial, central, centro, chad, chandler, charlotte, chelsea, chester, china, chinatown, clay, cloverleaf, cocoa, cognac, coin, colon, come, commonwealth, concord, confederation, converse, cork, corona, cove, crystal, dale, date, david, deal, delta, derby, despatch, dig, dire, dome, drama, draper, dura, eagle, eden, elk, enterprise, eureka, evergreen, extrema, fate, federal, felling, fishers, fleet, flint, flora, forest, fountain, gap, gay, george, georgia, god, goes, golden, gondola, gore, gorgonzola, gouda, grapevine, grays, green, groves, hale, harper, hawthorn, hazelwood, hercules, hermitage, highland, hillside, hire, hit, holiday, honcho, honda, hook, hoover, hope, horn, hub, hull, humble, hurricane, hurst, ibiza, imperial, independence, jam, jasper, jersey, jordan, kim, kong, kos, laguna, lancing, lander, largo, leek, lend, lens, leo, leon, liberal, liberty, lice, lido, lira, loos, lop, luau, mach, magenta, magna, male, man, manage, mango, manly, march, marina, marks, marsala, martin, mary, mascot, mason, massa, mercedes, mesa, metro, mile, mine, minot, mission, mobile, mocha, mon, moss, most, much, mustang, nice, normal, olympic, opportunity, oral, orange, orchards, orion, overland, pace, papa, papaya, para, paradise, paramount, parole, pearl, pen, perm, plantation, plaque, plum, plunge, police, pontiac, pop, porta, portage, posse, pout, prosper, pueblo, puma, punch, punchbowl, quarto, queens, reading, redding, regina, republic, reservoir, retreat, rich, rouge, rugby, rye, sale, salt, samba, same, sandwich, sandy, sari, savage, save, say, sedan, semi, shaw, shirley, slough, sparks, split, spring, springs, sue, summit, sunrise, sunset, superior, surprise, sweetwater, sycamore, tank, temple, tequila, terrace, than, time, tire, torrent, toyota, turbo, turkey, tyre, una, union, university, upland, uptown, van, vicar, vincent, vineyard, vista, vulcan, wadi, waldorf, wedding, weir, wells, wheeling, wildwood, winder, worms, wright, young, zion.
- Regenerate by downloading `cities15000.zip` and `countryInfo.txt` from https://download.geonames.org/export/dump/ and rerunning the one-off generator (kept outside the repo). Raise the threshold if the gzip -9 size nears the 2 MB budget; the 15,000 threshold gives about 155 KB compressed.
