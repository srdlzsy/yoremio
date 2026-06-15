FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY yoremio.sln ./
COPY Directory.Build.props ./
COPY global.json ./
COPY API/API.csproj API/
COPY Application/Application.csproj Application/
COPY Domain/Domain.csproj Domain/
COPY Infrastructure/Infrastructure.csproj Infrastructure/

RUN dotnet restore yoremio.sln

COPY . .
RUN dotnet publish API/API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

COPY --from=build /app/publish .

CMD ["sh", "-c", "dotnet API.dll --urls http://0.0.0.0:${PORT:-8080}"]