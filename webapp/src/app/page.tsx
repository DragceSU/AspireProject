"use client";

import { useMemo, useState } from "react";

type Product = {
  id: string;
  name: string;
  price: number;
  description: string;
  highlights: string[];
  badge: string;
};

type ControlStep = {
  title: string;
  detail: string;
  action: string;
  badge: string;
};

type CartState = Record<string, number>;

type OrderReceipt = {
  id: string;
  items: Array<{ id: string; name: string; quantity: number; price: number }>;
  total: number;
  notes: string;
  timestamp: string;
};

type OrderAcknowledgement = {
  orderId: string;
  receivedAt: string;
  total: number;
  currency: string;
  status: string;
  notes?: string | null;
};

const PRODUCTS: Product[] = [
  {
    id: "trolley",
    name: "Expedition Trolley",
    price: 189,
    description: "Foldable cargo companion with reinforced frame for quick station runs.",
    highlights: ["Carbon rails", "120L capacity", "Puncture-proof wheels"],
    badge: "New arrival",
  },
  {
    id: "bicycle",
    name: "Comet Bicycle",
    price: 890,
    description: "City-ready electric bicycle tuned for silent cruising and agile maneuvers.",
    highlights: ["80 km range", "Adaptive lights", "Hydraulic brakes"],
    badge: "E-bike",
  },
  {
    id: "laptop",
    name: "Nebula Laptop",
    price: 2190,
    description: "Performance workstation for on-the-go creation, compiling, and command.",
    highlights: ["14\" OLED", "32GB RAM", "2TB NVMe"],
    badge: "Creator kit",
  },
];

const CONTROL_STEPS: ControlStep[] = [
  {
    title: "Curate loadout",
    detail: "Cycle through the three launch-ready articles and set the quantities that fit your mission.",
    action: "Browse catalog",
    badge: "Step 01",
  },
  {
    title: "Confirm manifest",
    detail: "Lock the cart, add delivery notes, and let the system validate the stock in real time.",
    action: "Confirm cart",
    badge: "Step 02",
  },
  {
    title: "Dispatch order",
    detail: "Fire the order to generate a receipt with timestamped reference codes for tracking.",
    action: "Send request",
    badge: "Step 03",
  },
];

const API_BASE_URL = process.env.NEXT_PUBLIC_WEBAPI_BASE_URL ?? "http://localhost:5088";

function formatCurrency(value: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
  }).format(value);
}

export default function Home() {
  const [cart, setCart] = useState<CartState>({});
  const [isConfirmed, setIsConfirmed] = useState(false);
  const [notes, setNotes] = useState("");
  const [receipt, setReceipt] = useState<OrderReceipt | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const cartItems = useMemo(() => {
    return PRODUCTS.filter((product) => cart[product.id])
      .map((product) => ({
        id: product.id,
        name: product.name,
        price: product.price,
        quantity: cart[product.id] ?? 0,
      }))
      .filter((item) => item.quantity > 0);
  }, [cart]);

  const totalItems = cartItems.reduce((total, item) => total + item.quantity, 0);
  const subtotal = cartItems.reduce((total, item) => total + item.quantity * item.price, 0);
  const cartValue = formatCurrency(subtotal);

  const adjustItem = (productId: string, delta: number) => {
    setCart((prev) => {
      const current = prev[productId] ?? 0;
      const nextValue = Math.max(0, current + delta);
      const nextState = { ...prev };
      if (nextValue === 0) {
        delete nextState[productId];
      } else {
        nextState[productId] = nextValue;
      }
      return nextState;
    });
    setIsConfirmed(false);
  };

  const confirmCart = () => {
    if (!totalItems) {
      return;
    }
    setIsConfirmed(true);
  };

  const placeOrder = async () => {
    if (!isConfirmed || !totalItems || isSubmitting) {
      return;
    }

    const orderId = `ORD-${Date.now().toString().slice(-6)}`;
    const itemsSnapshot = cartItems.map((item) => ({ ...item }));
    const payload = {
      orderId,
      items: itemsSnapshot.map((item) => ({
        productId: item.id,
        name: item.name,
        quantity: item.quantity,
        unitPrice: item.price,
      })),
      total: subtotal,
      currency: "USD",
      notes: notes.trim(),
      placedAt: new Date().toISOString(),
    };

    setIsSubmitting(true);
    try {
      const response = await fetch(`${API_BASE_URL}/api/orders`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        throw new Error(`Order submission failed with status ${response.status}`);
      }

      const acknowledgement = (await response.json()) as OrderAcknowledgement;

      const orderReceipt: OrderReceipt = {
        id: acknowledgement.orderId,
        items: itemsSnapshot,
        total: acknowledgement.total,
        notes: acknowledgement.notes ?? "",
        timestamp: new Date(acknowledgement.receivedAt).toLocaleString(),
      };

      setReceipt(orderReceipt);
      setCart({});
      setNotes("");
      setIsConfirmed(false);
    } catch (error) {
      console.error("Failed to submit order", error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const heroMetrics = [
    { label: "Cart value", value: cartValue },
    { label: "Items", value: totalItems.toString() },
    { label: "State", value: isConfirmed ? "Confirmed" : "Draft" },
  ];

  return (
    <div className="shop-shell">
      <header className="shop-hero">
        <div className="hero-ambient hero-ambient-one" />
        <div className="hero-ambient hero-ambient-two" />
        <div className="hero-copy">
          <p className="shop-eyebrow">Aspire mobility depot</p>
          <h1>
            Outfit your mission with <span>ready-to-go</span> essentials.
          </h1>
          <p>
            Pick your gear. Add to cart. Dispatch.
          </p>
          <div className="hero-actions">
            <div className="hero-pill">
              <span className="dot online" />
              Cart status: {isConfirmed ? "Confirmed" : "Draft"}
            </div>
            <div className="hero-pill">
              Items ready: <strong>{totalItems}</strong>
            </div>
          </div>
          <div className="hero-metrics">
            {heroMetrics.map((metric) => (
              <div key={metric.label} className="hero-metric">
                <p className="metric-label">{metric.label}</p>
                <p className="metric-value">{metric.value}</p>
              </div>
            ))}
          </div>
        </div>
        <div className="hero-status">
          <p className="hero-status-label">Latest receipt</p>
          {receipt ? (
            <>
              <p className="hero-status-value">{receipt.id}</p>
              <p className="hero-status-detail">{receipt.timestamp}</p>
              <p className="hero-status-detail">{formatCurrency(receipt.total)}</p>
            </>
          ) : (
            <p className="hero-status-placeholder">No orders placed yet.</p>
          )}
        </div>
      </header>

      <section className="section catalog-section">
        <div className="section-header">
          <p className="section-eyebrow">Fleet inventory</p>
          <h2>Pick your launch vehicle</h2>
          <p>Only three products for now, each calibrated for a different type of mission.</p>
        </div>
        <div className="catalog-grid">
          {PRODUCTS.map((product) => (
            <article key={product.id} className="product-card">
              <div className="product-badge">{product.badge}</div>
              <header>
                <h3>{product.name}</h3>
                <p className="product-price">{formatCurrency(product.price)}</p>
              </header>
              <p className="product-description">{product.description}</p>
              <ul className="product-highlights">
                {product.highlights.map((detail) => (
                  <li key={detail}>{detail}</li>
                ))}
              </ul>
              <button className="btn primary" onClick={() => adjustItem(product.id, 1)}>
                Add to cart
              </button>
            </article>
          ))}
        </div>
      </section>

      <section className="cart-section">
        <div className="cart-panel">
          <div className="section-header">
            <p className="section-eyebrow">Mission cart</p>
            <h2>Confirm your manifest</h2>
            <p>
              Review each item, lock it in, and press order. You can adjust quantities at any time before confirmation.
            </p>
          </div>

          <div className="cart-metrics">
            <div className="metric-card">
              <p className="metric-label">Items</p>
              <p className="metric-value">{totalItems}</p>
            </div>
            <div className="metric-card">
              <p className="metric-label">Subtotal</p>
              <p className="metric-value">{formatCurrency(subtotal)}</p>
            </div>
            <div className="metric-card">
              <p className="metric-label">Status</p>
              <p className="metric-value">{isConfirmed ? "Confirmed" : "Draft"}</p>
            </div>
          </div>

          <div className="cart-items">
            {cartItems.length === 0 ? (
              <p className="empty-cart">Cart is empty. Add a product to get started.</p>
            ) : (
              cartItems.map((item) => (
                <div key={item.id} className="cart-item">
                  <div>
                    <p className="cart-item-name">{item.name}</p>
                    <p className="cart-item-price">{formatCurrency(item.price)}</p>
                  </div>
                  <div className="cart-item-controls">
                    <button onClick={() => adjustItem(item.id, -1)} aria-label={`Remove one ${item.name}`}>
                      -
                    </button>
                    <span>{item.quantity}</span>
                    <button onClick={() => adjustItem(item.id, 1)} aria-label={`Add one ${item.name}`}>
                      +
                    </button>
                  </div>
                </div>
              ))
            )}
          </div>

          {cartItems.length > 0 && (
            <textarea
              className="cart-notes"
              placeholder="Add delivery notes or mission context (optional)"
              value={notes}
              onChange={(event) => setNotes(event.target.value)}
            />
          )}

          <div className="cart-actions">
            <button className="btn secondary" onClick={confirmCart} disabled={!totalItems || isConfirmed || isSubmitting}>
              Confirm cart
            </button>
            <button className="btn primary" onClick={placeOrder} disabled={!isConfirmed || !totalItems || isSubmitting}>
              {isSubmitting ? "Submitting..." : "Place order"}
            </button>
          </div>
        </div>

        <div className="receipt-panel">
          <div className="section-header">
            <p className="section-eyebrow">Order feed</p>
            <h2>Latest confirmation</h2>
          </div>

          {receipt ? (
            <div className="receipt-card">
              <p className="receipt-id">{receipt.id}</p>
              <p className="receipt-time">{receipt.timestamp}</p>
              <ul>
                {receipt.items.map((item) => (
                  <li key={item.id}>
                    {item.quantity} × {item.name} — {formatCurrency(item.price * item.quantity)}
                  </li>
                ))}
              </ul>
              {receipt.notes && <p className="receipt-notes">Notes: {receipt.notes}</p>}
              <p className="receipt-total">Total: {formatCurrency(receipt.total)}</p>
            </div>
          ) : (
            <div className="receipt-empty">
              <p>No order yet. Confirm your cart to generate the first receipt.</p>
            </div>
          )}
        </div>
      </section>
    </div>
  );
}
