# 09. Async and performance

**Classification:** Mandatory; optimizations evidence-based

## Required behavior
Use async I/O end-to-end, propagate CancellationToken where supported, bound expensive work and paginate large results. Cache only with clear ownership, authorization, expiry and invalidation. Measure before optimizing.

## Prohibited behavior and exceptions
No .Result/.Wait in async flow, async void except required events, fire-and-forget scoped services or parallel operations on one DbContext. CPU-only helpers need not be async.

## Examples
Good: await query.ToListAsync(ct); bad: Task.WhenAll on queries sharing a DbContext.

## Review checklist
- [ ] Check cancellation, memory, query count, lifecycle, concurrency and measured bottlenecks.
- [ ] Record relevant evidence and justified deviations; do not claim an unexecuted check.
