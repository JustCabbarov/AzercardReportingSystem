# 1. Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# 2. Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project definition files for layer caching
COPY ["RMS.Presentation/RMS.Presentation.csproj", "RMS.Presentation/"]
COPY ["RMS.Application/RMS.Application.csproj", "RMS.Application/"]
COPY ["RMS.Persitence/RMS.Persitence.csproj", "RMS.Persitence/"]
COPY ["RMS.Contract/RMS.Contract.csproj", "RMS.Contract/"]
COPY ["RMS.Domain/RMS.Domain.csproj", "RMS.Domain/"]

# Restore dependencies
RUN dotnet restore "RMS.Presentation/RMS.Presentation.csproj"

# Copy full source code
COPY . .

# Build the presentation project
WORKDIR "/src/RMS.Presentation"
RUN dotnet build "RMS.Presentation.csproj" -c Release -o /app/build

# 3. Publish application
FROM build AS publish
RUN dotnet publish "RMS.Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Final runtime stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directory for logs and models if needed
RUN mkdir -p Logs models

ENTRYPOINT ["dotnet", "RMS.Presentation.dll"]
