@echo off
echo Restoring packages for all projects...

echo Restoring HybridCloudWorkloads.Core...
cd src\HybridCloudWorkloads.Core
dotnet restore
cd ..\..

echo Restoring HybridCloudWorkloads.Infrastructure...
cd src\HybridCloudWorkloads.Infrastructure
dotnet restore
cd ..\..

echo Restoring HybridCloudWorkloads.API...
cd src\HybridCloudWorkloads.API
dotnet restore
cd ..\..

echo All packages restored!
echo.
echo Build solution: dotnet build
echo Run API: 
cd src\HybridCloudWorkloads.API
dotnet run
pause