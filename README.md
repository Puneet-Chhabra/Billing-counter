# Billing Software

A billing workspace for a small food business. The current milestone includes a React/Vite order-entry screen and an ASP.NET Core API with EF Core SQLite persistence.

## Run locally

### API

```powershell
dotnet run --project backend/Billing.Api/Billing.Api.csproj --urls http://localhost:5000
```

The API creates `billing.db` on first start and seeds a Burgers category with Classic Burger and French Fries.

## Local login accounts

- Admin: `admin` / `admin123`
- Billing staff: `staff` / `staff123`

Change these development credentials before using the application outside a local environment. The Admin role can manage the menu; both roles can create and view orders.

After authentication changes, stop any older API process and start it again so the Users table and JWT middleware are loaded.

### Frontend

```powershell
Set-Location frontend/billing-web
npm.cmd install
npm.cmd run dev
```

Set `VITE_API_URL` when the API uses a different URL. The default development URL is `http://localhost:5080/api`.

## Verified endpoints

- `GET /api/menu`
- `POST /api/orders`
- `GET /api/orders?page=1&pageSize=20&search=...`
- `GET /api/orders/{id}`
- `POST /api/menu`
- `PUT /api/menu/{id}`

Order creation always reads current menu prices from the database and stores the item name, unit price, GST percentage, quantity, and line total on the order item.

## Validation

```powershell
dotnet test BillingSoftware.slnx
Set-Location frontend/billing-web
npm.cmd run build
```

## Next implementation slices

Authentication and role authorization, admin menu/settings screens, dashboard summaries, receipt print/PDF generation, PostgreSQL provider configuration, migrations, and API integration tests are planned after this foundation.
