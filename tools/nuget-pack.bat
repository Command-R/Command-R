CD %~dp0
nuget pack ..\CommandR\CommandR.csproj -Build
nuget pack ..\CommandR.WebApi\CommandR.WebApi.csproj -Build -IncludeReferencedProjects
PAUSE
