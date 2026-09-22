# Test strategy

OutlookNextEvent tests must prove the calendar shaping and widget rendering paths from `docs\architecture.md` sections 5-6 without requiring a Microsoft account, live Graph traffic, MSAL broker state, or the Windows Widgets Board.

## Layers

1. **Core unit tests** cover pure logic in `OutlookNextEvent.Core`: `EventShaper`, shaped event models, and `CardBuilder`. They use in-memory `NextEvent` fixtures and fixed clocks/time zones.
2. **App/Infrastructure seam tests** cover orchestration boundaries with fakes: auth state, Graph calendar responses, widget content states, and token-provider delegation. They must not call live Graph or MSAL.
3. **Manual MSIX/widget validation** remains for packaged install, widget registration, Widgets Board lifecycle, broker/WAM behavior, and companion-window sign-in because those depend on Windows shell and account state.

## Determinism rules

- Use `TimeProvider` injection with `FixedTimeProvider`; do not call `DateTime.Now`, `DateTime.UtcNow`, or `DateTimeOffset.UtcNow` in test assertions.
- Use fixed `DateTimeOffset` values from `TestClock.FixedUtcNow` and explicit time zones. Prefer `TimeZoneInfo.Utc`; when local display behavior needs Windows local semantics, use the Windows ID `Eastern Standard Time`.
- Test data should come from `NextEventFixtures` or `NextEventBuilder` so #13 and #14 do not duplicate calendar setup.
- Do not assert on machine-local culture unless the production code explicitly uses it. Card and shaping assertions should use stable dates/times.

## Reusable doubles and fixtures

- `FakeAuthService` implements `IAuthService` and can return a canned token, throw for silent sign-in-required/token-expired paths, throw for interactive sign-in, and record calls.
- `FakeGraphCalendarClient` implements `IGraphCalendarClient`/`ICalendarService`, returns deterministic event fixtures, records request windows/max counts, or throws to simulate Graph failures.
- `FixedTimeProvider` supplies a mutable deterministic UTC clock for code that accepts `TimeProvider`.
- `NextEventBuilder` and `NextEventFixtures` provide empty, single, many, all-day, cancelled, and cross-timezone event sets.

## Naming conventions

- Use `ClassUnderTest_MethodOrScenario_ExpectedBehavior` for test names.
- Keep smoke tests for fakes small; exhaustive EventShaper assertions belong to #13 and exhaustive card rendering assertions belong to #14.
- Name scenario fixtures by calendar condition, for example `AllDay`, `Cancelled`, `CrossTimeZone`, and `Many`.

## Commands

Run the full automated suite before opening or merging test-related PRs:

```powershell
dotnet restore .\OutlookNextEvent.sln
dotnet build .\OutlookNextEvent.sln --no-restore
dotnet test .\OutlookNextEvent.sln --no-build
```

Targeted development loops may run a single test project, but the final check for a branch should use `dotnet test .\OutlookNextEvent.sln`.
