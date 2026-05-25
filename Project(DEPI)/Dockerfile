FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln .
COPY *.csproj ./
COPY ./DAL/*.csproj ./DAL/
COPY ./PLL/*.csproj ./PLL/

# Copy the rest of the source code
COPY . ./

# Restore and publish
RUN dotnet restore HotelEcomm.csproj
RUN dotnet publish HotelEcomm.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "HotelEcomm.dll"]
