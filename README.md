# Pascal compiler

## Fetures
* Unit Testing - xUnit
* Code coverage - coverlet
* Report generator

## Basic Commands

```
$ dotnet build
$ dotnet run App
$ dotnet msbuild -t:TestTarget

```
## Tests
Run specific test
```
$ dotnet test tests
```

Run all tests
```
$ dotnet msbuild src -t:UnitTest
```

Generate xml coverage in "reports" directory
```
$ dotnet msbuild src -t:Coverage
```

Generate html report in "reports" directory
```
$ dotnet msbuild src -t:Report
```
