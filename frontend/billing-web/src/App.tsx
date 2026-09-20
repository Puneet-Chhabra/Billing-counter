import { useEffect, useMemo, useState } from "react";
import {
  CircleDot,
  CirclePlus,
  Leaf,
  LogOut,
  Minus,
  Pencil,
  Plus,
  ReceiptText,
  Search,
  Settings2,
  Trash2,
  Utensils,
  WalletCards,
} from "lucide-react";

type MenuItem = {
  id: number;
  name: string;
  description: string;
  price: number;
  categoryId: number;
  isAvailable: boolean;
  isVegetarian: boolean;
  category?: { id: number; name: string };
};
type CartLine = MenuItem & { quantity: number };
type Order = {
  id: number;
  orderNumber: string;
  orderDate: string;
  subtotal: number;
  discount: number;
  tax: number;
  grandTotal: number;
  paymentMethod: string;
  customerName?: string;
  customerPhone?: string;
  customerEmail?: string;
  items: {
    menuItemId: number;
    itemName: string;
    unitPrice: number;
    quantity: number;
    total: number;
    isVegetarian: boolean;
  }[];
};
type Session = { token: string; username: string; role: string };
type Page = "billing" | "history" | "menu";

const API = import.meta.env.VITE_API_URL ?? "http://localhost:5000/api";
const money = (value: number) =>
  new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: "INR",
    maximumFractionDigits: 0,
  }).format(value);
const dateInput = (date: Date) => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
};
const today = new Date();
const currentMonthStart = dateInput(
  new Date(today.getFullYear(), today.getMonth(), 1),
);
const todayInput = dateInput(today);
const headerDate = today.toLocaleDateString("en-IN", {
  day: "numeric",
  month: "short",
  year: "numeric",
});

function FoodMark({ vegetarian }: { vegetarian: boolean }) {
  return vegetarian ? (
    <Leaf size={15} className="veg-icon" aria-label="Vegetarian" />
  ) : (
    <CircleDot size={15} className="nonveg-icon" aria-label="Non-vegetarian" />
  );
}

function App() {
  const [session, setSession] = useState<Session | null>(() =>
    JSON.parse(localStorage.getItem("billing-session") ?? "null"),
  );
  const [page, setPage] = useState<Page>("billing");
  const [loginForm, setLoginForm] = useState({
    username: "admin",
    password: "admin123",
  });
  const [loginError, setLoginError] = useState("");
  const [menu, setMenu] = useState<MenuItem[]>([]);
  const [cart, setCart] = useState<CartLine[]>([]);
  const [search, setSearch] = useState("");
  const [discount, setDiscount] = useState(0);
  const [billingTax, setBillingTax] = useState(0);
  const [billingTotal, setBillingTotal] = useState(0);
  const [payment, setPayment] = useState("UPI");
  const [customer, setCustomer] = useState({ name: "", phone: "", email: "" });
  const [notice, setNotice] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [orders, setOrders] = useState<Order[]>([]);
  const [showHistory, setShowHistory] = useState(false);
  const [historySearch, setHistorySearch] = useState("");
  const [historyFrom, setHistoryFrom] = useState(currentMonthStart);
  const [historyTo, setHistoryTo] = useState(todayInput);
  const [historyPayment, setHistoryPayment] = useState("All");
  const [historyTotal, setHistoryTotal] = useState(0);
  const [historyTotalAmount, setHistoryTotalAmount] = useState(0);
  const [historyPage, setHistoryPage] = useState(1);
  const historyPageSize = 10;
  const historyPageCount = Math.max(
    1,
    Math.ceil(historyTotal / historyPageSize),
  );
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);
  const [showMenuManagement, setShowMenuManagement] = useState(false);
  const [editingMenuItem, setEditingMenuItem] = useState<MenuItem | null>(null);
  const [menuManagementSearch, setMenuManagementSearch] = useState("");
  const [menuForm, setMenuForm] = useState({
    name: "",
    description: "",
    price: "",
    categoryId: "1",
    isAvailable: true,
    isVegetarian: true,
  });

  const request = (url: string, options: RequestInit = {}) =>
    fetch(url, {
      ...options,
      headers: {
        "Content-Type": "application/json",
        ...(session ? { Authorization: `Bearer ${session.token}` } : {}),
        ...options.headers,
      },
    });
  const loadMenu = () =>
    request(
      `${API}/${session?.role === "Admin" && page === "menu" ? "menu/manage" : "menu"}`,
    )
      .then((response) => {
        if (!response.ok) throw new Error("Menu request failed");
        return response.json();
      })
      .then(setMenu);
  const loadOrders = (
    searchValue = historySearch,
    fromValue = historyFrom,
    toValue = historyTo,
    paymentValue = historyPayment,
    pageValue = 1,
  ) => {
    const params = new URLSearchParams({
      page: String(pageValue),
      pageSize: String(historyPageSize),
    });
    if (searchValue.trim()) params.set("search", searchValue.trim());
    if (fromValue) params.set("from", fromValue);
    if (toValue) params.set("to", toValue);
    if (paymentValue !== "All") params.set("paymentMethod", paymentValue);
    return request(`${API}/orders?${params}`)
      .then((response) => response.json())
      .then((result) => {
        setOrders(result.items ?? []);
        setHistoryTotal(result.total ?? 0);
        setHistoryTotalAmount(result.totalAmount ?? 0);
      });
  };

  useEffect(() => {
    if (session)
      loadMenu().catch(() =>
        setNotice(
          `Unable to load the menu. Check that the API is running at ${API}.`,
        ),
      );
  }, [session, page]);
  useEffect(() => {
    if (!session || !cart.length) {
      setBillingTax(0);
      setBillingTotal(0);
      return;
    }
    request(`${API}/orders/preview`, {
      method: "POST",
      body: JSON.stringify({
        items: cart.map((line) => ({
          menuItemId: line.id,
          quantity: line.quantity,
        })),
        discount,
        paymentMethod: payment,
        customerName: null,
        customerPhone: null,
        customerEmail: null,
      }),
    })
      .then((response) => (response.ok ? response.json() : null))
      .then((result) => {
        if (result) {
          setBillingTax(result.tax);
          setBillingTotal(result.grandTotal);
        }
      });
  }, [session, cart, discount, payment]);
  useEffect(() => {
    if (page === "menu" && !showMenuManagement) setPage("billing");
    if (page === "history" && !showHistory) setPage("billing");
  }, [page, showMenuManagement, showHistory]);

  const filtered = useMemo(
    () =>
      menu.filter((item) =>
        `${item.name} ${item.description}`
          .toLowerCase()
          .includes(search.toLowerCase()),
      ),
    [menu, search],
  );
  const managedMenu = menu.filter((item) =>
    `${item.name} ${item.description}`
      .toLowerCase()
      .includes(menuManagementSearch.toLowerCase()),
  );
  const subtotal = cart.reduce(
    (sum, line) => sum + line.price * line.quantity,
    0,
  );
  const tax = billingTax;
  const total = billingTotal;
  const add = (item: MenuItem) =>
    setCart((lines) =>
      lines.some((line) => line.id === item.id)
        ? lines.map((line) =>
            line.id === item.id
              ? { ...line, quantity: line.quantity + 1 }
              : line,
          )
        : [...lines, { ...item, quantity: 1 }],
    );
  const change = (id: number, delta: number) =>
    setCart((lines) =>
      lines.flatMap((line) =>
        line.id !== id
          ? [line]
          : line.quantity + delta <= 0
            ? []
            : [{ ...line, quantity: line.quantity + delta }],
      ),
    );
  const save = async () => {
    if (!cart.length || isSaving) return;
    setIsSaving(true);
    try {
      const response = await request(`${API}/orders`, {
        method: "POST",
        body: JSON.stringify({
          items: cart.map((line) => ({
            menuItemId: line.id,
            quantity: line.quantity,
          })),
          discount,
          paymentMethod: payment,
          customerName: customer.name || null,
          customerPhone: customer.phone || null,
          customerEmail: customer.email || null,
        }),
      });
      if (response.ok) {
        const saved = (await response.json()) as Order;
        setNotice(`Order ${saved.orderNumber} saved.`);
        setCart([]);
        setCustomer({ name: "", phone: "", email: "" });
        await loadOrders();
      } else {
        const message = await response.text();
        setNotice(message || "Could not save this order.");
      }
    } catch {
      setNotice(`Could not reach the API at ${API}.`);
    } finally {
      setIsSaving(false);
    }
  };
  const startNewOrder = () => {
    setPage("billing");
    setShowHistory(false);
    setShowMenuManagement(false);
    setCart([]);
    setDiscount(0);
    setPayment("UPI");
    setCustomer({ name: "", phone: "", email: "" });
    setSearch("");
    setNotice("");
  };
  const openHistory = async () => {
    setPage("history");
    setShowHistory(true);
    setShowMenuManagement(false);
    setHistoryPage(1);
    await loadOrders(historySearch, historyFrom, historyTo, historyPayment, 1);
  };
  const changeHistoryPage = async (nextPage: number) => {
    setHistoryPage(nextPage);
    await loadOrders(
      historySearch,
      historyFrom,
      historyTo,
      historyPayment,
      nextPage,
    );
  };
  const openOrder = async (order: Order) => {
    const response = await request(`${API}/orders/${order.id}`);
    if (response.ok) setSelectedOrder((await response.json()) as Order);
  };
  const editOrder = async (_order: Order) => {
    setNotice("Completed orders cannot be edited. Create a new order instead.");
  };
  const printOrder = async (order: Order) => {
    await openOrder(order);
    setTimeout(() => window.print(), 100);
  };
  const openMenuManagement = () => {
    setPage("menu");
    setShowMenuManagement(true);
    setShowHistory(false);
  };
  const startMenuItem = (item?: MenuItem) => {
    setEditingMenuItem(item ?? null);
    setMenuForm(
      item
        ? {
            name: item.name,
            description: item.description,
            price: String(item.price),
            categoryId: String(item.categoryId),
            isAvailable: item.isAvailable,
            isVegetarian: item.isVegetarian,
          }
        : {
            name: "",
            description: "",
            price: "",
            categoryId: "1",
            isAvailable: true,
            isVegetarian: true,
          },
    );
  };
  const saveMenuItem = async (event: React.FormEvent) => {
    event.preventDefault();
    const payload = {
      id: editingMenuItem?.id ?? 0,
      name: menuForm.name,
      description: menuForm.description,
      price: Number(menuForm.price),
      categoryId: Number(menuForm.categoryId),
      isAvailable: menuForm.isAvailable,
      isVegetarian: menuForm.isVegetarian,
    };
    const response = await request(
      `${API}/menu${editingMenuItem ? `/${editingMenuItem.id}` : ""}`,
      {
        method: editingMenuItem ? "PUT" : "POST",
        body: JSON.stringify(payload),
      },
    );
    if (response.ok) {
      await loadMenu();
      setNotice(`${editingMenuItem ? "Updated" : "Added"} ${menuForm.name}.`);
      startMenuItem();
    } else setNotice("Could not save the menu item.");
  };
  const clearHistoryFilters = async () => {
    setHistorySearch("");
    setHistoryFrom(currentMonthStart);
    setHistoryTo(todayInput);
    setHistoryPayment("All");
    setHistoryPage(1);
    await loadOrders("", currentMonthStart, todayInput, "All", 1);
  };
  const login = async (event: React.FormEvent) => {
    event.preventDefault();
    setLoginError("");
    const response = await fetch(`${API}/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(loginForm),
    });
    if (!response.ok) {
      setLoginError("Invalid username or password.");
      return;
    }
    const nextSession = (await response.json()) as Session;
    localStorage.setItem("billing-session", JSON.stringify(nextSession));
    setSession(nextSession);
  };
  const logout = () => {
    localStorage.removeItem("billing-session");
    setSession(null);
    setPage("billing");
    setShowHistory(false);
    setShowMenuManagement(false);
  };

  if (!session)
    return (
      <main className="login-shell">
        <form className="login-card" onSubmit={login}>
          <span className="brand-mark">
            <Utensils size={18} />
          </span>
          <p className="eyebrow">Counter billing</p>
          <h1>Sign in</h1>
          <p className="muted">Use your staff or admin account to continue.</p>
          <label>
            Username
            <input
              required
              value={loginForm.username}
              onChange={(event) =>
                setLoginForm({ ...loginForm, username: event.target.value })
              }
            />
          </label>
          <label>
            Password
            <input
              required
              type="password"
              value={loginForm.password}
              onChange={(event) =>
                setLoginForm({ ...loginForm, password: event.target.value })
              }
            />
          </label>
          {loginError && <div className="login-error">{loginError}</div>}
          <button className="save-button" type="submit">
            Sign in
          </button>
        </form>
      </main>
    );

  return (
    <main className={`shell page-${page}`}>
      <header className="topbar">
        <div className="brand">
          <span className="brand-mark">
            <Utensils size={18} />
          </span>
          <div>
            <strong>Counter</strong>
            <small>
              {session.username} · {session.role}
            </small>
          </div>
        </div>
        <nav className="topbar-meta page-nav">
          <button
            className={
              page === "billing" ? "history-link active" : "history-link"
            }
            onClick={startNewOrder}
          >
            <ReceiptText size={14} /> Billing
          </button>
          {session.role === "Admin" && (
            <button
              className={
                page === "menu" ? "history-link active" : "history-link"
              }
              onClick={openMenuManagement}
            >
              <Settings2 size={14} /> Menu
            </button>
          )}
          <button
            className={
              page === "history" ? "history-link active" : "history-link"
            }
            onClick={openHistory}
          >
            Order history
          </button>
          <button className="history-link" onClick={logout}>
            <LogOut size={14} /> Sign out
          </button>
          <span className="status-dot" /> Register online{" "}
          <span className="date">{headerDate}</span>
        </nav>
      </header>
      <section className="intro">
        <div>
          <p className="eyebrow">Daily service · register 01</p>
          <h1>Build an order</h1>
          <p className="muted">
            Pick menu items, tune the quantities, and send the bill when it is
            ready.
          </p>
        </div>
        <button className="outline-button" onClick={startNewOrder}>
          <ReceiptText size={16} /> New order
        </button>
      </section>
      {notice && <div className="notice">{notice}</div>}
      {showHistory && (
        <div className="history-summary">
          <span>{historyTotal} matching orders</span>
          <strong>Revenue {money(historyTotalAmount)}</strong>
        </div>
      )}
      {showHistory && (
        <div className="history-pagination">
          <button
            className="outline-button"
            disabled={historyPage <= 1}
            onClick={() => void changeHistoryPage(historyPage - 1)}
          >
            Previous
          </button>
          <span>
            Page {historyPage} of {historyPageCount}
          </span>
          <button
            className="outline-button"
            disabled={historyPage >= historyPageCount}
            onClick={() => void changeHistoryPage(historyPage + 1)}
          >
            Next
          </button>
        </div>
      )}
      {showMenuManagement && (
        <section className="management-panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">Admin workspace</p>
              <h2>Menu management</h2>
            </div>
            <button
              className="outline-button"
              onClick={() => setShowMenuManagement(false)}
            >
              Back to billing
            </button>
          </div>
          <div className="management-grid">
            <form className="menu-form" onSubmit={saveMenuItem}>
              <h3>{editingMenuItem ? "Edit item" : "Add item"}</h3>
              <label>
                Name
                <input
                  required
                  value={menuForm.name}
                  onChange={(event) =>
                    setMenuForm({ ...menuForm, name: event.target.value })
                  }
                />
              </label>
              <label>
                Description
                <input
                  value={menuForm.description}
                  onChange={(event) =>
                    setMenuForm({
                      ...menuForm,
                      description: event.target.value,
                    })
                  }
                />
              </label>
              <label>
                Price
                <input
                  required
                  type="number"
                  min="0"
                  step="0.01"
                  value={menuForm.price}
                  onChange={(event) =>
                    setMenuForm({ ...menuForm, price: event.target.value })
                  }
                />
              </label>
              <label className="food-choice">
                <input
                  type="radio"
                  checked={menuForm.isVegetarian}
                  onChange={() =>
                    setMenuForm({ ...menuForm, isVegetarian: true })
                  }
                />{" "}
                <Leaf size={14} className="veg-icon" /> Vegetarian
              </label>
              <label className="food-choice">
                <input
                  type="radio"
                  checked={!menuForm.isVegetarian}
                  onChange={() =>
                    setMenuForm({ ...menuForm, isVegetarian: false })
                  }
                />{" "}
                <CircleDot size={14} className="nonveg-icon" /> Non-vegetarian
              </label>
              <label className="availability">
                <input
                  type="checkbox"
                  checked={menuForm.isAvailable}
                  onChange={(event) =>
                    setMenuForm({
                      ...menuForm,
                      isAvailable: event.target.checked,
                    })
                  }
                />{" "}
                Available for billing
              </label>
              <div className="form-actions">
                <button className="save-button" type="submit">
                  {editingMenuItem ? "Update item" : "Add item"}
                </button>
                {editingMenuItem && (
                  <button
                    className="outline-button"
                    type="button"
                    onClick={() => startMenuItem()}
                  >
                    Cancel
                  </button>
                )}
              </div>
            </form>
            <div className="managed-items">
              <div className="managed-heading">
                <h3>Current menu</h3>
                <span>{managedMenu.length} items</span>
              </div>
              <label className="management-search">
                <Search size={16} />
                <input
                  value={menuManagementSearch}
                  onChange={(event) =>
                    setMenuManagementSearch(event.target.value)
                  }
                  placeholder="Search current menu"
                />
              </label>
              {managedMenu.length === 0 ? (
                <p className="muted">No matching menu items.</p>
              ) : (
                managedMenu.map((item) => (
                  <div className="managed-item" key={item.id}>
                    <div>
                      <strong>
                        <FoodMark vegetarian={item.isVegetarian} /> {item.name}
                      </strong>
                      <span>
                        {money(item.price)} · {item.description}
                      </span>
                    </div>
                    <button
                      className="outline-button"
                      onClick={() => startMenuItem(item)}
                    >
                      <Pencil size={14} /> Edit
                    </button>
                  </div>
                ))
              )}
            </div>
          </div>
        </section>
      )}
      {showHistory && (
        <section className="history-panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">Saved tickets</p>
              <h2>Order history</h2>
            </div>
            <button
              className="outline-button"
              onClick={() => {
                setShowHistory(false);
                setSelectedOrder(null);
              }}
            >
              Back to billing
            </button>
          </div>
          <div className="history-filters">
            <label>
              <Search size={16} />
              <input
                value={historySearch}
                onChange={(event) => setHistorySearch(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Enter") {
                    event.preventDefault();
                    void loadOrders();
                  }
                }}
                placeholder="Order number or item"
              />
            </label>
            <label>
              From
              <input
                type="date"
                value={historyFrom}
                onChange={(event) => setHistoryFrom(event.target.value)}
              />
            </label>
            <label>
              To
              <input
                type="date"
                value={historyTo}
                onChange={(event) => setHistoryTo(event.target.value)}
              />
            </label>
            <label>
              Payment
              <select
                value={historyPayment}
                onChange={(event) => setHistoryPayment(event.target.value)}
              >
                <option>All</option>
                <option>UPI</option>
                <option>Cash</option>
                <option>Card</option>
                <option>Other</option>
              </select>
            </label>
            <button className="save-button" onClick={() => loadOrders()}>
              Search
            </button>
            <button className="outline-button" onClick={clearHistoryFilters}>
              Clear
            </button>
          </div>
          <p className="history-result-count">{historyTotal} matching orders</p>
          {orders.length === 0 ? (
            <p className="muted">No saved orders match these filters.</p>
          ) : (
            <div className="history-list">
              {orders.map((order) => (
                <article className="history-row" key={order.id}>
                  <div>
                    <strong>{order.orderNumber}</strong>
                    <span>
                      {new Date(order.orderDate).toLocaleString()} ·{" "}
                      {order.paymentMethod}
                    </span>
                  </div>
                  <b>{money(order.grandTotal)}</b>
                  <div className="history-actions">
                    <button
                      className="outline-button"
                      onClick={() => openOrder(order)}
                    >
                      View
                    </button>
                    <button
                      className="outline-button"
                      onClick={() => editOrder(order)}
                    >
                      Edit
                    </button>
                    <button
                      className="outline-button"
                      onClick={() => printOrder(order)}
                    >
                      Print
                    </button>
                  </div>
                </article>
              ))}
            </div>
          )}
          {selectedOrder && (
            <div className="print-order">
              <div className="detail-heading">
                <div>
                  <p className="eyebrow">Order details</p>
                  <h2>{selectedOrder.orderNumber}</h2>
                </div>
                <div className="detail-actions">
                  <button
                    className="outline-button"
                    onClick={() => editOrder(selectedOrder)}
                  >
                    Edit order
                  </button>
                  <button
                    className="outline-button"
                    onClick={() => printOrder(selectedOrder)}
                  >
                    Print bill
                  </button>
                </div>
              </div>
              {(selectedOrder.customerName ||
                selectedOrder.customerPhone ||
                selectedOrder.customerEmail) && (
                <div className="customer-detail">
                  <strong>Customer</strong>
                  <span>{selectedOrder.customerName}</span>
                  <span>{selectedOrder.customerPhone}</span>
                  <span>{selectedOrder.customerEmail}</span>
                </div>
              )}
              {selectedOrder.items.map((item, index) => (
                <div key={`${item.itemName}-${index}`}>
                  <span>
                    <FoodMark vegetarian={item.isVegetarian} /> {item.itemName}{" "}
                    × {item.quantity} @ {money(item.unitPrice)}
                  </span>
                  <b>{money(item.total)}</b>
                </div>
              ))}
              <hr />
              <div>
                <span>Subtotal</span>
                <b>{money(selectedOrder.subtotal)}</b>
              </div>
              <div>
                <span>Discount</span>
                <b>-{money(selectedOrder.discount)}</b>
              </div>
              <div>
                <span>GST</span>
                <b>{money(selectedOrder.tax)}</b>
              </div>
              <div>
                <span>Total</span>
                <strong>{money(selectedOrder.grandTotal)}</strong>
              </div>
            </div>
          )}
        </section>
      )}
      <div className="workspace">
        <section className="menu-panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">Available now</p>
              <h2>Menu</h2>
            </div>
            <span className="item-count">{filtered.length} items</span>
          </div>
          <label className="search">
            <Search size={17} />
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search menu"
            />
          </label>
          <div className="menu-grid">
            {filtered.map((item) => (
              <article className="menu-item" key={item.id}>
                <div>
                  <span className="category">
                    <FoodMark vegetarian={item.isVegetarian} />{" "}
                    {item.category?.name ?? "Menu"}
                  </span>
                  <h3>{item.name}</h3>
                  <p>{item.description}</p>
                </div>
                <div className="menu-item-footer">
                  <strong>{money(item.price)}</strong>
                  <button
                    className="add-button"
                    onClick={() => add(item)}
                    aria-label={`Add ${item.name}`}
                  >
                    <Plus size={17} />
                  </button>
                </div>
              </article>
            ))}
          </div>
        </section>
        <aside className="order-panel">
          <div className="panel-heading">
            <div>
              <p className="eyebrow">Current ticket</p>
              <h2>New order</h2>
            </div>
            <WalletCards size={21} />
          </div>
          <div className="cart">
            {cart.length === 0 ? (
              <div className="empty">
                <ReceiptText size={28} />
                <p>Your order is empty</p>
                <small>Add something from the menu to begin.</small>
              </div>
            ) : (
              cart.map((line) => (
                <div className="cart-line" key={line.id}>
                  <div>
                    <strong>
                      <FoodMark vegetarian={line.isVegetarian} /> {line.name}
                    </strong>
                    <span>{money(line.price)} each</span>
                  </div>
                  <div className="line-actions">
                    <button onClick={() => change(line.id, -1)}>
                      <Minus size={14} />
                    </button>
                    <b>{line.quantity}</b>
                    <button onClick={() => change(line.id, 1)}>
                      <Plus size={14} />
                    </button>
                    <button
                      className="remove"
                      onClick={() =>
                        setCart((lines) =>
                          lines.filter((cartLine) => cartLine.id !== line.id),
                        )
                      }
                    >
                      <Trash2 size={15} />
                    </button>
                  </div>
                </div>
              ))
            )}
          </div>
          <div className="checkout">
            <div className="customer-fields">
              <p className="eyebrow">
                Customer details <span>(optional)</span>
              </p>
              <input
                value={customer.name}
                onChange={(event) =>
                  setCustomer({ ...customer, name: event.target.value })
                }
                placeholder="Customer name"
              />
              <input
                value={customer.phone}
                onChange={(event) =>
                  setCustomer({ ...customer, phone: event.target.value })
                }
                placeholder="Phone number"
              />
              <input
                type="email"
                value={customer.email}
                onChange={(event) =>
                  setCustomer({ ...customer, email: event.target.value })
                }
                placeholder="Email address"
              />
            </div>
            <div className="form-row">
              <label>Discount</label>
              <input
                type="number"
                min="0"
                value={discount || ""}
                onChange={(event) => setDiscount(Number(event.target.value))}
                placeholder="0"
              />
            </div>
            <div className="form-row">
              <label>Payment</label>
              <select
                value={payment}
                onChange={(event) => setPayment(event.target.value)}
              >
                <option>UPI</option>
                <option>Cash</option>
                <option>Card</option>
                <option>Other</option>
              </select>
            </div>
            <div className="totals">
              <div>
                <span>Subtotal</span>
                <b>{money(subtotal)}</b>
              </div>
              <div>
                <span>Discount</span>
                <b>-{money(Math.min(discount, subtotal))}</b>
              </div>
              <div>
                <span>GST</span>
                <b>{money(tax)}</b>
              </div>
              <div className="grand">
                <span>Total</span>
                <strong>{money(total)}</strong>
              </div>
            </div>
            <button
              className="save-button"
              disabled={!cart.length || isSaving}
              onClick={save}
            >
              <CirclePlus size={18} /> {isSaving ? "Saving…" : "Save order"}
            </button>
          </div>
        </aside>
      </div>
    </main>
  );
}

export default App;
