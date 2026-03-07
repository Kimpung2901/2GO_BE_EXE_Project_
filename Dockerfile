# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY 2GO_EXE_Project.API/2GO_EXE_Project.API.csproj 2GO_EXE_Project.API/
COPY 2GO_EXE_Project.BAL/2GO_EXE_Project.BLL.csproj 2GO_EXE_Project.BAL/
COPY 2GO_EXE_Project.Domain/2GO_EXE_Project.Domain.csproj 2GO_EXE_Project.Domain/
COPY 2GO_EXE_Project.DAL/2GO_EXE_Project.DAL.csproj 2GO_EXE_Project.DAL/
RUN dotnet restore 2GO_EXE_Project.API/2GO_EXE_Project.API.csproj

COPY . .
RUN dotnet publish 2GO_EXE_Project.API/2GO_EXE_Project.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "2GO_EXE_Project.API.dll"]