# cai: the Code Assurance Index reference tools

`cai` recomputes a CAI from its evidence and checks a signed CAI delivery, with no service, no account and no network.
It folds evidence with the same [`Cai.Scoring`](../Cai.Scoring) library the API and the in-browser calculator use, so the
number it prints is the number they print.

## Get it

Download the archive for your platform from the
[releases](https://github.com/code-assurance-initiative/CodeAssuranceIndex/releases) (Linux, macOS and Windows, x64 and
Arm), unpack it and run `cai`. No .NET install is needed. Check the download against `SHA256SUMS` in the same release.

Each archive holds:

- `cai` (`cai.exe` on Windows), the program;
- `rubrics/`, the published rubric archive at the time of the release, which `cai verify` and `cai verify-delivery` read
  to reproduce a number. For a rubric version published later, point `--rubrics` at a newer archive;
- `LICENSE` (Apache-2.0) and this README.

`cai --version` prints which release you have.

## Use it

```bash
# Fold an evidence bundle to its CAI headline and per-lens contributions
cai score evidence.json --rubrics rubrics

# Reproduce a published headline; exit 1 if it does not match
cai verify evidence.json --rubrics rubrics --expect 70.3

# Check a signed delivery: the signature, and that its evidence folds to the number it states; exit 1 on failure
cai verify-delivery package.json --keys keys.json --rubrics rubrics

# Check the signature alone (no rubric archive needed; makes no claim about the number)
cai verify-delivery package.json --keys keys.json --no-reproduce
```

`keygen` and `sign` mint and sign deliveries; run `cai` with no arguments for every option. Exit codes: 0 ok, 1 a
verification failed, 2 a usage or file error.

The input is a CAI **evidence bundle** (see [`examples/evidence.sample.json`](../../examples/evidence.sample.json)), the
portable, documented record a score is computed from. The same evidence under the same rubric version always folds to
the same number. The delivery format is specified in [docs/spec/cai-delivery-package.md](../../docs/spec/cai-delivery-package.md).

## Why it exists

It is the executable proof that the standard is open: anyone can recompute, or falsify, a published CAI from its evidence,
and check a signed delivery without trusting whoever produced it. See
[docs/adr/0002](../../docs/adr/0002-deterministic-reproducible-scoring.md).
