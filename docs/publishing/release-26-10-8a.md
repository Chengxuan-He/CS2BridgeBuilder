# Bridge Builder 26.10.8a

- Fixed self-check cleanup stopping when a backup or move fails; remaining faulty bridge assets are now deleted where filesystem permissions allow.
- Attempt dependency migration before removing invalid legacy bridges.
- Continue cleanup after individual file failures and report remaining inaccessible paths.
- Removed redundant migration and cleanup code.
