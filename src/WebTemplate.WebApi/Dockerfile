FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/WebTemplate.WebApi/WebTemplate.WebApi.csproj", "src/WebTemplate.WebApi/"]
COPY ["src/WebTemplate.Application/WebTemplate.Application.csproj", "src/WebTemplate.Application/"]
COPY ["src/WebTemplate.Domain/WebTemplate.Domain.csproj", "src/WebTemplate.Domain/"]
COPY ["src/WebTemplate.Infrastructure/WebTemplate.Infrastructure.csproj", "src/WebTemplate.Infrastructure/"]
RUN dotnet restore "src/WebTemplate.WebApi/WebTemplate.WebApi.csproj"
COPY . .
WORKDIR "/src/src/WebTemplate.WebApi"
RUN dotnet build "WebTemplate.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WebTemplate.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebTemplate.WebApi.dll"]
