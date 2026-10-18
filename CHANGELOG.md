# Changelog

## [2.1.0]

### Added

- CASSIE announcement when someone picks up the SCP-035 mask. It can be turned off or changed in the config
  (`enable_pickup_announcement`, `pickup_announcement`, `pickup_announcement_subtitles`).
- CedMod support: kills between SCP-035 and other players no longer count as teamkills, so CedMod's friendly fire
  autoban no longer bans players for them.

### Fixed

- The UCI integration works with UncomplicatedCustomItems 4.2.0, where `ICustomItem` was replaced by the
  `CustomItem` class. Older UCI versions keep working.

## [2.0.0]

### Changed

- Rewrote the whole plugin.
- Fixed a bunch of bugs.
- Added UCI integration.
- Updated to UCR 9.6.0.
