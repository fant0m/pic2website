FROM mcr.microsoft.com/dotnet/core/aspnet:2.1-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR /src
COPY ["Pic2Website/Pic2Website.csproj", "Pic2Website/"]
RUN dotnet restore "Pic2Website/Pic2Website.csproj"
COPY . .
WORKDIR "/src/Pic2Website"
RUN dotnet build "Pic2Website.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Pic2Website.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Pic2Website.dll"]