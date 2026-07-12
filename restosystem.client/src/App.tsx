import { useState, useEffect, useCallback } from 'react';
import './App.css';

// ─── API helper ───
const API = (import.meta as any).env?.VITE_API_URL ?? '';

async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API}/api${path}`, {
    headers: { 'Content-Type': 'application/json', ...init?.headers },
    ...init,
  });
  if (!res.ok) throw new Error(`API ${res.status}: ${res.statusText}`);
  return res.json();
}

// ─── Types ───
interface InventoryDashboard {
  totalItems: number; lowStockItems: number;
  nearExpiryBatches: number; totalWasteLast30Days: number;
}

interface HRDashboard {
  totalEmployees: number; activeEmployees: number;
  pendingRegularization: number; todayShifts: number; openIncidents: number;
}

// Status/role display helpers
const STATUS_LABELS: Record<number, string> = {
  0: 'Active', 1: 'Probationary', 2: 'Regularized',
  3: 'Resigned', 4: 'Terminated', 5: 'OnLeave',
};
const STATUS_BADGE: Record<number, string> = {
  0: 'badge-success', 1: 'badge-warning', 2: 'badge-success',
  3: 'badge-danger', 4: 'badge-danger', 5: 'badge-info',
};
const ROLE_LABELS: Record<number, string> = {
  0: 'Owner', 1: 'Store Manager', 2: 'Cashier',
  3: 'Inventory Clerk', 4: 'HR', 5: 'Admin',
};

function fmt(s: unknown): string {
  if (s === null || s === undefined) return '—';
  if (typeof s === 'number') return s.toString();
  return String(s);
}

function safeDate(v: unknown): Date | null {
  if (!v) return null;
  try { return new Date(v as string); } catch { return null; }
}

function fmtDate(v: unknown): string {
  const d = safeDate(v);
  return d ? d.toLocaleDateString() : '—';
}

function fmtDateTime(v: unknown): string {
  const d = safeDate(v);
  return d ? d.toLocaleString() : '—';
}

function fmtMoney(v: unknown, decimals = 2): string {
  if (v === null || v === undefined || v === '') return '—';
  const n = Number(v);
  return isNaN(n) ? '—' : `₱${n.toFixed(decimals)}`;
}

function fmtHours(v: unknown): string {
  if (v === null || v === undefined) return '—';
  const n = Number(v);
  return isNaN(n) ? '—' : `${n}h`;
}

// ─── Icon helper ───
const Icon = ({ name }: { name: string }) => {
  const icons: Record<string, string> = {
    dashboard: '📊', inventory: '📦', items: '📋', batches: '🏷️',
    waste: '🗑️', suppliers: '🚚', purchase: '📄', recipes: '🍳',
    equipment: '🔧', hr: '👥', employees: '👤', attendance: '⏰',
    payroll: '💰', payslips: '📃', memos: '📝', incidents: '⚠️',
    meals: '🍽️', pos: '💳', orders: '🛵', reports: '📈',
  };
  return <span className="nav-icon">{icons[name] ?? '📌'}</span>;
};

// ─── Main App ───
export default function App() {
  const [page, setPage] = useState('inv-dashboard');

  type PageInfo = { section: string; label: string; icon: string };
  const pages: Record<string, PageInfo> = {
    'inv-dashboard': { section: 'inventory', label: 'Dashboard', icon: 'dashboard' },
    'inv-items': { section: 'inventory', label: 'Items', icon: 'items' },
    'inv-batches': { section: 'inventory', label: 'Batches', icon: 'batches' },
    'inv-waste': { section: 'inventory', label: 'Waste', icon: 'waste' },
    'inv-suppliers': { section: 'inventory', label: 'Suppliers', icon: 'suppliers' },
    'hr-dashboard': { section: 'hr', label: 'Dashboard', icon: 'dashboard' },
    'hr-employees': { section: 'hr', label: 'Employees', icon: 'employees' },
    'hr-attendance': { section: 'hr', label: 'Attendance', icon: 'attendance' },
    'hr-payroll': { section: 'hr', label: 'Payroll', icon: 'payroll' },
    'hr-payslips': { section: 'hr', label: 'Payslips', icon: 'payslips' },
    'hr-memos': { section: 'hr', label: 'Memos', icon: 'memos' },
    'hr-incidents': { section: 'hr', label: 'Incidents', icon: 'incidents' },
  };

  const renderPage = () => {
    switch (page) {
      case 'inv-dashboard': return <InventoryDashboardPage />;
      case 'inv-items': return <InventoryItemsPage />;
      case 'inv-batches': return <BatchesPage />;
      case 'inv-waste': return <WastePage />;
      case 'inv-suppliers': return <SuppliersPage />;
      case 'hr-dashboard': return <HRDashboardPage />;
      case 'hr-employees': return <EmployeesPage />;
      case 'hr-attendance': return <AttendancePage />;
      case 'hr-payroll': return <PayrollPage />;
      case 'hr-payslips': return <PayslipsPage />;
      case 'hr-memos': return <MemosPage />;
      case 'hr-incidents': return <IncidentsPage />;
      default: return <InventoryDashboardPage />;
    }
  };

  const current = pages[page];

  return (
    <div className="app-layout">
      <aside className="sidebar">
        <div className="sidebar-header">
          <h1>RestoSystem</h1>
          <p>Management Suite</p>
        </div>
        <nav className="sidebar-nav">
          <div className="nav-section-title">Inventory</div>
          {(['inv-dashboard','inv-items','inv-batches','inv-waste','inv-suppliers'] as const).map(k => (
            <button key={k} className={`nav-item ${page === k ? 'active' : ''}`}
              onClick={() => setPage(k)}>
              <Icon name={pages[k].icon} /> {pages[k].label}
            </button>
          ))}
          <div className="nav-section-title" style={{ marginTop: '1rem' }}>Human Resources</div>
          {(['hr-dashboard','hr-employees','hr-attendance','hr-payroll','hr-payslips','hr-memos','hr-incidents'] as const).map(k => (
            <button key={k} className={`nav-item ${page === k ? 'active' : ''}`}
              onClick={() => setPage(k)}>
              <Icon name={pages[k].icon} /> {pages[k].label}
            </button>
          ))}
        </nav>
      </aside>

      <main className="main-content">
        {current && (
          <div className="page-header">
            <h2>{current.label}</h2>
            <p>RestoSystem {current.section === 'inventory' ? 'Inventory' : 'HR'} module</p>
          </div>
        )}
        {renderPage()}
      </main>
    </div>
  );
}

// ══════════════════════════════════════════
// COMPONENT HELPERS
// ══════════════════════════════════════════

/** Safe stat card - won't crash if data is partial */
function StatCard({ label, value, cls }: { label: string; value: React.ReactNode; cls: string }) {
  return (
    <div className="stat-card">
      <div className="stat-label">{label}</div>
      <div className={`stat-value ${cls}`}>{value}</div>
    </div>
  );
}

/** Employee status badge from enum integer */
function StatusBadge({ status }: { status: number | null | undefined }) {
  const s = status ?? -1;
  const label = STATUS_LABELS[s] ?? 'Unknown';
  const cls = STATUS_BADGE[s] ?? 'badge-danger';
  return <span className={`badge ${cls}`}>{label}</span>;
}

/** Empty state placeholder */
function EmptyState({ message }: { message: string }) {
  return (
    <tr><td colSpan={42}>
      <div className="empty-state"><p>{message}</p></div>
    </td></tr>
  );
}

/** Error banner */
function ErrorBanner({ message }: { message: string | null }) {
  if (!message) return null;
  return <div className="error-banner">⚠️ {message}</div>;
}

// ══════════════════════════════════════════
// INVENTORY DASHBOARD
// ══════════════════════════════════════════
function InventoryDashboardPage() {
  const [data, setData] = useState<InventoryDashboard | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api<InventoryDashboard>('/inventory/dashboard').then(setData).catch(e => setError(e.message));
  }, []);

  return (
    <div>
      <ErrorBanner message={error} />
      <div className="stats-grid">
        <StatCard label="Total Items" value={fmt(data?.totalItems)} cls="info" />
        <StatCard label="Low Stock Alerts" value={fmt(data?.lowStockItems)} cls="danger" />
        <StatCard label="Near-Expiry Batches" value={fmt(data?.nearExpiryBatches)} cls="warning" />
        <StatCard label="Waste Cost (30d)" value={fmtMoney(data?.totalWasteLast30Days)} cls="danger" />
      </div>
    </div>
  );
}

// ══════════════════════════════════════════
// HR DASHBOARD
// ══════════════════════════════════════════
function HRDashboardPage() {
  const [data, setData] = useState<HRDashboard | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api<HRDashboard>('/hr/dashboard').then(setData).catch(e => setError(e.message));
  }, []);

  return (
    <div>
      <ErrorBanner message={error} />
      <div className="stats-grid">
        <StatCard label="Total Employees" value={fmt(data?.totalEmployees)} cls="info" />
        <StatCard label="Active Staff" value={fmt(data?.activeEmployees)} cls="success" />
        <StatCard label="Pending Regularization" value={fmt(data?.pendingRegularization)} cls="warning" />
        <StatCard label="Today's Shifts" value={fmt(data?.todayShifts)} cls="info" />
        <StatCard label="Open Incidents" value={fmt(data?.openIncidents)} cls={data?.openIncidents ? 'danger' : 'success'} />
      </div>
    </div>
  );
}

// ══════════════════════════════════════════
// INVENTORY ITEMS
// ══════════════════════════════════════════
type Item = { id: number; name: string; category: string; currentStock: number; minStockLevel: number; unitOfMeasure: string; isPerishable: boolean; isActive: boolean; description?: string; barcode?: string; batches?: any[] };

function InventoryItemsPage() {
  const [items, setItems] = useState<Item[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ name: '', category: '', unitOfMeasure: '', minStockLevel: 10, reorderPoint: 20, description: '', isPerishable: false });
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState('');

  const load = useCallback(() => {
    api<Item[]>('/inventory/items').then(setItems).catch(e => setError(e.message));
  }, []);
  useEffect(load, [load]);

  const create = async () => {
    try {
      await api('/inventory/items', {
        method: 'POST',
        body: JSON.stringify({ ...form, branchId: 1 })
      });
      setShowForm(false);
      setForm({ name: '', category: '', unitOfMeasure: '', minStockLevel: 10, reorderPoint: 20, description: '', isPerishable: false });
      setError(null);
      load();
    } catch (e: any) {
      setError(e.message);
    }
  };

  const filtered = filter
    ? items.filter(i => i.name.toLowerCase().includes(filter.toLowerCase()) || i.category.toLowerCase().includes(filter.toLowerCase()))
    : items;

  return (
    <div>
      <ErrorBanner message={error} />
      <div className="toolbar">
        <input className="form-control" placeholder="Search items..." value={filter} onChange={e => setFilter(e.target.value)} />
        <div className="toolbar-spacer" />
        <button className="btn btn-primary" onClick={() => setShowForm(true)}>+ New Item</button>
      </div>

      <div className="data-table">
        <table>
          <thead><tr><th>Name</th><th>Category</th><th>Stock</th><th>Min</th><th>UoM</th><th>Status</th></tr></thead>
          <tbody>
            {filtered.length === 0 && <EmptyState message="No inventory items yet. Add your first item." />}
            {filtered.map(i => (
              <tr key={i.id}>
                <td><strong>{i.name}</strong>{i.description && <><br /><small style={{color:'var(--text-muted)'}}>{i.description}</small></>}</td>
                <td><span className="badge badge-info">{i.category}</span></td>
                <td style={{ color: i.currentStock <= i.minStockLevel ? 'var(--danger)' : 'inherit', fontWeight: 600 }}>
                  {i.currentStock}
                </td>
                <td>{i.minStockLevel}</td>
                <td>{i.unitOfMeasure}</td>
                <td>
                  {i.isPerishable && <span className="badge badge-warning" style={{marginRight:4}}>Perishable</span>}
                  {!i.isActive && <span className="badge badge-danger">Inactive</span>}
                  {i.isActive && !i.isPerishable && <span className="badge badge-success">Active</span>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showForm && (
        <div className="modal-overlay" onClick={() => setShowForm(false)}>
          <div className="modal" onClick={e => e.stopPropagation()}>
            <h3>New Inventory Item</h3>
            <div className="form-group">
              <label>Name</label>
              <input className="form-control" value={form.name} onChange={e => setForm(f => ({...f, name: e.target.value}))} />
            </div>
            <div className="form-group">
              <label>Category</label>
              <select className="form-control" value={form.category} onChange={e => setForm(f => ({...f, category: e.target.value}))}>
                <option value="">Select...</option>
                <option value="Produce">Produce</option>
                <option value="Meat">Meat</option>
                <option value="Dairy">Dairy</option>
                <option value="Dry Goods">Dry Goods</option>
                <option value="Packaging">Packaging</option>
                <option value="Beverages">Beverages</option>
                <option value="Condiments">Condiments</option>
              </select>
            </div>
            <div style={{display:'grid', gridTemplateColumns:'1fr 1fr', gap:'0.75rem'}}>
              <div className="form-group">
                <label>Unit of Measure</label>
                <input className="form-control" placeholder="kg, pcs, liters" value={form.unitOfMeasure} onChange={e => setForm(f => ({...f, unitOfMeasure: e.target.value}))} />
              </div>
              <div className="form-group">
                <label>Min Stock Level</label>
                <input className="form-control" type="number" value={form.minStockLevel} onChange={e => setForm(f => ({...f, minStockLevel: +e.target.value}))} />
              </div>
            </div>
            <div className="form-group">
              <label style={{display:'flex', alignItems:'center', gap:'0.5rem'}}>
                <input type="checkbox" checked={form.isPerishable} onChange={e => setForm(f => ({...f, isPerishable: e.target.checked}))} />
                Perishable item
              </label>
            </div>
            <div className="modal-actions">
              <button className="btn btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
              <button className="btn btn-primary" onClick={create}>Create Item</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ══════════════════════════════════════════
// BATCHES
// ══════════════════════════════════════════
type Batch = { id: number; inventoryItem?: { name: string }; batchNumber?: string; quantity: number; unitCost: number; expiryDate?: string; storageLocation?: { area: string }; supplier?: { name: string }; isExpired?: boolean };

function BatchesPage() {
  const [batches, setBatches] = useState<Batch[]>([]);
  const [expiringOnly, setExpiringOnly] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api<Batch[]>(`/inventory/batches${expiringOnly ? '?expiringSoon=true' : ''}`)
      .then(setBatches).catch(e => setError(e.message));
  }, [expiringOnly]);

  return (
    <div>
      <ErrorBanner message={error} />
      <div className="toolbar">
        <label style={{display:'flex',alignItems:'center',gap:6,fontSize:'0.85rem',cursor:'pointer'}}>
          <input type="checkbox" checked={expiringOnly} onChange={e => setExpiringOnly(e.target.checked)} />
          Expiring within 7 days only
        </label>
      </div>

      <div className="data-table">
        <table>
          <thead><tr><th>Item</th><th>Batch</th><th>Qty</th><th>Unit Cost</th><th>Location</th><th>Expiry</th><th>Supplier</th><th>Status</th></tr></thead>
          <tbody>
            {batches.length === 0 && <EmptyState message="No batches recorded." />}
            {batches.map(b => {
              const expiryDate = safeDate(b.expiryDate);
              const now = Date.now();
              const expiring = expiryDate && expiryDate.getTime() <= now + 7 * 86400000;
              return (
                <tr key={b.id}>
                  <td><strong>{b.inventoryItem?.name ?? '—'}</strong></td>
                  <td style={{fontSize:'0.8rem',color:'var(--text-muted)'}}>{b.batchNumber ?? '—'}</td>
                  <td style={{fontWeight:600}}>{b.quantity}</td>
                  <td>{fmtMoney(b.unitCost)}</td>
                  <td>{b.storageLocation?.area ?? '—'}</td>
                  <td>{fmtDate(b.expiryDate)}</td>
                  <td>{b.supplier?.name ?? '—'}</td>
                  <td>
                    {b.isExpired ? <span className="badge badge-danger">Expired</span>
                      : expiring ? <span className="badge badge-warning">Expiring</span>
                      : <span className="badge badge-success">Good</span>}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ══════════════════════════════════════════
// WASTE
// ══════════════════════════════════════════
function WastePage() {
  const [records, setRecords] = useState<any[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api<any[]>('/inventory/waste').then(setRecords).catch(e => setError(e.message));
  }, []);

  return (
    <div>
      <ErrorBanner message={error} />
      <div className="data-table">
        <table>
          <thead><tr><th>Item</th><th>Qty</th><th>Reason</th><th>Est. Cost</th><th>Date</th></tr></thead>
          <tbody>
            {records.length === 0 && <EmptyState message="No waste records." />}
            {records.map(r => (
              <tr key={r.id}>
                <td><strong>{r.inventoryItem?.name ?? '—'}</strong></td>
                <td>{fmt(r.quantity)}</td>
                <td><span className="badge badge-warning">{r.reason ?? '—'}</span></td>
                <td>{fmtMoney(r.estimatedCost)}</td>
                <td>{fmtDate(r.wasteDate)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ══════════════════════════════════════════
// SUPPLIERS
// ══════════════════════════════════════════
function SuppliersPage() {
  const [suppliers, setSuppliers] = useState<any[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api<any[]>('/inventory/suppliers').then(setSuppliers).catch(e => setError(e.message));
  }, []);

  return (
    <div>
      <ErrorBanner message={error} />
      <div className="data-table">
        <table>
          <thead><tr><th>Name</th><th>Contact Person</th><th>Contact</th><th>Email</th><th>TIN</th></tr></thead>
          <tbody>
            {suppliers.length === 0 && <EmptyState message="No suppliers yet." />}
            {suppliers.map(s => (
              <tr key={s.id}>
                <td><strong>{s.name}</strong></td>
                <td>{s.contactPerson ?? '—'}</td>
                <td>{s.contactNumber}</td>
                <td>{s.email}</td>
                <td style={{fontSize:'0.8rem',color:'var(--text-muted)'}}>{s.tinNumber}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ══════════════════════════════════════════
// EMPLOYEES
// ══════════════════════════════════════════
function EmployeesPage() {
  const [employees, setEmployees] = useState<any[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState('');

  useEffect(() => {
    api<any[]>('/hr/employees').then(setEmployees).catch(e => setError(e.message));
  }, []);

  const filtered = filter
    ? employees.filter(e =>
        (e.lastName ?? '').toLowerCase().includes(filter.toLowerCase()) ||
        (e.firstName ?? '').toLowerCase().includes(filter.toLowerCase()) ||
        (e.employeeCode ?? '').toLowerCase().includes(filter.toLowerCase()))
    : employees;

  return (
    <div>
      <ErrorBanner message={error} />
      <div className="toolbar">
        <input className="form-control" placeholder="Search employees..." value={filter} onChange={e => setFilter(e.target.value)} />
        <div className="toolbar-spacer" />
      </div>

      <div className="data-table">
        <table>
          <thead><tr><th>Code</th><th>Name</th><th>Position</th><th>Branch</th><th>Status</th><th>Hired</th></tr></thead>
          <tbody>
            {filtered.length === 0 && <EmptyState message="No employees yet." />}
            {filtered.map((e: any) => (
              <tr key={e.id}>
                <td style={{fontSize:'0.8rem',color:'var(--text-muted)'}}>{e.employeeCode}</td>
                <td><strong>{e.lastName}, {e.firstName}</strong></td>
                <td>{e.position}{e.role !== undefined && <><br /><small style={{color:'var(--text-muted)'}}>{ROLE_LABELS[e.role] ?? ''}</small></>}</td>
                <td>{e.branchName ?? e.branchId ?? '—'}</td>
                <td><StatusBadge status={e.status} /></td>
                <td>{fmtDate(e.hireDate)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ══════════════════════════════════════════
// ATTENDANCE
// ══════════════════════════════════════════
function AttendancePage() {
  const [logs, setLogs] = useState<any[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api<any[]>('/hr/attendance').then(setLogs).catch(e => setError(e.message));
  }, []);

  return (
    <div>
      <ErrorBanner message={error} />
      <div className="toolbar">
        <input className="form-control" type="date" />
        <div className="toolbar-spacer" />
      </div>

      <div className="data-table">
        <table>
          <thead><tr><th>Employee</th><th>Clock In</th><th>Clock Out</th><th>Hours</th><th>Verified</th></tr></thead>
          <tbody>
            {logs.length === 0 && <EmptyState message="No attendance records yet." />}
            {logs.map(l => {
              const clockIn = safeDate(l.clockIn);
              const clockOut = safeDate(l.clockOut);
              const hours = clockIn && clockOut
                ? Math.round((clockOut.getTime() - clockIn.getTime()) / 3600000 * 10) / 10
                : null;
              return (
                <tr key={l.id}>
                  <td><strong>{l.employee?.lastName}, {l.employee?.firstName}</strong></td>
                  <td>{fmtDateTime(l.clockIn)}</td>
                  <td>{clockOut ? fmtDateTime(l.clockOut) : <span className="badge badge-warning">In Progress</span>}</td>
                  <td>{hours ? `${hours}h` : '—'}</td>
                  <td>{l.isFaceVerified ? <span className="badge badge-success">Face</span> : <span className="badge badge-info">Manual</span>}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ══════════════════════════════════════════
// PAYROLL
// ══════════════════════════════════════════
function PayrollPage() {
  const [payrolls, setPayrolls] = useState<any[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api<any[]>('/hr/payroll').then(setPayrolls).catch(e => setError(e.message));
  }, []);

  return (
    <div>
      <ErrorBanner message={error} />
      <div className="toolbar">
        <span className="badge badge-info">Sun–Sat cycle, paid Tuesday</span>
      </div>

      <div className="data-table">
        <table>
          <thead><tr><th>Period</th><th>Employee</th><th>Gross</th><th>Deductions</th><th>Net Pay</th><th>Hours</th><th>Shifts</th></tr></thead>
          <tbody>
            {payrolls.length === 0 && <EmptyState message="No payroll entries yet. Generate a payroll period to begin." />}
            {payrolls.map(p => (
              <tr key={p.id}>
                <td style={{fontSize:'0.8rem'}}>
                  {p.payrollPeriod ? `${fmtDate(p.payrollPeriod.weekStart)} – ${fmtDate(p.payrollPeriod.weekEnd)}` : '—'}
                </td>
                <td><strong>{p.employee?.lastName}, {p.employee?.firstName}</strong></td>
                <td>{fmtMoney(p.grossPay)}</td>
                <td>{fmtMoney(p.totalDeductions)}</td>
                <td style={{fontWeight:700,color:'var(--success)'}}>{fmtMoney(p.netPay)}</td>
                <td>{fmtHours(p.totalHoursWorked)}</td>
                <td>{fmt(p.totalShifts)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ══════════════════════════════════════════
// PAYSLIPS
// ══════════════════════════════════════════
function PayslipsPage() {
  const [payslips, setPayslips] = useState<any[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api<any[]>('/hr/payslips').then(setPayslips).catch(e => setError(e.message));
  }, []);

  return (
    <div>
      <ErrorBanner message={error} />
      <div className="data-table">
        <table>
          <thead><tr><th>Employee</th><th>Period</th><th>Generated</th><th>Delivered</th></tr></thead>
          <tbody>
            {payslips.length === 0 && <EmptyState message="No payslips generated yet." />}
            {payslips.map(p => (
              <tr key={p.id}>
                <td><strong>{p.employee?.lastName}, {p.employee?.firstName}</strong></td>
                <td>{p.payroll?.payrollPeriod ? `${fmtDate(p.payroll.payrollPeriod.weekStart)} – ${fmtDate(p.payroll.payrollPeriod.weekEnd)}` : '—'}</td>
                <td>{fmtDate(p.generatedDate)}</td>
                <td>{p.isDelivered ? <span className="badge badge-success">Sent</span> : <span className="badge badge-warning">Pending</span>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ══════════════════════════════════════════
// MEMOS
// ══════════════════════════════════════════
function MemosPage() {
  const [memos, setMemos] = useState<any[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api<any[]>('/hr/memos').then(setMemos).catch(e => setError(e.message));
  }, []);

  return (
    <div>
      <ErrorBanner message={error} />
      <div className="data-table">
        <table>
          <thead><tr><th>Subject</th><th>Type</th><th>Date</th><th>Acknowledged</th></tr></thead>
          <tbody>
            {memos.length === 0 && <EmptyState message="No memos yet." />}
            {memos.map(m => (
              <tr key={m.id}>
                <td><strong>{m.subject}</strong></td>
                <td><span className="badge badge-info">{m.type}</span></td>
                <td>{fmtDate(m.issueDate)}</td>
                <td>{m.isAcknowledged ? <span className="badge badge-success">Yes</span> : <span className="badge badge-warning">No</span>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ══════════════════════════════════════════
// INCIDENTS
// ══════════════════════════════════════════
function IncidentsPage() {
  const [incidents, setIncidents] = useState<any[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api<any[]>('/hr/incidents').then(setIncidents).catch(e => setError(e.message));
  }, []);

  return (
    <div>
      <ErrorBanner message={error} />
      <div className="data-table">
        <table>
          <thead><tr><th>Title</th><th>Employee</th><th>Severity</th><th>Status</th><th>Date</th></tr></thead>
          <tbody>
            {incidents.length === 0 && <EmptyState message="No incident reports." />}
            {incidents.map(i => (
              <tr key={i.id}>
                <td><strong>{i.title}</strong></td>
                <td>{i.employee?.lastName}, {i.employee?.firstName}</td>
                <td>
                  {i.severity === 'Critical' || i.severity === 'High' ? <span className="badge badge-danger">{i.severity}</span>
                    : i.severity === 'Medium' ? <span className="badge badge-warning">{i.severity}</span>
                    : <span className="badge badge-info">{i.severity ?? '—'}</span>}
                </td>
                <td>{i.status}</td>
                <td>{fmtDate(i.incidentDate)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
