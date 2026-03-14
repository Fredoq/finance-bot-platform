# Finance Core Migration Policy

- `V0001__finance_core_baseline.sql` is the single mutable baseline migration before the first intentional rollout
- Until an explicit rollout command is given, schema fixes must update `V0001__finance_core_baseline.sql` in place
- Do not create `V0002__...sql` or later scripts before the baseline is frozen for rollout
- After the baseline is frozen and applied intentionally, every next schema change must use a new versioned script
