# ================================
# 🧱 Base runtime (ASP.NET Core)
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Dùng root để tránh lỗi permission khi mount volume
USER root

# Thư mục chứa app
WORKDIR /app

# Mở cổng ứng dụng
EXPOSE 8080
EXPOSE 8081

# ================================
# 🛠️ Build app
# ================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy toàn bộ source code
COPY . .

# Khôi phục package
RUN dotnet restore "DESAPI/DESAPI.csproj"

# Build project
WORKDIR "/src/DESAPI"
RUN dotnet build "DESAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ================================
# 🚀 Publish app
# ================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DESAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ================================
# 🔧 Final image để chạy thực tế
# ================================
FROM base AS final

WORKDIR /app

# Tắt IPv6 để tránh lỗi Npgsql trên host không hỗ trợ
ENV DOTNET_EnableIPv6=false

# Copy file đã publish
COPY --from=publish /app/publish .

# Khởi chạy ứng dụng
ENTRYPOINT ["dotnet", "DESAPI.dll"]
