# Architecture for OptionTools

This repository is a C# library and console host for option pricing, Greeks, and portfolio projection. `OptionLib` implements European Black-Scholes pricing and position math. `option_console` projects positions across dates and underlying prices. Tests live in `optionlib_test`.

Target framework is .NET 10.

## Repository Layout

| Folder | Purpose |
|--------|---------|
| `OptionLib/` | Core library — pricing, Greeks, instruments, accounts, portfolios |
| `option_console/` | Console host for chain and position projections |
| `optionlib_test/` | MSTest suite for `OptionLib` |
| `.grok/rules/` | Agent rules, coding standards, and this architecture file |

There is no root-level solution. Build the project or solution for the area you are changing:

- `OptionLib/OptionLib.csproj` and `OptionLib/OptionLib.sln`
- `optionlib_test/optionlib_test.csproj`
- `option_console/option_console.sln` (needs sibling `DynamicTable`)

`option_console` project-references **DynamicTable** as a sibling under `source_code`. `OptionLib` and `optionlib_test` do not require it.

## Design Principles

- Price, point, and value are distinct: **Price** is the instrument quote, **Point** is Price × ContractLot, **Value** is Point × CurrencyPerPoint. Convert only through `IUnderlying.ConvertVar`.
- Quantity is always in contract lots.
- Instruments project to a date and underlying price via `IInstrument.ProjectTo`. Reuse an `OptionProjection` instance when calling in a loop.
- Option identity is an OCC-style key from `OptionKeyCCC` (`SYMBOL` + `yyMMdd` + `C`/`P` + strike×100).
- Expiry and trading-period times are normalized to 16:00. No projection after expiry.
- Keep pricing math in `OptionCalculator`. Keep position/cash ledger in `UnderlyingAccount`. Keep bar-loop and strategy hooks on `UnderlyingPortfolio`.
- Keep libraries free of console/test code. Sample usage belongs in `option_console`, validation in `optionlib_test`.
- Do not add new NuGet packages unless authorized. `OptionLib` currently uses `Microsoft.Extensions.ObjectPool` and the BCL.

## Component Architecture

- **`IUnderlying` / `Underlying`** — exchange, symbol (max 6 chars), currency, `CurrencyPerPoint`, `ContractLot`, and `ConvertVar`.
- **`IInstrument` / `IOption` / `Option`** — a European call or put. `ProjectTo` fills an `OptionProjection` from `OptionCalculator.Calculate`. `DeriveVolatility` solves implied vol.
- **`OptionCalculator`** — static Black-Scholes European price and Greeks (`OptionGreeks`: Price, Delta, Gamma, Vega, Theta, Rho). Also time-to-expiry in years and implied volatility.
- **`OptionProjection`** — snapshot of price, Greeks, point, value, and the source `IInstrument` at a datetime and underlying price.
- **`IOptionParm` / `OptionParm`** — strike granularity and expiration weekdays for walking a chain.
- **`ITradingPeriod` / `TradingPeriod`** — next/previous trading day at 16:00.
- **`OptionUtil`** — string formatting, option-chain lists, and date/price projection grids over an `UnderlyingPortfolio`.
- **`UnderlyingAccount`** — ledger of equity and option positions plus cash. Enforces chronological date order. Handles open/adjust/close, exercise, and assignment.
- **`UnderlyingPortfolio`** — `IInstrument` over an account. `UpdateBar` expires options, then fires `UpdateBarBegin` / `UpdateOptionBar` / `UpdateBarEnd`. `ProjectTo` aggregates option and equity projections.

Typical flow: construct `Underlying` → `Option` or `UnderlyingPortfolio` → `ProjectTo` / `UpdateBar` → consume `OptionProjection`.

## Architectural Constraints

- Do not replace Black-Scholes in `OptionCalculator` with another model without an explicit design change and an `architecture.md` update.
- Do not compute Price/Point/Value conversions outside `IUnderlying.ConvertVar`.
- Do not project an option after expiry; `Option.ProjectTo` must remain a no-op in that case.
- Do not move console or test code into `OptionLib`.
- Do not add a DynamicTable dependency to `OptionLib` or `optionlib_test`. Only `option_console` may reference it.
- Do not introduce new NuGet packages without authorization.
- There is no root solution; do not assume `dotnet build` at the repo root builds everything.
