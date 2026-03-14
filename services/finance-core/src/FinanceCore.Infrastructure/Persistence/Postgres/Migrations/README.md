# Finance Core Migration Policy

This folder contains the PostgreSQL migration scripts for Finance Core.

- `V0001__finance_core_baseline.sql` is the single mutable baseline migration before the first intentional rollout
- Until an explicit rollout command is given, schema fixes must update `V0001__finance_core_baseline.sql` in place
- Do not create `V0002__...sql` or later scripts before the baseline is frozen for rollout
- Once the baseline is frozen and rolled out, all subsequent schema changes must be delivered as new versioned scripts
