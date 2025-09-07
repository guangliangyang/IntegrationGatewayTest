# Test Directory

## 📁 Structure

```
tests/
├── Integration.Tests/           # ✅ Integration Tests (xUnit)
│   ├── Integration/
│   │   ├── Base/               # Test infrastructure
│   │   └── Products/           # Product API tests
│   ├── Integration.Tests.csproj
│   ├── appsettings.Test.json   # Test configuration
│   └── .env                    # Environment variables
└── Performance.Tests/          # ⚠️ Performance Tests (NBomber)
    ├── Scenarios/              # Performance test scenarios  
    ├── Scripts/                # Execution scripts
    ├── README.md               # Detailed performance test docs
    └── Performance.Tests.csproj
```

## ✅ Integration Tests

**Status**: ✅ **Fully Working** (18/18 tests passing)

- **Framework**: xUnit + ASP.NET Core Testing
- **Discovery**: VSCode Test Explorer ✅
- **Location**: `tests/Integration.Tests/`
- **Namespace**: `Integration.Tests.Integration.*`

### Running Integration Tests

```bash
# From project root
cd tests/Integration.Tests
dotnet test

# Or from VSCode Test Explorer
```

## ⚠️ Performance Tests  

**Status**: ⚠️ **Excluded from Solution** (NBomber package conflict)

- **Framework**: NBomber 
- **Issue**: Version conflict between NBomber 5.8.2 and NBomber.Http 6.0.0
- **Location**: `tests/Performance.Tests/`

### Running Performance Tests Independently

```bash
# Direct execution (may have package issues)
cd tests/Performance.Tests
dotnet run light

# Using scripts
./Scripts/run-performance-tests.ps1 light
```

## 🔧 VSCode Configuration

The following files have been configured for optimal VSCode support:

- `.vscode/settings.json` - Workspace settings
- `.vscode/tasks.json` - Build and test tasks

### VSCode Test Explorer

To refresh test discovery:

1. **Reload Window**: `Ctrl+Shift+P` → "Developer: Reload Window"
2. **Restart OmniSharp**: `Ctrl+Shift+P` → "OmniSharp: Restart OmniSharp"  
3. **Refresh Tests**: `Ctrl+Shift+P` → "Test: Refresh Tests"

## 🎯 Current Status

- ✅ **Integration Tests**: Working perfectly with VSCode
- ✅ **Directory Structure**: Clean and organized
- ✅ **Solution Build**: No errors
- ⚠️ **Performance Tests**: Need NBomber package version fix

## 🔄 Next Steps

To re-enable performance tests in the solution:

1. Fix NBomber package version conflicts in `Performance.Tests.csproj`
2. Add the project back to `IntegrationGateway.sln`
3. Update solution build configuration

For now, performance tests can be run independently while integration tests work seamlessly with VSCode Test Explorer.