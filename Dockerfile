FROM node:24-alpine AS frontend-build

WORKDIR /src/apps/frontend

COPY apps/frontend/package.json apps/frontend/package-lock.json ./
RUN npm ci

COPY apps/frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build

WORKDIR /src

COPY apps/backend/Calendar.Backend/Calendar.Backend.csproj apps/backend/Calendar.Backend/
RUN dotnet restore apps/backend/Calendar.Backend/Calendar.Backend.csproj

COPY apps/backend/Calendar.Backend/ apps/backend/Calendar.Backend/
COPY --from=frontend-build /src/apps/frontend/dist apps/backend/Calendar.Backend/wwwroot
RUN dotnet publish apps/backend/Calendar.Backend/Calendar.Backend.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
EXPOSE 8080

COPY --from=backend-build /app/publish ./

ENTRYPOINT ["dotnet", "Calendar.Backend.dll"]
