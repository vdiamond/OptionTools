# OptionTools

.NET library and console tools for **option pricing, Greeks, and portfolio projection**. `OptionLib` implements Black-Scholes pricing and position math; `option_console` prints price/value projections across dates and underlying prices.

## Repository layout

```
OptionTools/
├── OptionLib/            # Pricing, Greeks, instruments, portfolios
├── option_console/       # Console app for position projections
├── optionlib_test/       # MSTest suite for OptionLib
└── .grok/rules/          # Coding standards
```

### Projects

| Project | Role |
|---------|------|
| **OptionLib** | Black-Scholes price and Greeks, option/underlying models, account and portfolio projection |
| **option_console** | Console app that projects option positions across dates and underlying prices |
| **optionlib_test** | Unit tests for `OptionLib` pricing |

### External dependencies

`option_console` project-references **DynamicTable** as a sibling repository. Clone it next to this repository so those references resolve:

```
source_code/
├── OptionTools/       # this repo
└── DynamicTable/      # Table, TableCsv, TableExtension
```

[`DynamicTable`](https://github.com/vdiamond/DynamicTable) is already public. `OptionLib` and `optionlib_test` do not require it.

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
dotnet build option_console/option_console.sln
```

## Test

```bash
dotnet test optionlib_test/optionlib_test.csproj
```

## Run

```bash
dotnet run --project option_console/option_console.csproj
```
