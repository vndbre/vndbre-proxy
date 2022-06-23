FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["VndbReProxy.Api/VndbReProxy.Api.fsproj", "VndbReProxy.Api/"]
RUN dotnet restore "VndbReProxy.Api/VndbReProxy.Api.fsproj"
COPY . .
WORKDIR "/src/VndbReProxy.Api"
RUN dotnet build "VndbReProxy.Api.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VndbReProxy.Api.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS http://*:$PORT
ENTRYPOINT ["dotnet", "VndbReProxy.Api.dll"]
