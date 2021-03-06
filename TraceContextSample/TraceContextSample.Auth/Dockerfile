#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["TraceContextSample.Auth/TraceContextSample.Auth.csproj", "TraceContextSample.Auth/"]
COPY ["TraceContextSample.Core/TraceContextSample.csproj", "TraceContextSample.Core/"]
RUN dotnet restore "TraceContextSample.Auth/TraceContextSample.Auth.csproj"
COPY . .
WORKDIR "/src/TraceContextSample.Auth"
RUN dotnet build "TraceContextSample.Auth.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TraceContextSample.Auth.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TraceContextSample.Auth.dll"]