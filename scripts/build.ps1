$ErrorActionPreference = 'Stop'
dotnet restore "$PSScriptRoot\..\StudioManager.sln"
dotnet build "$PSScriptRoot\..\StudioManager.sln" -c Release --no-restore
dotnet test "$PSScriptRoot\..\tests\StudioManager.Tests\StudioManager.Tests.csproj" -c Release --no-build
