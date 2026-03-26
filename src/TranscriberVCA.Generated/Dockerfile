#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Container we use for final publish
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build container
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Copy the code into the container
WORKDIR /src
COPY ["src/TranscriberVCA.Generated/TranscriberVCA.Generated.csproj", "TranscriberVCA.Generated/"]

# NuGet restore
RUN dotnet restore "TranscriberVCA.Generated/TranscriberVCA.Generated.csproj"
COPY ["src/TranscriberVCA.Generated/", "TranscriberVCA.Generated/"]

# Build the API
WORKDIR "TranscriberVCA.Generated"
RUN dotnet build "TranscriberVCA.Generated.csproj" -c Release -o /app/build

# Publish it
FROM build AS publish
RUN dotnet publish "TranscriberVCA.Generated.csproj" -c Release -o /app/publish

# Make the final image for publishing
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TranscriberVCA.Generated.dll"]
