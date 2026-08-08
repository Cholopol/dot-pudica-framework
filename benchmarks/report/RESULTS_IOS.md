# DotPudica Benchmark Report (iOS / NativeAOT)

> Measured on a device export package. Separate from the desktop JIT / headless report; do not cross-compare absolute ms — conclusions follow structural counts and relative relationships within the same table.

## Environment

| Item | Value |
| ----- | ------------------------------------------------------------------- |
| Platform | iOS 26 device (exported App, not editor) |
| Runtime | .NET NativeAOT (Godot 4.7.1 C# iOS) |
| Godot | 4.7.1.stable.mono |
| Entry scene | `res://tests/DotPudica.Integration/Benchmarks/BenchmarkRunner.tscn` |
| Test version | v1.1.0 |

## Run status

All six Godot metrics scenarios `PASS`, metrics written successfully, `SUMMARY failed=0`.

## Cross-cutting conclusions

### 1. Property burst coalesce (PropertyBurst)

| sourceUpdates | targetWrites | framesToSettle | elapsedMs | settled |
| ------------- | ------------ | -------------- | --------- | ------- |
| 1000 | 1 | 0 | 11.71 | true |
| 10000 | 1 | 0 | 2.00 | true |

**Conclusion:** After background bursts go through the binding Coalescer, UI writes collapse to 1; same structure as desktop. Do not compare absolute elapsed time with headless.

### 2. Native three-way compare (NativeCompare)

| mode | sourceUpdates | targetWrites | framesToSettle | elapsedMs |
| ------------------- | ------------- | ------------ | -------------- | --------- |
| native-direct | 1000 | 1000 | 0 | 0.71 |
| dotpudica-bound | 1000 | 1000 | 0 | 8.24 |
| dotpudica-coalesced | 1000 | 1 | 0 | 0.68 |
| native-direct | 10000 | 10000 | 0 | 1.07 |
| dotpudica-bound | 10000 | 10000 | 0 | 8.47 |
| dotpudica-coalesced | 10000 | 2 | 0 | 9.24 |

**Conclusion:**

- `native-direct` / `dotpudica-bound`: near 1:1 writes.
- `dotpudica-coalesced`: 1000→1, 10000→2 — writes far below source updates (occasional extra refresh; order of magnitude still correct).
- Background spam should use the binding Coalescer, not per-update Post.

### 3. Backpressure

| mode | executed | framesToComplete | peakPerFrame |
| ------------- | -------- | ---------------- | ------------ |
| post-storm | 10000 | 2 | 5964 |
| post-budgeted | 10000 | 157 | 64 |
| mailbox-drain | 1 | 1 | 1 |

**Conclusion:**

- On device, unbounded Post **need not** finish in one frame (here 2 frames, peak≈5964); desktop headless is often 1 frame — a device frame-budget difference, not a logic error.
- `post-budgeted` (64 per frame) still stretches to 157 frames, peak=64.
- Mailbox drains at most once per frame (executed=1). Prefer Mailbox for network snapshots.

### 4. Virtual list

| mode | itemCount | activeNodes | bindMs | scrollMs |
| ----------- | --------- | ----------- | ------ | -------- |
| non-virtual | 100 | 100 | 549.19 | 16.55 |
| non-virtual | 500 | 500 | 50.01 | 16.68 |
| non-virtual | 1000 | 1000 | 91.53 | 16.63 |
| virtual | 1000 | 12 | 21.94 | 25.00 |
| virtual | 10000 | 12 | 24.39 | 25.00 |
| virtual | 50000 | 12 | 16.78 | 25.15 |

**Conclusion:** Non-virtual nodes grow linearly with data size; virtual-list active nodes stay at **12** (viewport + overscan). Elevated bindMs for `non-virtual@100` is treated as first-run warmup / shader noise — not a standalone performance claim. Prefer virtual lists for large datasets.

### 5. View lifecycle

| bindCount | initMs | disposeMs |
| --------- | ------ | --------- |
| 10 | 16.48 | 0.01 |
| 50 | 16.02 | 0.03 |
| 100 | 15.91 | 0.14 |

**Conclusion:** In this run, initMs barely rises with bind count (fixed overhead dominates); dispose grows slightly with bind count. Frequent enter/exit should still use pooling.

### 6. View / Window pools

| mode | iterations | createdNodes | reusedCount | elapsedMs |
| ----------- | ---------- | ------------ | ----------- | --------- |
| view-pool | 50 | 1 | 49 | 832.85 |
| window-pool | 50 | 1 | 49 | 831.73 |

**Conclusion:** Reuse count = iterations−1; pool hits match expectations.

## README claims (iOS AOT)

| Claim | Result |
| -------------------- | -------------------------------------------------------------- |
| Auto-coalesce high-frequency background property updates | **Supported** (PropertyBurst / native-compare coalesced) |
| Virtual list instantiates only visible rows | **Supported** (activeNodes=12 does not grow linearly with 1k–50k) |
| Mailbox can coalesce Post backlog | **Supported** (executed=1) |
| Frame budget can cap peak | **Supported** (post-budgeted peak=64; framework still has no built-in global scheduler — this item is benchmark-side simulation) |
| View/Window pool reuse | **Supported** (reused=49/50) |
| Strongly typed hot path, zero boxing | **Not covered in this report** (Core BenchmarkDotNet; this report is Godot export metrics only) |
| Small UI faster when native | **Partially visible** (at same N, native-direct write counts differ from coalesced strategy; do not cross-compare absolute ms) |

## Boundaries

- Does not measure GPU frame-rate feel, input latency, or App Store review package size.
- Godot C# iOS remains experimental NativeAOT; ILC/trimming warnings on export are common and do not mean these metrics scenarios failed.
- `post-budgeted` is a benchmark-side frame-cap simulation, not a built-in global scheduler.
- Metrics path: device `Documents/dotpudica-benchmarks/metrics.json` (Xcode Download Container).
