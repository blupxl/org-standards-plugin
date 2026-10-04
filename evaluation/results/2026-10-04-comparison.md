# Retrieval evaluation: comparison

- **Date:** 2026-10-04 · **Commit:** 3010e51
- **Data:** seed (fictitious placeholder standards): 73 topics from 3 owners (standards `ed2dc83d51aa`, golden set `a437a7ce2d8a`)

| Metric | tags-exact | tags-suggest |
|---|---|---|
| Mean recall | 9% | 22% |
| Found every expected topic | 8% | 21% |
| Found at least one | 10% | 23% |
| Returned nothing | 41 of 50 | 29 of 50 |
| Mean precision | 15% | 9% |
| Mean topics returned | 1.8 | 5.2 |
| Forbidden topics returned | 0 | 1 |
| No-standard tasks answered with nothing | 2 of 2 | 1 of 2 |
| Mean recall@5 | n/a (unranked) | n/a (unranked) |
| Category recall | 5% | 12% |
| Category precision | 44% | 26% |
