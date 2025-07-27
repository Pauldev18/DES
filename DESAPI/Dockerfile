FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# COPY file csproj riêng để cache tốt hơn
COPY DESAPI.csproj ./
RUN dotnet restore DESAPI.csproj

# COPY toàn bộ source code
COPY . .

# BUILD
RUN dotnet publish DESAPI.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DESAPI.dll"]
