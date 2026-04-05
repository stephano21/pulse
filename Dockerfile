# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Pulse.slnx ./
COPY src/Pulse.Domain/Pulse.Domain.csproj src/Pulse.Domain/
COPY src/Pulse.Application/Pulse.Application.csproj src/Pulse.Application/
COPY src/Pulse.Infrastructure/Pulse.Infrastructure.csproj src/Pulse.Infrastructure/
COPY src/Pulse.Api/Pulse.Api.csproj src/Pulse.Api/

RUN dotnet restore src/Pulse.Api/Pulse.Api.csproj

COPY src/ src/

RUN dotnet publish src/Pulse.Api/Pulse.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Pulse.Api.dll"]
