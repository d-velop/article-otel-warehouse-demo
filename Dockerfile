FROM mcr.microsoft.com/dotnet/sdk:8.0 as build

WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT Production
COPY . ./
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 as base

WORKDIR /app
COPY --from=build /app/out .
EXPOSE 5086
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

ENTRYPOINT [ "dotnet","/app/WarehouseManager.dll" ]
