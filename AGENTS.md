# AGENTS.md

Development happens only through Victor and Cursor agents.

Governing standard: `.cursor/rules/coding-standards.mdc` (always applied). Repo-specific design constraints: `.cursor/rules/architecture.mdc`. Existing code does not fully comply (single-line `if` bodies without braces, mutable classes, nullable warnings, an empty test). Bring code into compliance when you touch it. Do not restyle unrelated code.

## What this repo is

`OptionTools` is a .NET 10 (`net10.0`) library and console sample for European Black-Scholes pricing, Greeks, and portfolio projection. It is source in this repository, not a packed NuGet.

- `OptionLib` builds from its project file.
- `optionlib_test` is an MSTest project. Its only test method is empty.
- `option_console` is a sample host. `Main` calls `run_qqq()`, which reads a machine-local CSV.

Nullable reference types are enabled. There is no root `.editorconfig`, and warnings are not treated as errors.

## Build, test, and run

Verified with the .NET SDK 10.0.404. There is no repository-root solution or project. `dotnet build` at the root fails with MSB1003.

```bash
dotnet build OptionLib/OptionLib.csproj
dotnet build optionlib_test/optionlib_test.csproj
dotnet test optionlib_test/optionlib_test.csproj
```

`dotnet build OptionLib/OptionLib.csproj` succeeds. A clean compile reports nullable warnings (CS8618, CS8625) and zero errors. `dotnet test optionlib_test/optionlib_test.csproj` passes 1 test and fails 0. That test is `TestXXX.TestPrice` and its body is empty, so it does not check prices or Greeks. The run also prints a warning that `Microsoft.NET.Test.Sdk` 17.12.0 is outdated (`VSTEST_ARTIFACTS_DIRECTORY`). Leave that package reference as it is.

`dotnet build OptionLib/OptionLib.sln` fails. The solution lists `..\BinarySerializer\BinarySerializer.csproj` (`BinarySerializer/BinarySerializer.csproj` at the repo root). That file is not in the tree. No C# file references BinarySerializer, and there is no `vdiamond/BinarySerializer` repository. Build the csproj.

`option_console` does not build until [DynamicTable](https://github.com/vdiamond/DynamicTable) is cloned next to this repo. With that clone:

```bash
dotnet build option_console/option_console.csproj
dotnet build option_console/option_console.sln
dotnet run --project option_console/option_console.csproj
```

Both console builds succeed when the sibling is present (nullable warnings, zero errors). `dotnet run` then throws `FileNotFoundException` because `run_qqq` loads `d:\predict\index_extract_ta.csv`. On Linux that path is resolved under the working directory. Do not add that CSV to the repo.

Without the sibling, `dotnet build option_console/option_console.csproj` fails with MSB9008 and CS0246 (`Table`, `TableCsv`, `TableExtension`). `dotnet build option_console/option_console.sln` fails with MSB3202 for the same three project files.

## Layout

| Path | Role |
|------|------|
| `OptionLib/` | Library. `OptionLib.csproj` is the build entry. `OptionLib.sln` also references the missing BinarySerializer project. |
| `option_console/` | Console sample. `option_console.csproj` and `option_console.sln`. The solution also includes `optionlib_test` and the DynamicTable projects. |
| `optionlib_test/` | MSTest project. No solution file of its own. References `OptionLib`. |
| `.cursor/rules/` | Agent rules. |

Library sources: `OptionCalculator` (Black-Scholes and probability helpers), `Option` / `Underlying`, `UnderlyingAccount` (cash and positions), `UnderlyingPortfolio` (bar loop and projection), `OptionUtil`, `OptionParm`, `TradingPeriod`, `OptionKeyCCC`.

## Dependencies on other vdiamond repos

**DynamicTable** — https://github.com/vdiamond/DynamicTable

- How: sibling-folder `ProjectReference` with a relative path. Not a git submodule, not an HTTP API, and not a PackageReference.
- Projects: `option_console` only. `OptionLib` and `optionlib_test` must not reference it.
- Expected layout: clone the repo next to this one.

```
source_code/
├── OptionTools/          # this repo
└── DynamicTable/
    ├── Table/Table/Table.csproj
    ├── TableCsv/TableCsv/TableCsv.csproj
    └── TableExtension/TableExtension/TableExtension.csproj
```

`option_console/option_console.csproj` references:

- `..\..\DynamicTable\Table\Table\Table.csproj`
- `..\..\DynamicTable\TableCsv\TableCsv\TableCsv.csproj`
- `..\..\DynamicTable\TableExtension\TableExtension\TableExtension.csproj`
- `..\OptionLib\OptionLib.csproj`

No other vdiamond repository is referenced. No downstream dependents are recorded in this repo.

**NuGet (do not change):** `OptionLib` references preview package `Microsoft.Extensions.ObjectPool` version `11.0.0-preview.5.26302.115`. `OptionProjectionPool` uses it. `AllowUnsafeBlocks` is true on `OptionLib`; the library source has no `unsafe` blocks. `optionlib_test` references `Microsoft.NET.Test.Sdk` 17.12.0 and `MSTest` 3.6.4.

## Conventions observed

- Target `net10.0`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`. `optionlib_test` sets `<LangVersion>latest</LangVersion>`.
- Namespace `OptionLib` for the library; `option_console` and `optionlib_test` for the other projects.
- Private fields use `_camelCase`. Several types wrap private fields in `#region private`.
- Constructors tend to come first. Member order is not consistent: `UnderlyingPortfolio` groups public helpers and protected virtuals in their own regions.
- Single-statement `if` bodies often omit braces.
- Types are mutable classes and interfaces, not records. Most classes are not `sealed`.
- Price, point, and value stay distinct. Convert units only through `IUnderlying.ConvertVar`. Quantity is in contract lots.
- Option identity is `OptionKeyCCC` (`SYMBOL` + `yyMMdd` + `C`/`P` + strike×100).
- Expiry and trading-period times are normalized to 16:00. `Option.ProjectTo` does not write a projection when `dateTime` is after expiry.
- Pricing math stays in `OptionCalculator`. The position ledger stays in `UnderlyingAccount` (`Adjust`, `Close`, `Exercise`, `Assign`, `Expire`). The bar loop stays on `UnderlyingPortfolio`.
- `option_console` is the sample host. Tests stay in `optionlib_test`.

## Do not touch

- Do not change public OptionLib APIs (types, method signatures, Price/Point/Value conversion, or the Black-Scholes model in `OptionCalculator`) unless Victor asks.
- Do not add NuGet packages. Do not change the ObjectPool preview version or the test package versions.
- Do not add a DynamicTable reference to `OptionLib` or `optionlib_test`.
- Do not edit `OptionLib/OptionLib.sln` to drop or retarget BinarySerializer unless Victor asks. Do not add a BinarySerializer project.
- Do not commit secrets or local config (`.env`, `secrets.json`, `local.settings.json`, `appsettings.*.local.json`). None are in the tree. Do not commit `bin/`, `obj/`, or `TestResults/`.
- Do not commit `d:\predict\index_extract_ta.csv` or retarget `run_qqq` at a new data file unless Victor asks.
- Do not add a root solution unless Victor asks.

Development happens only through Victor and Cursor agents.
