# Contract documents

[PROJECT_CONTRACT.md](PROJECT_CONTRACT.md) is the normative project contract. The other Markdown
documents in this directory preserve supporting audits, measurements and implementation evidence.
The repository and directory-level AGENTS.md files route agents to the applicable requirements.

## English-language revision

On 2026-10-13, remaining Chinese contract prose was translated without changing requirements.
Existing English documents were reviewed and retained. Baseline: branch `dev`, HEAD
`5bea1d0bef04dd2ac718809852d3f657c7388e58`; local rollback reference:
`refs/rollback/english-contracts-guide-20261013`. Earlier uncommitted changes were preserved.

The two width-measurement TSV files now use the English sample-name alias
`TwoCarriagewaySixLane`. This is a documentation label, not a renamed game asset. To recover the
exact original sample names, replace that token with the Unicode string represented by these escapes:

```text
\u4e24\u5757\u677f\u516d\u8f66\u9053
```

No measurements, numeric tokens, bit patterns, sample suffixes or dispositions were changed.
Hashes in the original audit reports describe the original generated reports, not the translated
TSV bytes. Reverse the alias substitution before comparing original sample names or file hashes.

For a contributor workflow, see [Adding a bridge style with a coding agent](../Adding-Bridge-Styles-With-Coding-Agents.md).
