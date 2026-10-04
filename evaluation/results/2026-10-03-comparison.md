# Retrieval evaluation: comparison

- **Date:** 2026-10-03 · **Commit:** c667422
- **Data:** seed (fictitious placeholder standards): 60 topics from 3 owners (standards `edb230d305d1`, golden set `33bd42062bb0`)

| Metric | tags-exact | tags-suggest |
|---|---|---|
| Mean recall | 6% | 14% |
| Found every expected topic | 4% | 13% |
| Found at least one | 7% | 16% |
| Returned nothing | 41 of 47 | 30 of 47 |
| Mean precision | 9% | 6% |
| Mean topics returned | 1.1 | 3.1 |
| Forbidden topics returned | 0 | 0 |
| No-standard tasks answered with nothing | 2 of 2 | 1 of 2 |
| Mean recall@5 | n/a (unranked) | n/a (unranked) |
| Category recall | 4% | 10% |
| Category precision | 50% | 25% |
