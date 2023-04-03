#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
COPY . /app/
RUN dotnet publish /app/Fidobot.csproj -c Release -o /runtime/
WORKDIR /runtime/

ENTRYPOINT ["dotnet", "Fidobot.dll"]
