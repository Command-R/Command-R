CD %~dp0
nuget pack ..\CommandR\CommandR.csproj -Build
nuget pack ..\CommandR.MongoQueue\CommandR.MongoQueue.csproj -Build -IncludeReferencedProjects
nuget pack ..\CommandR.WebApi\CommandR.WebApi.csproj -Build -IncludeReferencedProjects
PAUSE
