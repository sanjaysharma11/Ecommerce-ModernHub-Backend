# ✅ Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# ✅ SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# ✅ Copy and restore project dependencies
COPY ["ECommerceApi.csproj", "./"]
RUN dotnet restore

# ✅ Copy entire solution
COPY . .

# ✅ Publish application for production
RUN dotnet publish "ECommerceApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ✅ Final runtime stage
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# ✅ Set entrypoint
ENTRYPOINT ["dotnet", "ECommerceApi.dll"]
