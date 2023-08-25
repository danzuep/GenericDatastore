# Run this before the copy commands to cache an image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
#COPY root.crt /usr/local/share/ca-certificates/
#RUN apk --no-cache add ca-certificates && update-ca-certificates
WORKDIR /app

# Pull the build image
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY Directory.Build.props ./Directory.Build.props
COPY Data.Base/*.csproj ./Data.Base/
COPY Data.EF/*.csproj ./Data.EF/
COPY Data.Mongo/*.csproj ./Data.Mongo/
COPY Data.Service/*.csproj ./Data.Service/
RUN dotnet restore ./Data.Service/Data.Service.csproj

# Copy everything else and build
FROM build AS publish
COPY Data.Base/. ./Data.Base/
COPY Data.EF/. ./Data.EF/
COPY Data.Mongo/. ./Data.Mongo/
COPY Data.Service/. ./Data.Service/
WORKDIR /src/Data.Service
RUN dotnet publish -c Release -o /app --no-restore

# Copy files from the build image into the runtime image
FROM base AS runtime
COPY --from=publish /app ./

# Set HTTPS binding
#ENV ASPNETCORE_URLS=https://+:443
#EXPOSE 443

# Set entrypoint
ENTRYPOINT ["dotnet", "Data.Service.dll"]