nuget pack Transformalize.Transform.Metadata.nuspec -OutputDirectory "c:\temp\modules"
nuget pack Transformalize.Transform.Metadata.Autofac.nuspec -OutputDirectory "c:\temp\modules"

REM nuget push "c:\temp\modules\Transformalize.Transform.Metadata.0.6.9-beta.nupkg" -source https://api.nuget.org/v3/index.json
REM nuget push "c:\temp\modules\Transformalize.Transform.Metadata.Autofac.0.6.9-beta.nupkg" -source https://api.nuget.org/v3/index.json
