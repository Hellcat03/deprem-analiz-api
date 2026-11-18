FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /kaynak
COPY src/ src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /yayin -p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /yayin .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet","Api.dll"]
