# Project fixtures

Five small, invented projects (no real company or project). They are test data for measuring
whether the classifier recognizes the taxonomy's exact evidence (REQ-FACTS B1/B2), and whether it
claims evidence that isn't there: packages, Aspire calls, compose images and file patterns. They are
never built; they sit outside `src/` so no project compiles them.

Layout:

- `projects/<fixture>/`: the fixture itself. It holds no answers.
- `expected/<fixture>.json`: the scoring key for that fixture. `where` paths are relative to the
  fixture's root.

## Key format

- `components`: each with `name`, `path` and `exact`, every exact match the taxonomy's `signals` and
  `files` produce there. An entry is `{ category, facet, evidence, where }`, optionally
  `scored: false` with a `why`, or a `why` that explains how the match was read.
  `evidence` is a file pattern from the category's `files`, or the text found in `where`
  (the concrete name when the signal has a wildcard, such as `redis:7` for `redis:* (image)`).
- `outside`: the same entries for files that belong to no component (a root `*.slnx`).
- `distractors`: text that looks like evidence but isn't (prose, comments, near-miss names).

Signal forms: a package signal matches a `PackageReference`, `(npm)` a `package.json` dependency,
`(image)` a compose `image:`, and `AddX(` a `.AddX(` call. A name must match whole (`pg-promise`
is not `pg`).

## Scoring rules

B2 runs on a copy of each fixture outside the repo, so neither `expected/` nor repo context can be
reached.

- Miss: an entry with `scored` not false whose category the classifier omits for that component.
- Unsupported exact match: the classifier cites exact evidence that isn't in the key. A category
  whose only cited evidence is a distractor (commented-out line, prose or near-miss name) counts.
- An omitted category that is an ancestor of an included kind (frontend when react is included) is
  not a miss: the gateway expands it.
- Components are matched by path. Also report a fixture-level union score (categories over all the
  fixture's components and `outside`), so evidence attributed to the other component isn't a miss.
- `scored: false` entries (node on a browser app: a file pattern only, and the taxonomy describes
  node as a server runtime) are never counted as a miss. Citing them is not an error.
- Variance: the same fixture and component giving different exact categories across the three runs.
- Categories only inferred from meaning (for example postgres from an Aspire EF Core package) are
  not scored either way.

`ProjectFixtureTests` checks each key against the taxonomy and the files: every entry is a real
signal or file pattern present outside comments, every file pattern match and every declared
package, image or call that matches a signal is keyed, no entry is also a distractor, and every
fixture has a key and every key a fixture.
