FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
COPY . /app
WORKDIR /app/src/Board/Host/Board.Host.DbMigrator
RUN dotnet restore
RUN dotnet publish -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# Непривилегированный пользователь из базового образа; HTTP слушается на порту 8080.
USER $APP_UID
ENTRYPOINT ["dotnet", "Board.Host.DbMigrator.dll"]
