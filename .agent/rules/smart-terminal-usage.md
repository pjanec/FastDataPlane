---
trigger: always_on
---

run 'dotnet build' and 'dotnet test' command using powershell one-liner  with  grepping the compilation errors, exceptions, failed tests, passed tests etc so you do not need to run same command again with redirection to file. Use your internal file tools wherever applicable instead of using terminal command to show the content of files.

something like 

```powershell
dotnet test Fdp.Tests\Fdp.Tests.csproj --filter "FullyQualifiedName~Fdp.Tests.FlightRecorderTests" --nologo --verbosity minimal |
Select-String -Pattern "Failed","Error","Assert","Expected","Actual","Unhandled","Exception","Build FAILED" -Context 0,4
```

or better by outputting to a .trx file and analyzing it using your internal file tool

```
dotnet test Fdp.Tests\Fdp.Tests.csproj `
  --nologo
  --verbosity minimal 
  --filter "FullyQualifiedName~Fdp.Tests.FlightRecorderTests" `
  --logger "trx;LogFileName=FlightRecorderTests.trx"
---