# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Copy the common library and server project
COPY LastLight.Common/ ./LastLight.Common/
COPY LastLight.Server/ ./LastLight.Server/

# Restore dependencies for the server
RUN dotnet restore LastLight.Server/LastLight.Server.csproj

# Build and publish the server
RUN dotnet publish LastLight.Server/LastLight.Server.csproj -c Release -o out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build-env /app/out .

# Expose the UDP port the server listens on
EXPOSE 5000/udp

# Run the server
ENTRYPOINT ["dotnet", "LastLight.Server.dll"]
