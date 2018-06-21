FROM microsoft/aspnetcore:2.0-nanoserver-sac2016 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0-nanoserver-sac2016 AS build
WORKDIR /src
COPY src/ServerCluster/Soei.Apollo.ServerWorker/Soei.Apollo.ServerWorker.csproj src/ServerCluster/Soei.Apollo.ServerWorker/
RUN dotnet restore src/ServerCluster/Soei.Apollo.ServerWorker/Soei.Apollo.ServerWorker.csproj
COPY . .
WORKDIR /src/src/ServerCluster/Soei.Apollo.ServerWorker
RUN dotnet build Soei.Apollo.ServerWorker.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Soei.Apollo.ServerWorker.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Soei.Apollo.ServerWorker.dll"]
