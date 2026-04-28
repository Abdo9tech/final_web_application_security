# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY ["Project(DEPI)/HotelEcomm.csproj", "Project(DEPI)/"]
COPY ["DAL/DAL.csproj", "DAL/"]
COPY ["PLL/PLL.csproj", "PLL/"]
RUN dotnet restore "Project(DEPI)/HotelEcomm.csproj"

# Copy source and publish
COPY . .
WORKDIR "/src/Project(DEPI)"
RUN dotnet publish "HotelEcomm.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd --gid 10001 appgroup \
    && useradd --uid 10001 --gid appgroup --create-home --shell /usr/sbin/nologin appuser

COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD curl -fsS http://127.0.0.1:8080/health/live || exit 1

USER appuser
ENTRYPOINT ["dotnet", "HotelEcomm.dll"]
