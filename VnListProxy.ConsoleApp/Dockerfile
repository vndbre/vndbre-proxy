FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["VnListProxy.ConsoleApp/VnListProxy.ConsoleApp.fsproj", "VnListProxy.ConsoleApp/"]
RUN dotnet restore "VnListProxy.ConsoleApp/VnListProxy.ConsoleApp.fsproj"
COPY . .
WORKDIR "/src/VnListProxy.ConsoleApp"
RUN dotnet build "VnListProxy.ConsoleApp.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VnListProxy.ConsoleApp.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VnListProxy.ConsoleApp.dll"]
