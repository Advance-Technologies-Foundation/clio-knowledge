# Test Coverage Checklist

Minimum coverage for feature-gated code:

1. Feature enabled path.
2. Feature disabled path.
3. Shared constant usage from `Constants.cs`.
4. Any observable output or branch difference caused by the feature state.

Prefer focused tests that prove the gated behavior instead of reproducing unrelated setup noise.
