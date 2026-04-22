# Contributing to NeoTreescan

Thanks for taking the time to contribute. NeoTreescan is an Apache 2.0
open-source project maintained by Neortal Technologies Inc. and community
contributors.

## Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md).
By participating you agree to uphold its terms. Report unacceptable behavior
to [contact@neortal.com](mailto:contact@neortal.com).

## How to contribute

### Reporting bugs

1. Search [existing issues](../../issues?q=is%3Aissue) first to avoid
   duplicates.
2. Open a new issue using the **Bug report** template.
3. Include: Windows version, NeoTreescan version, steps to reproduce, what
   you expected, and what happened. Screenshots help.

### Suggesting features

Open an issue using the **Feature request** template. Describe the use case
before the proposed implementation. Feature requests that ship as PRs move
faster than ones that do not.

### Submitting a pull request

1. Fork the repository and create a branch off `main` with a descriptive
   name (`fix/treemap-flicker`, `feat/csv-export`).
2. Make your changes. Keep PRs focused - one logical change per PR.
3. Follow the code guidelines below.
4. Make sure `dotnet build NeoTreescan.sln -c Release` succeeds with no new
   warnings.
5. Smoke-test your change against a real scan (a folder with at least a few
   thousand files).
6. Commit with a clear message. Sign off your commits (see
   [Developer Certificate of Origin](#developer-certificate-of-origin) below).
7. Push your branch and open a PR against `main`. Fill in the PR template.

## Developer Certificate of Origin

By contributing to this project you certify that you have the right to submit
the contribution under the project's Apache 2.0 license, per the
[Developer Certificate of Origin](https://developercertificate.org/).

Sign off every commit with `git commit -s` (or `-S -s` if you also GPG-sign).
This appends a `Signed-off-by:` line to the commit message. PRs without
sign-off will be asked to amend.

## Code guidelines

### Separation of concerns

- **`NeoTreescan.Core`** has no UI dependencies. All scanning, aggregation,
  and export logic lives here. It must build and run in any .NET 9 host, not
  just WPF.
- **`NeoTreescan.App`** is the WPF shell. All display formatting (byte to
  MB/GB/TB, color pickers, percent converters) lives here.

Code that crosses that boundary (e.g. scanning logic that reaches into WPF
types) will be asked to move.

### MVVM

The app uses `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`,
`[RelayCommand]`). Stick to that pattern; do not hand-roll `INotifyPropertyChanged`
plumbing. Bindings go through ViewModels, not directly to models.

### Style

- C# language version: latest. Nullable reference types are enabled.
- Four-space indentation, no tabs.
- Follow the existing file and folder naming. PascalCase for types and public
  members; `_camelCase` for private fields.
- Prefer short comments that explain *why*, not *what*. If a comment
  describes what the code does, the code should be clearer.
- Do not introduce new NuGet dependencies without discussing first. Every
  dependency affects the published EXE size.

### No em dashes

Use regular hyphens (`-`) or rephrase. This is a project convention applied
across the codebase and documentation.

### Performance

NeoTreescan routinely scans millions of files. Keep hot paths allocation-free
where possible:

- Prefer `stackalloc` or pooled buffers for short-lived arrays.
- Use `Span<T>` / `ReadOnlySpan<char>` for path manipulation.
- Avoid `string.Split` and LINQ in the scanner inner loop.

### Error handling

- Scanner errors (access denied, path too long, I/O failure) should be
  collected into `ScanResult.Errors`, not thrown. A scan should never abort
  because of one bad folder.
- UI code should surface user-facing errors as a status-bar message or a
  dialog, not an unhandled exception.

### Tests

The project does not yet have an automated test suite. Contributions that add
one are welcome. Until then, PRs are expected to include a manual test plan
in the description ("scanned `C:\`, exported to Excel, confirmed totals match
the treemap").

## Commit message style

Follow this format:

```
area: short summary in imperative mood

Optional longer explanation of why the change was made. Wrap at 72
columns. Reference issues with #123.

Signed-off-by: Your Name <you@example.com>
```

`area` is one of: `scanner`, `treemap`, `export`, `ui`, `ci`, `docs`,
`build`, `deps`. Pick the closest match; use `misc` only if nothing fits.

## Review expectations

- A maintainer will review within a few business days.
- Expect feedback. Do not take review comments personally; the goal is a
  good codebase.
- Squash-merge is the default. Keep your branch history reasonable; final
  commit messages are rewritten at merge time.

## Release process

Releases are tagged as `vMAJOR.MINOR.PATCH` on `main`. A GitHub Actions
workflow builds the single-file EXE and attaches it to the release. See
[.github/workflows/release.yml](.github/workflows/release.yml).

## Questions?

Open a [discussion](../../discussions) or email
[contact@neortal.com](mailto:contact@neortal.com).
