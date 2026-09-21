# Billing Counter User Manual

## 1. Purpose

Billing Counter helps staff create food orders, calculate discounts and GST, record payment methods, print bills, and review order history. Administrators can also maintain menu items.

## 2. User roles

| Role | Billing | Order history | Menu management |
| --- | --- | --- | --- |
| Billing staff | Yes | Yes | No |
| Admin | Yes | Yes | Yes |

## 3. Start the application locally

Open a terminal at the repository root and start the API:

```powershell
dotnet run --project backend/Billing.Api/Billing.Api.csproj --urls http://localhost:5000
```

Leave that terminal running. Open a second terminal:

```powershell
Set-Location frontend/billing-web
npm.cmd ci
npm.cmd run dev
```

Open `http://localhost:5173` in a browser. `http://127.0.0.1:5173` is also supported.

Local development accounts:

- Admin: `admin` / `admin123`
- Billing staff: `staff` / `staff123`

These accounts are for local testing only and must not be used in Azure.

## 4. Sign in

1. Enter the username and password.
2. Select **Sign in**.
3. Confirm the username and role appear in the top-left corner.

If the API is unavailable, the login screen displays the API address it could not reach.

## 5. Create an order

1. Open **Billing**.
2. Use **Search menu** when needed.
3. Select the plus icon on a menu item to add it.
4. Use the plus, minus, or remove controls in the current ticket to adjust the order.
5. Enter the customer name. It is required.
6. Optionally enter phone and email.
7. Optionally enter a discount amount.
8. Select **UPI**, **Cash**, **Card**, or **Other**.
9. Review subtotal, discount, GST, and total.
10. Select **Save order**.
11. Confirm that an order-number message appears.

The API reloads current menu prices and calculates the final amount. Browser values are not trusted as authoritative prices.

Select **New order** to clear the current ticket and customer details.

## 6. Review and print orders

1. Open **Order history**.
2. Filter by order number or item name, customer name, date range, or payment method.
3. Select **Search** or press Enter in a search field.
4. Select **Clear** to restore the current-month filters.
5. Use **Previous** and **Next** when results span multiple pages.
6. Select **View** to display the complete saved bill.
7. Select **Print** to open the browser print dialog.

Completed orders cannot be edited. Create a new order instead.

## 7. Manage the menu

Menu management is available only to administrators.

### Add an item

1. Open **Menu**.
2. Enter name, description, and price.
3. Select the correct category.
4. Select vegetarian or non-vegetarian.
5. Keep **Available for billing** selected unless the item should be hidden.
6. Select **Add item**.

### Update an item

1. Find the item under **Current menu**.
2. Select **Edit**.
3. Change its details.
4. Select **Update item**.

Turn off **Available for billing** instead of deleting an item. Historical bills retain their original item details and prices.

## 8. Sign out

Select **Sign out** in the top navigation. Always sign out when leaving a shared billing device.

## 9. First-run test checklist

Use this checklist before entering real orders:

- Sign in as Admin.
- Confirm both seeded menu items appear.
- Add one item and change its quantity.
- Enter a test customer and save a Cash order.
- Open Order history and find the saved order.
- View the bill and open the print dialog.
- Add or update a menu item as Admin.
- Sign out and sign in as Billing staff.
- Confirm Billing staff cannot see Menu management.
- Repeat the billing flow at a narrow/mobile browser width.

Use an obvious customer name such as `Local UI Test` so test records are easy to identify.

## 10. Troubleshooting

### Unable to reach the API

Confirm the API terminal says it is listening on `http://localhost:5000`. Restart it after configuration changes.

### Invalid username or password

Confirm the API is running in the `Development` environment and use one of the local accounts above. A database created with different users is not overwritten on startup.

### Menu is temporarily empty after sign-in

The menu is loaded from the API after authentication. Wait briefly. If it remains empty, check the API terminal for errors and refresh the page.

### Port already in use

Stop the older process using the port, or start the API on another port and set `VITE_API_URL` before starting Vite.

### Reset local data

Stop the API, back up the database if needed, and delete `backend/Billing.Api/billing.db`. Restarting the API creates a fresh migrated development database. This permanently removes local orders and menu changes.

## 11. Production notes

Production runs the combined React/API container in Azure Container Apps and stores data in Azure SQL. Production credentials, signing keys, and database connection strings must be supplied through Container Apps secrets. See the repository README for deployment configuration.
