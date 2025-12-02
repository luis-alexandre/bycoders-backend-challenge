# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Build + test image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy everything (solution + src + tests)
COPY . .

# Restore solution
RUN dotnet restore "CnabStore.sln"

# Run unit tests (fails the build if tests fail)
RUN dotnet test "CnabStore.sln"

# Publish API project
RUN dotnet publish "src/CnabStore.Api/CnabStore.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# API will listen on 8080 inside the container
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CnabStore.Api.dll"]
