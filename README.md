# cortex-dotnet

## Run example on net8.0

```
cd example
dotnet build -p:TargetFramework=net8.0
```

## Publish new version

Make sure you have a valid API key.

```
cd src
dotnet pack -c Release
dotnet nuget push bin/Release/DataCortex.VER.nupkg --source nuget.org --api-key $NUGET_API_KEY
```
