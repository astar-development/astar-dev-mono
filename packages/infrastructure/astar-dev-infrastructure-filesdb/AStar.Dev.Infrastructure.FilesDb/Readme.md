# Infrastructure - FilesDb Notes

# Run from Infrastructure FilesDb directory

dotnet ef migrations add "InitialCreation" --startup-project "../../apis/AStar.Dev.Files.Api/AStar.Dev.Files.Api.csproj"

To apply, simply run the solution - migrations are automatically applied via the migrations project :-)
