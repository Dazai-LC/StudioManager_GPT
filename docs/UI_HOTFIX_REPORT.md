# UI hotfix report — Login & Dashboard

Branch: `codex/modern-login-dashboard`

## Scope

- Fix Login overlap and vertical clipping at the supported minimum window size.
- Fix Dashboard schedule/activity overflow with real long data.
- Replace decorative `Xem tất cả` labels with keyboard-accessible links connected to the existing Bookings page.
- Preserve authentication, dashboard data loading, sidebar collapse, navigation and skeleton loading behavior.

## UI quality checklist

- [x] Login uses proportional columns and explicit layout rows; controls no longer share pixel positions.
- [x] Login content fits the supported minimum window size (`1060 × 680`) without overlapping.
- [x] Input, option, action, error and footer areas have independent layout rows.
- [x] Dashboard schedule and activity rows follow the actual container width.
- [x] Long service/customer/activity values show ellipsis only where space is finite and expose the complete value by tooltip.
- [x] Table cells expose their complete formatted value by tooltip.
- [x] `Xem tất cả` is a real `LinkLabel` and navigates to the existing `Lịch chụp` page.
- [x] Static year text is visually neutral and no longer looks like a clickable link.
- [x] Existing authentication, SQL data loading, skeleton, menu and sidebar behavior remain connected.
- [x] `git diff --check` passes.
- [ ] Windows visual smoke test at 100%, 125% and 150% DPI — requires Visual Studio/.NET 8 on Windows.
- [ ] `dotnet build StudioManager.sln` — requires .NET SDK, which is unavailable in the Codex runtime.

## IMAGE / ASSET PLACEHOLDERS

1. Login
   - Location: Left promotional panel
   - Type: Studio background image
   - Status: Placeholder (painted navy gradient; layout container is complete)
   - Expected asset: Studio photography / workspace image with a dark overlay-safe composition

2. Login
   - Location: Brand mark above `StudioManager`
   - Type: Product logo
   - Status: Placeholder (Unicode brand glyph)
   - Expected asset: Transparent StudioManager logo/icon, preferably PNG or SVG-derived bitmap

3. Login
   - Location: Brand mark in the left promotional header
   - Type: Product logo
   - Status: Placeholder (Unicode brand glyph)
   - Expected asset: Transparent StudioManager logo/icon

4. Main navigation
   - Location: Sidebar brand mark
   - Type: Product logo
   - Status: Placeholder (Unicode brand glyph)
   - Expected asset: Compact transparent StudioManager logo suitable for expanded and collapsed sidebar states

5. Main header
   - Location: Signed-in user avatar
   - Type: User profile image
   - Status: Placeholder (generated initials)
   - Expected asset: Optional user avatar supplied by the account/profile module

## Notes

- No random external images were added.
- The Login promotional panel keeps stable dimensions when the final background image is supplied.
- A studio availability thumbnail/card from the reference artwork was not added because the current repository has no matching business workflow or room-image asset; adding a decorative nonfunctional card would violate the interaction requirement.
