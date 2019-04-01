FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# install System.Drawing & leptonica native dependencies
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libc6-dev \
        libgdiplus \
        libx11-dev \
		libgif7 \
		libjpeg62 \
		libopenjp2-7 \
		libpng16-16 \
		libtiff5 \
		libwebp6 \
		tesseract-ocr \
     && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-stretch AS build
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