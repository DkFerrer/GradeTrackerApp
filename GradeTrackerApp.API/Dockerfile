FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["GradeTrackerApp.API/GradeTrackerApp.API.csproj", "GradeTrackerApp.API/"]
RUN dotnet restore "GradeTrackerApp.API/GradeTrackerApp.API.csproj"
COPY . .
WORKDIR "/src/GradeTrackerApp.API"
RUN dotnet build "GradeTrackerApp.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GradeTrackerApp.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GradeTrackerApp.API.dll"]