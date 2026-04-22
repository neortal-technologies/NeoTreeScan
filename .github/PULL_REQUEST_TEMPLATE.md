## Summary

<!-- One or two sentences describing what this PR changes and why. -->

## Related issue

<!-- "Closes #123" or "Refs #123", or "N/A" if there is no tracking issue. -->

## Change type

- [ ] Bug fix
- [ ] New feature
- [ ] Refactor (no functional change)
- [ ] Performance improvement
- [ ] Documentation
- [ ] Build / CI / tooling
- [ ] Other:

## Test plan

<!-- How did you verify this works? Include the scan target, file count, and
     anything notable you observed. Screenshots welcome for UI changes. -->

- [ ] `dotnet build NeoTreescan.sln -c Release` succeeds with no new warnings
- [ ] Ran a real scan (describe target)
- [ ] Exported to Excel and verified output
- [ ] Tested on Windows 10
- [ ] Tested on Windows 11

## Checklist

- [ ] Commits are signed off (`git commit -s`)
- [ ] Commit messages follow the project style (see CONTRIBUTING.md)
- [ ] Code stays within the architectural boundary (Core has no UI deps, UI
      has no scanning logic)
- [ ] No new NuGet dependencies, or the PR description explains why one is
      justified
- [ ] CHANGELOG.md updated under `[Unreleased]` if user-visible
- [ ] Documentation updated if behavior changed

## Screenshots / recordings

<!-- For UI changes, before / after screenshots or a short screen capture. -->
