FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0 AS build
WORKDIR /src
COPY ../Apollo.sln ./
COPY ../src/ServerCluster/Apollo.Server.Kubernetes/Apollo.Server.Kubernetes.csproj src/ServerCluster/Apollo.Server.Kubernetes/

RUN dotnet restore -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/src/ServerCluster/Apollo.Server.Kubernetes
RUN dotnet build Apollo.Server.Kubernetes.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Apollo.Server.Kubernetes.csproj -c Release -o /app

FROM base AS final
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Apollo.Server.Kubernetes.dll"]
