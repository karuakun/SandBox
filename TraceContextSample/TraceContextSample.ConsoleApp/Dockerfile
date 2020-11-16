#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["TraceContextSample.ConsoleApp/TraceContextSample.ConsoleApp.csproj", "TraceContextSample.ConsoleApp/"]
COPY ["TraceContextSample.Core/TraceContextSample.csproj", "TraceContextSample.Core/"]
RUN dotnet restore "TraceContextSample.ConsoleApp/TraceContextSample.ConsoleApp.csproj"
COPY . .
WORKDIR "/src/TraceContextSample.ConsoleApp"
RUN dotnet build "TraceContextSample.ConsoleApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TraceContextSample.ConsoleApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TraceContextSample.ConsoleApp.dll"]