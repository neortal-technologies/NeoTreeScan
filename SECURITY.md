# Security Policy

## Supported versions

Only the latest released version of NeoTreescan receives security fixes.
Older versions will not be patched. Upgrade to the latest release from the
[Releases page](../../releases).

| Version  | Supported          |
|----------|--------------------|
| Latest   | Yes                |
| Older    | No                 |

## Reporting a vulnerability

**Please do not open a public GitHub issue for security problems.**

Report vulnerabilities privately by either:

1. **GitHub Security Advisories** - go to the repository's **Security** tab
   and click **Report a vulnerability**. This is the preferred channel.
2. **Email** - send details to
   [contact@neortal.com](mailto:contact@neortal.com). PGP encryption is not
   required; if you prefer encrypted mail, request a public key in your
   initial unencrypted message and we will send one.

Include as much of the following as possible:

- A description of the issue and its impact.
- Steps to reproduce (ideally a minimal proof-of-concept).
- Affected version(s) of NeoTreescan and Windows.
- Whether elevated privileges are required.
- Any suggested remediation.

## What to expect

- Acknowledgment within **3 business days** of your report.
- A triage decision (accepted / rejected / needs-info) within **10 business
  days**.
- A fix target date once triage is done. Critical issues aim for a release
  within 30 days of confirmation; lower-severity issues may be bundled into
  the next scheduled release.
- Credit in the release notes and in this file, if you want it.

## Scope

In scope:

- NeoTreescan.exe and its source code in this repository.
- Elevation-of-privilege paths (what happens when the app runs as
  Administrator).
- File-parsing issues (crafted filesystem contents causing crashes, hangs,
  or out-of-bounds reads).
- Any unexpected network activity (NeoTreescan should make **zero** network
  calls).

Out of scope:

- Issues in upstream dependencies (.NET, WPF, ClosedXML,
  CommunityToolkit.Mvvm). Report those to the respective upstream project.
- Attacks requiring physical access to a signed-in machine.
- Social-engineering scenarios where the user is tricked into running
  malware named "NeoTreescan.exe" that is not our release artifact. Always
  verify release artifacts against the checksums published on the release
  page.

## Coordinated disclosure

We follow a **90-day coordinated disclosure** policy. We will work with you
on a disclosure timeline. The default is:

- Day 0: Report received.
- Day 1-10: Triage.
- Day 10-90: Fix development, release preparation.
- Day 90 (or at fixed release, whichever is sooner): public disclosure with
  credit.

If a fix is ready sooner, we release sooner. If the issue is actively being
exploited, we may shorten this timeline.

## Hall of fame

Researchers who have responsibly disclosed security issues in NeoTreescan
will be listed here, with their consent.

*(No reports yet.)*
