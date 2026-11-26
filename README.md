# OptionTools

.NET library and console tools for **option pricing, Greeks, and portfolio projection**. `OptionLib` implements Black-Scholes pricing and position math; `option_console` prints price/value projections across dates and underlying prices.

Agent guidance: [AGENTS.md](AGENTS.md).

## Repository layout

```
OptionTools/
├── OptionLib/            # Pricing, Greeks, instruments, portfolios
├── option_console/       # Console app for position projections
└── optionlib_test/       # MSTest project for OptionLib
```

There is no solution at the repository root. `dotnet build` with no project file fails. Build a project file, or `option_console/option_console.sln` when DynamicTable is present.

### Projects

| Project | Role |
|---------|------|
| **OptionLib** | Black-Scholes price and Greeks, option/underlying models, account and portfolio projection |
| **option_console** | Console app that projects option positions across dates and underlying prices |
| **optionlib_test** | MSTest project for `OptionLib`. The only test method is currently empty |

### External dependencies

`option_console` project-references **DynamicTable** as a sibling repository. Clone it next to this repository so those references resolve:

```
source_code/
├── OptionTools/       # this repo
└── DynamicTable/      # Table, TableCsv, TableExtension
```

[`DynamicTable`](https://github.com/vdiamond/DynamicTable) is already public. `OptionLib` and `optionlib_test` do not require it.

`OptionLib/OptionLib.sln` also lists `..\BinarySerializer\BinarySerializer.csproj`. That project is not in this repository, so building the solution fails. Build `OptionLib/OptionLib.csproj` instead.

## Requirements

- [.NET 10](https://dotnet.microsoft.com/) SDK

## Build

From the repository root:

```bash
dotnet build OptionLib/OptionLib.csproj
dotnet build optionlib_test/optionlib_test.csproj
```

With DynamicTable available as a sibling:

```bash
dotnet build option_console/option_console.csproj
dotnet build option_console/option_console.sln
```

## Test

```bash
dotnet test optionlib_test/optionlib_test.csproj
```

The suite currently contains one empty test (`TestPrice`), so a pass does not check pricing results.

## Run

`option_console` needs the DynamicTable sibling. `Main` calls `run_qqq()`, which loads `d:\predict\index_extract_ta.csv`. Without that file the process throws `FileNotFoundException`.

```bash
dotnet run --project option_console/option_console.csproj
```
