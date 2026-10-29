# Dynamic Island for Windows

WPF / .NET 9 MVP: music, calendar and weather. All interface text is English. No third-party UI libraries.

## Run

Open `dynamic_island_prototype.sln` in Visual Studio 2022 with the .NET 9 SDK and Windows desktop development workload, or run:

```
dotnet run --project dynamic_island_prototype
```

The island starts completely hidden. Move the pointer to the **very top edge, at the center of the primary display** to reveal the expanded island. The panel attaches directly to the top edge with rounded bottom corners. The invisible activation strip is 288 DIPs wide and 8 DIPs high, scaled for the monitor's DPI. It does not intercept clicks. Move away and the island fades and slides out after a 450 ms grace period. Context menus and the Settings dialog keep it open. Right-click the visible island and choose **Exit Dynamic Island** to quit.

Use the **small icon tabs at the upper left** to select Music / Calendar / Weather. A short white underline marks the selected tab; the gear at the upper right opens Settings. Selection is managed by WidgetManager, and the selected widget stays active when the island hides. Content fades and slides during switching. The compact black panel uses a large album cover with track details and inline playback controls beside it.

## Calendar and weather setup

Right-click the island and choose **Settings…**. This separate dialog accepts keyboard input; the island itself remains non-activating.

1. In Google Calendar on the web, go to **Settings → Settings for my calendars → your calendar → Integrate calendar → Secret address in iCal format**. Paste the HTTPS link into the calendar field. Leave it empty to disconnect. Workspace administrators can disable this address.
2. Enter a weather city, press **Search**, and choose the correct city from the results. Press **Save**. No weather API key is required. The app uses the selected coordinates, without requesting device location or guessing from your IP.

Google's read-only iCal feed avoids OAuth credentials and Windows calendar-store packaging/capability requirements for this desktop MVP. It cannot edit events and may reflect Google's feed propagation delay. Ical.Net 5.2.3 handles recurrence rules, exceptions, time zones and all-day dates; it is a data-format library, not a UI library.

Calendar refreshes every minute and lists up to 30 ongoing/upcoming events before the end of today in the computer's local time zone. Finished and cancelled events are excluded. Scroll the expanded view for additional events. Weather refreshes every 15 minutes and shows current temperature in Celsius with a WMO condition icon. Clear skies have separate day/night icons. Both refresh immediately after settings are saved. Connection failures display **Unable to update** and retry automatically.

Settings are stored in `%LOCALAPPDATA%\DynamicIsland\settings.json`, outside the repository. The calendar URL is a private access link: it is masked in the dialog, stored locally as plain text, and never logged by these widgets. Calendar event data is held only in memory. City search uses Open-Meteo Geocoding (GeoNames); weather uses Open-Meteo Forecast. The free Open-Meteo endpoint is for non-commercial use.

Sources: [Google Calendar iCal setup](https://support.google.com/calendar/answer/37648?hl=en), [Open-Meteo weather API](https://open-meteo.com/en/docs), [geocoding API](https://open-meteo.com/en/docs/geocoding-api), [Ical.Net](https://github.com/ical-org/ical.net).

Music metadata and previous / play-pause / next commands come from Windows Global System Media Transport Controls. Start playback in an app that publishes a Windows media session. Unsupported commands are disabled. With no session, the island displays **Nothing playing**. Artwork has a music-note fallback.

## Structure

- `Widgets/IWidget.cs`: initialization, view model, compact and expanded view factories, disposal. No dependency on MainWindow.
- `Widgets/WidgetManager.cs`: ordered collection, active widget and reordering.
- `Widgets/MusicWidget.cs`: event-driven Windows media session integration; subscribes only to the current session and releases subscriptions on shutdown.
- `Widgets/MusicViewModel.cs`: bindable state and asynchronous commands.
- `Widgets/CalendarWidget.cs`, `WeatherWidget.cs`: independent IWidget implementations using a small shared refresh lifecycle. Cancellation prevents a previous settings request from overwriting newer data.
- `Services/CalendarService.cs`, `WeatherService.cs`: calendar parsing and weather/geocoding HTTP calls, separate from WPF views.
- `Settings/`: persistent configuration and the settings dialog.
- `Platform/IslandWindowBehavior.cs`: native window styles, monitor positioning, DPI and fullscreen detection.
- `MainWindow`: widget host and hover Storyboard. A fixed outer window keeps the top center stable during animation; only the pill changes size.

The overlay uses WS_EX_NOACTIVATE, WS_EX_TOOLWINDOW and WM_MOUSEACTIVATE/MA_NOACTIVATE. Playback clicks are delivered without activating the main window. An 80 ms background timer checks pointer position and foreground window bounds against the primary monitor, without global mouse hooks. Fullscreen apps on the primary display suppress reveal; apps on other monitors do not. PerMonitorV2 and physical-pixel Win32 positioning support mixed DPI, negative monitor coordinates and display changes.

## Manual verification

- Start / pause music; change tracks and switch players; close the current player.
- Check missing artwork, long titles, unavailable commands and the no-session state.
- Hover repeatedly during transitions; click playback controls while typing in another application and confirm that focus stays there.
- Confirm startup is invisible; trigger from the top center, move down to the controls, then away. Confirm the hidden island does not block clicks and that unrelated top-edge positions do not reveal it.
- Keep Settings or the context menu open while moving away; the island should stay open. After closing them, it should hide normally.
- Launch a fullscreen app on the primary display, then switch back. Repeat on a secondary display.
- Change the primary display, resolution and scaling (100%, 150%, 200%); disconnect and reconnect a display.
- Exit from the context menu and confirm the process ends.
- Configure a calendar and city, restart and verify persistence; switch through all three widgets and back to music.
- Verify no-event, disconnected calendar, no-city, invalid feed and offline states; restore connectivity and wait for retry.
- Change settings while a request is loading; verify the previous request does not overwrite the new selection.

Fullscreen detection is based on foreground window bounds, including borderless fullscreen. Exclusive fullscreen games, protected players and specific multi-monitor hardware require manual testing. The system-selected current media session is used; a player chooser is outside this MVP.

CPU/RAM and timer widgets remain planned. Add them individually through IWidget; no plugin loader is needed yet.

## Verification

Build: `dotnet build dynamic_island_prototype.sln -c Release`

Deterministic calendar/condition checks: `dotnet run --project verification/verification.csproj -c Release`

Include live Open-Meteo requests: `dotnet run --project verification/verification.csproj -c Release -- --live`

The verification console covers today's filtering/order, ongoing/all-day events, unbounded recurrence, EXDATE, RECURRENCE-ID overrides, multi-day overlap, local conversion from UTC, exclusive all-day end dates, condition icons and URL validation. UI animation, focus and monitor behavior still need the manual checks above.

## GitHub and distribution

GitHub Actions builds the WPF application and runs deterministic checks on Windows for pushes and pull requests. Live API checks are intentionally manual so CI does not depend on weather service availability. `global.json` selects a stable .NET 9 SDK (9.0.300 or a newer .NET 9 feature band).

Build a portable Windows x64 folder, including the .NET runtime:

```powershell
dotnet publish dynamic_island_prototype/dynamic_island_prototype.csproj -c Release -r win-x64 --self-contained true -o artifacts/win-x64
```

Zip the entire `artifacts/win-x64` folder for distribution, not just the executable. The output is unsigned and is not an installer. Runtime settings remain in the user's local app data folder. Build outputs, IDE state, private settings, calendar exports and signing keys are excluded by `.gitignore`.

To publish the source for the first time, create an empty GitHub repository, then run these commands from this folder (replace `YOUR_NAME`):

```powershell
git init -b main
git add .
git diff --cached --stat
git commit -m "Add Windows Dynamic Island prototype"
git remote add origin https://github.com/YOUR_NAME/dynamic-island-prototype.git
git push -u origin main
```

No project license has been selected yet. Choose one before inviting reuse or redistribution. Dependencies retain their own licenses; Ical.Net and NodaTime use their upstream licenses. See [Ical.Net](https://github.com/ical-org/ical.net) and [NodaTime](https://github.com/nodatime/nodatime) for their license texts.
