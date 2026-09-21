FROM node:24-alpine AS frontend-build
WORKDIR /src/frontend/billing-web
COPY frontend/billing-web/package.json frontend/billing-web/package-lock.json ./
RUN npm ci
COPY frontend/billing-web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS api-build
WORKDIR /src
COPY BillingSoftware.slnx ./
COPY backend/ ./backend/
COPY --from=frontend-build /src/frontend/billing-web/dist/ ./backend/Billing.Api/wwwroot/
RUN dotnet publish backend/Billing.Api/Billing.Api.csproj -c Release -o /app/publish --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=api-build /app/publish ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Billing.Api.dll"]
