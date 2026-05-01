FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GrpcApi/GrpcApi.csproj", "GrpcApi/"]
RUN dotnet restore "GrpcApi/GrpcApi.csproj"
COPY . .
WORKDIR "/src/GrpcApi"
RUN dotnet build "GrpcApi.csproj" -c Release -o /app/build
RUN dotnet publish "GrpcApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install libmsquic for HTTP/3 support
RUN apt-get update && apt-get install -y libmsquic && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
COPY certs/ /app/certs/

ENV ASPNETCORE_URLS="https://+:5001"
ENV ASPNETCORE_Kestrel__Certificates__Default__Path="/app/certs/api.crt"
ENV ASPNETCORE_Kestrel__Certificates__Default__KeyPath="/app/certs/api.key"

EXPOSE 5001/tcp
EXPOSE 5001/udp

ENTRYPOINT ["dotnet", "GrpcApi.dll"]
