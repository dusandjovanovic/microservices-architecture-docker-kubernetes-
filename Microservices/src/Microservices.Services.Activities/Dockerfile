#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM microsoft/aspnetcore:2.0-stretch AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0-stretch AS build
WORKDIR /src
COPY ["src/Microservices.Services.Activities/Microservices.Services.Activities.csproj", "src/Microservices.Services.Activities/"]
COPY ["src/Microservices.Common/Microservices.Common.csproj", "src/Microservices.Common/"]
RUN dotnet restore "src/Microservices.Services.Activities/Microservices.Services.Activities.csproj"
COPY . .
WORKDIR "/src/src/Microservices.Services.Activities"
RUN dotnet build "Microservices.Services.Activities.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Microservices.Services.Activities.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microservices.Services.Activities.dll"]