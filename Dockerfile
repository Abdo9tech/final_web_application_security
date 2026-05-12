# ── Stage 1: Build ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first (layer cache for restore)
COPY HotelEcommerce.sln ./
COPY DAL/DAL.csproj DAL/
COPY PLL/PLL.csproj PLL/
COPY Project(DEPI)/HotelEcomm.csproj Project(DEPI)/

# Restore NuGet packages
RUN dotnet restore HotelEcommerce.sln

# Copy everything else
COPY . .

# Build and publish in Release mode
RUN dotnet publish "Project(DEPI)/HotelEcomm.csproj" -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create and switch to non-root user (MINIMAL CHANGE)
USER app
# aspnet:9.0 image already has 'app' user (UID 1000)

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build --chown=app:app /app/publish .

ENTRYPOINT ["dotnet", "HotelEcomm.dll"]


