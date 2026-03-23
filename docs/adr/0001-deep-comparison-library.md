# ADR-0001: Use CompareNETObjects for Deep Comparison

**Status:** Approved
**Date:** 2026-03-17
**Context:** Issue #3 — deep comparison support for `RunResult`

## Decision

Use [CompareNETObjects](https://github.com/GregFinzer/Compare-Net-Objects) (NuGet: `CompareNETObjects`) rather than writing a custom deep comparison.

## Rationale

### Why couple to CompareNETObjects

- **61M+ NuGet downloads**, 1.1K GitHub stars — de facto standard for .NET deep comparison
- **13 years of edge-case fixes**: circular references, enums, collections, nullables, indexers, DateTime kinds, etc.
- **No transitive dependencies**, ~100KB, targets netstandard2.0
- **Ms-PL license** (permissive) — compatible with MIT
- **Detailed property-path diffs** out of the box (e.g. `Person.Address.City`) which is exactly what issue #3 needs
- Active maintenance: last release July 2025, 394 commits, 275+ unit tests

### Why not duplicate

- Reflection-based deep comparison is non-trivial to get right
- We'd rediscover the same edge cases they've already fixed
- Maintenance burden shifts to us for zero differentiated value

### Risks accepted

- **Single maintainer** (GregFinzer) — bus factor of ~1
- Mitigation: coupling is localized to `RunResult`; swappable if abandoned
- Library is mature/stable enough that "abandoned" ≠ "broken"

## Alternatives Considered

| Option | Reason to reject |
|---|---|
| JSON serialize + string compare | Loses property-path diff output; fragile with non-serializable types |
| FluentAssertions `BeEquivalentTo` | Testing library, heavy dependency for a library-to-library use case |
| DeepEqual | Boolean-only result, no diff report |
| Custom `IEqualityComparer<T>` | Per-type, doesn't generalize; no diff report |
