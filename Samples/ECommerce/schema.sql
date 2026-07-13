-- E-Commerce Database Schema for Cloudflare D1

-- جدول سفارشات
CREATE TABLE IF NOT EXISTS orders (
    id TEXT PRIMARY KEY,
    customer_email TEXT NOT NULL,
    total_amount REAL NOT NULL,
    status TEXT DEFAULT 'Pending' CHECK(status IN ('Pending', 'Paid', 'Shipped', 'Cancelled', 'Refunded')),
    payment_intent_id TEXT,
    shipping_address TEXT,
    billing_address TEXT,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_orders_customer ON orders(customer_email);
CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status);
CREATE INDEX IF NOT EXISTS idx_orders_created ON orders(created_at);
CREATE INDEX IF NOT EXISTS idx_orders_payment ON orders(payment_intent_id);

-- جدول اقلام سفارش
CREATE TABLE IF NOT EXISTS order_items (
    id TEXT PRIMARY KEY,
    order_id TEXT NOT NULL,
    product_id TEXT NOT NULL,
    product_name TEXT NOT NULL,
    product_sku TEXT,
    price REAL NOT NULL,
    quantity INTEGER NOT NULL CHECK(quantity > 0),
    discount REAL DEFAULT 0,
    subtotal REAL NOT NULL,
    FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_order_items_order ON order_items(order_id);
CREATE INDEX IF NOT EXISTS idx_order_items_product ON order_items(product_id);

-- جدول محصولات
CREATE TABLE IF NOT EXISTS products (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT,
    slug TEXT UNIQUE,
    sku TEXT UNIQUE,
    price REAL NOT NULL CHECK(price >= 0),
    compare_at_price REAL,
    cost_price REAL,
    stock INTEGER NOT NULL DEFAULT 0 CHECK(stock >= 0),
    low_stock_threshold INTEGER DEFAULT 5,
    category TEXT NOT NULL,
    subcategory TEXT,
    brand TEXT,
    tags TEXT, -- JSON array
    images TEXT, -- JSON array of URLs
    attributes TEXT, -- JSON object
    is_active INTEGER DEFAULT 1,
    is_featured INTEGER DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_products_category ON products(category);
CREATE INDEX IF NOT EXISTS idx_products_slug ON products(slug);
CREATE INDEX IF NOT EXISTS idx_products_active ON products(is_active);
CREATE INDEX IF NOT EXISTS idx_products_featured ON products(is_featured);
CREATE INDEX IF NOT EXISTS idx_products_price ON products(price);

-- جدول مشتریان
CREATE TABLE IF NOT EXISTS customers (
    id TEXT PRIMARY KEY,
    email TEXT UNIQUE NOT NULL,
    first_name TEXT,
    last_name TEXT,
    phone TEXT,
    password_hash TEXT,
    avatar_url TEXT,
    email_verified INTEGER DEFAULT 0,
    total_orders INTEGER DEFAULT 0,
    total_spent REAL DEFAULT 0,
    last_order_date TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_customers_email ON customers(email);

-- جدول آدرس‌ها
CREATE TABLE IF NOT EXISTS addresses (
    id TEXT PRIMARY KEY,
    customer_id TEXT NOT NULL,
    type TEXT CHECK(type IN ('shipping', 'billing', 'both')),
    is_default INTEGER DEFAULT 0,
    first_name TEXT NOT NULL,
    last_name TEXT NOT NULL,
    company TEXT,
    address_line1 TEXT NOT NULL,
    address_line2 TEXT,
    city TEXT NOT NULL,
    state TEXT,
    postal_code TEXT NOT NULL,
    country TEXT NOT NULL,
    phone TEXT,
    FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_addresses_customer ON addresses(customer_id);

-- جدول دسته‌بندی‌ها
CREATE TABLE IF NOT EXISTS categories (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    slug TEXT UNIQUE NOT NULL,
    description TEXT,
    parent_id TEXT,
    image_url TEXT,
    sort_order INTEGER DEFAULT 0,
    is_active INTEGER DEFAULT 1,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (parent_id) REFERENCES categories(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_categories_parent ON categories(parent_id);
CREATE INDEX IF NOT EXISTS idx_categories_slug ON categories(slug);

-- جدول تخفیف‌ها و کدهای تبلیغاتی
CREATE TABLE IF NOT EXISTS discounts (
    id TEXT PRIMARY KEY,
    code TEXT UNIQUE NOT NULL,
    description TEXT,
    type TEXT CHECK(type IN ('percentage', 'fixed', 'free_shipping')),
    value REAL NOT NULL,
    min_order_amount REAL,
    max_discount_amount REAL,
    usage_limit INTEGER,
    usage_count INTEGER DEFAULT 0,
    starts_at TEXT,
    expires_at TEXT,
    is_active INTEGER DEFAULT 1,
    applicable_categories TEXT, -- JSON array
    applicable_products TEXT, -- JSON array
    created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_discounts_code ON discounts(code);
CREATE INDEX IF NOT EXISTS idx_discounts_active ON discounts(is_active);

-- جدول بررسی موجودی
CREATE TABLE IF NOT EXISTS inventory_logs (
    id TEXT PRIMARY KEY,
    product_id TEXT NOT NULL,
    quantity_change INTEGER NOT NULL,
    reason TEXT CHECK(reason IN ('sale', 'return', 'restock', 'adjustment', 'damaged')),
    reference_type TEXT, -- 'order', 'return', etc.
    reference_id TEXT,
    notes TEXT,
    created_by TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (product_id) REFERENCES products(id)
);

CREATE INDEX IF NOT EXISTS idx_inventory_product ON inventory_logs(product_id);
CREATE INDEX IF NOT EXISTS idx_inventory_reference ON inventory_logs(reference_type, reference_id);

-- جدول نظرات و امتیازات
CREATE TABLE IF NOT EXISTS reviews (
    id TEXT PRIMARY KEY,
    product_id TEXT NOT NULL,
    customer_id TEXT,
    customer_name TEXT,
    customer_email TEXT,
    rating INTEGER NOT NULL CHECK(rating >= 1 AND rating <= 5),
    title TEXT,
    content TEXT NOT NULL,
    is_verified_purchase INTEGER DEFAULT 0,
    is_approved INTEGER DEFAULT 0,
    helpful_count INTEGER DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE,
    FOREIGN KEY (customer_id) REFERENCES customers(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_reviews_product ON reviews(product_id);
CREATE INDEX IF NOT EXISTS idx_reviews_approved ON reviews(is_approved);
CREATE INDEX IF NOT EXISTS idx_reviews_rating ON reviews(rating);

-- داده‌های نمونه برای تست

INSERT OR IGNORE INTO categories (id, name, slug, description) VALUES 
    ('cat-electronics', 'Electronics', 'electronics', 'Electronic devices and accessories'),
    ('cat-office', 'Office', 'office', 'Office supplies and furniture'),
    ('cat-stationery', 'Stationery', 'stationery', 'Notebooks, pens, and writing materials');

INSERT OR IGNORE INTO products (id, name, description, slug, price, stock, category, is_active) VALUES 
    ('prod-1', 'Laptop Pro X1', 'High performance laptop with latest processor', 'laptop-pro-x1', 1299.99, 10, 'Electronics', 1),
    ('prod-2', 'Wireless Mouse', 'Ergonomic wireless mouse with long battery life', 'wireless-mouse', 49.99, 50, 'Electronics', 1),
    ('prod-3', 'Mechanical Keyboard', 'RGB mechanical keyboard with Cherry MX switches', 'mechanical-keyboard', 129.50, 25, 'Electronics', 1),
    ('prod-4', 'USB-C Hub', '7-in-1 USB-C Hub with HDMI and SD card reader', 'usb-c-hub', 39.99, 100, 'Electronics', 1),
    ('prod-5', 'Monitor 27"', '4K IPS Monitor with HDR support', 'monitor-27-4k', 349.00, 15, 'Electronics', 1),
    ('prod-6', 'Desk Lamp', 'LED Desk Lamp with adjustable brightness', 'desk-lamp', 24.99, 40, 'Office', 1),
    ('prod-7', 'Office Chair', 'Ergonomic office chair with lumbar support', 'office-chair', 199.99, 8, 'Office', 1),
    ('prod-8', 'Notebook Set', 'Pack of 5 premium notebooks', 'notebook-set', 12.50, 200, 'Stationery', 1);
