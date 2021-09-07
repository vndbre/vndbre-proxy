FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["VndbReProxy.ConsoleApp/VndbReProxy.ConsoleApp.fsproj", "VndbReProxy.ConsoleApp/"]
RUN dotnet restore "VndbReProxy.ConsoleApp/VndbReProxy.ConsoleApp.fsproj"
COPY . .
WORKDIR "/src/VndbReProxy.ConsoleApp"
RUN dotnet build "VndbReProxy.ConsoleApp.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VndbReProxy.ConsoleApp.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VndbReProxy.ConsoleApp.dll"]
