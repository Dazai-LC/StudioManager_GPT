# Bookings UI hotfix report

Branch: `fix/booking-detail-grid-ui`

## Scope completed

- Replaced the booking detail dialog's competing docked controls with a two-row `TableLayoutPanel`: a fixed summary card above a filling `TabControl`. The summary and tabs now occupy separate rows, so the tabs cannot cover the booking information.
- The main booking grid's **Kết thúc** column now uses `dd/MM/yyyy HH:mm`, matching the displayed start date and preserving the actual date for multi-day bookings.
- Renamed displayed booking-grid headers to Vietnamese: **Mã lịch**, **Khách hàng**, **Bắt đầu**, **Kết thúc**, **Nhiếp ảnh gia**, **Phòng**, **Trạng thái**, **Tổng tiền**, and **Còn lại**. Date and financial columns have a minimum readable width; horizontal scrolling remains available when the window is narrow.
- Updated the shared button treatment: calmer semantic colors, consistent 10px corner radius, balanced padding, and distinct hover/pressed colors. Existing handlers and role visibility are unchanged.
- Long summary text in the detail form uses ellipsis only when necessary and exposes the complete value through a tooltip.

## UI quality checklist

- [x] Detail summary and tabs have separate layout rows; no sibling control can overlap the summary.
- [x] Start/end text in the grid includes date and time.
- [x] Grid headings use Vietnamese display labels instead of source/property names.
- [x] Existing buttons retain their handlers; this change only updates shared visual treatment.
- [x] `git diff --check` passes.
- [ ] Windows build and visual smoke test at 100%, 125%, and 150% DPI — pending local Windows environment.

## IMAGE / ASSET PLACEHOLDERS

No image or asset placeholder was introduced or changed by this hotfix.

## Verification boundary

The current execution environment has no .NET SDK or Windows Forms renderer. No build, GUI run, SQL operation, or screenshot assertion was claimed as passed here. The following local check is required before merging:

```powershell
dotnet build StudioManager.sln
dotnet run --project .\src\StudioManager.WinForms\StudioManager.WinForms.csproj
```

Then open **Lịch chụp**, select a booking, choose **Chi tiết**, resize within the supported range, and verify the full end date, summary, tabs, and toolbar buttons.
