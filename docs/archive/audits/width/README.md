# Width audit evidence

The two width-measurement TSV files now use the English sample-name alias
`TwoCarriagewaySixLane`. This is a documentation label, not a renamed game asset. To recover the
exact original sample names, replace that token with the Unicode string represented by these escapes:

```text
\u4e24\u5757\u677f\u516d\u8f66\u9053
```

No measurements, numeric tokens, bit patterns, sample suffixes or dispositions were changed.
Hashes in the original audit reports describe the original generated reports, not the translated
TSV bytes. Reverse the alias substitution before comparing original sample names or file hashes.
