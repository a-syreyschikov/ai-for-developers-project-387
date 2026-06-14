FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY Calendar.Backend/Calendar.Backend.csproj Calendar.Backend/
RUN dotnet restore Calendar.Backend/Calendar.Backend.csproj

COPY Calendar.Backend/ Calendar.Backend/
RUN dotnet publish Calendar.Backend/Calendar.Backend.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Calendar.Backend.dll"]
