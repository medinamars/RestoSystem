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
      {/* Sidebar */}
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

      {/* Main */}
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
// INVENTORY DASHBOARD
// ══════════════════════════════════════════
function InventoryDashboardPage() {
  const [data, setData] = useState<InventoryDashboard | null>(null);

  useEffect(() => {
    api<InventoryDashboard>('/inventory/dashboard').then(setData).catch(() => {});
  }, []);

  const stats = [
    { label: 'Total Items', value: data?.totalItems ?? '—', cls: 'info' },
    { label: 'Low Stock Alerts', value: data?.lowStockItems ?? '—', cls: 'danger' },
    { label: 'Near-Expiry Batches', value: data?.nearExpiryBatches ?? '—', cls: 'warning' },
    { label: 'Waste Cost (30d)', value: data ? `₱${data.totalWasteLast30Days.toLocaleString()}` : '—', cls: 'danger' },
  ];

  return (
    <div>
      <div className="stats-grid">
        {stats.map(s => (
          <div key={s.label} className="stat-card">
            <div className="stat-label">{s.label}</div>
            <div className={`stat-value ${s.cls}`}>{s.value}</div>
          </div>
        ))}
      </div>

      <div className="data-table">
        <table>
          <thead><tr><th>Module</th><th>Quick Actions</th></tr></thead>
          <tbody>
            <tr><td>Items</td><td><span className="badge badge-info">Track raw materials</span></td></tr>
            <tr><td>Batches</td><td><span className="badge badge-warning">{data?.nearExpiryBatches ?? 0} expiring soon</span></td></tr>
            <tr><td>Waste</td><td><span className="badge badge-danger">Log & monitor waste</span></td></tr>
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ══════════════════════════════════════════
// HR DASHBOARD
// ══════════════════════════════════════════
function HRDashboardPage() {
  const [data, setData] = useState<HRDashboard | null>(null);

  useEffect(() => {
    api<HRDashboard>('/hr/dashboard').then(setData).catch(() => {});
  }, []);

  const stats = [
    { label: 'Total Employees', value: data?.totalEmployees ?? '—', cls: 'info' },
    { label: 'Active Staff', value: data?.activeEmployees ?? '—', cls: 'success' },
    { label: 'Pending Regularization', value: data?.pendingRegularization ?? '—', cls: 'warning' },
    { label: "Today's Shifts", value: data?.todayShifts ?? '—', cls: 'info' },
    { label: 'Open Incidents', value: data?.openIncidents ?? '—', cls: data?.openIncidents ? 'danger' : 'success' },
  ];

  return (
    <div>
      <div className="stats-grid">
        {stats.map(s => (
          <div key={s.label} className="stat-card">
            <div className="stat-label">{s.label}</div>
            <div className={`stat-value ${s.cls}`}>{s.value}</div>
          </div>
        ))}
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

  const load = useCallback(() => { api<Item[]>('/inventory/items').then(setItems).catch(() => {}); }, []);
  useEffect(load, [load]);

  const create = async () => {
    await api('/inventory/items', { method: 'POST', body: JSON.stringify({ ...form, branchId: 1, currentStock: 0, isActive: true }) });
    setShowForm(false); setForm({ name: '', category: '', unitOfMeasure: '', minStockLevel: 10, reorderPoint: 20, description: '', isPerishable: false }); load();
  };

  return (
    <div>
      <div className="toolbar">
        <input className="form-control" placeholder="Search items..." />
        <div className="toolbar-spacer" />
        <button className="btn btn-primary" onClick={() => setShowForm(true)}>+ New Item</button>
      </div>

      <div className="data-table">
        <table>
          <thead><tr><th>Name</th><th>Category</th><th>Stock</th><th>Min</th><th>UoM</th><th>Status</th></tr></thead>
          <tbody>
            {items.length === 0 && <tr><td colSpan={6}><div className="empty-state"><p>No inventory items yet. Add your first item.</p></div></td></tr>}
            {items.map(i => (
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
type Batch = { id: number; inventoryItem: { name: string }; batchNumber?: string; quantity: number; unitCost: number; expiryDate?: string; storageLocation?: { area: string }; supplier?: { name: string }; isExpired?: boolean };

function BatchesPage() {
  const [batches, setBatches] = useState<Batch[]>([]);
  const [expiringOnly, setExpiringOnly] = useState(false);

  useEffect(() => {
    api<Batch[]>(`/inventory/batches${expiringOnly ? '?expiringSoon=true' : ''}`)
      .then(setBatches).catch(() => {});
  }, [expiringOnly]);

  return (
    <div>
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
            {batches.length === 0 && <tr><td colSpan={8}><div className="empty-state"><p>No batches recorded.</p></div></td></tr>}
            {batches.map(b => {
              const expiring = b.expiryDate && new Date(b.expiryDate) <= new Date(Date.now() + 7*86400000);
              return (
                <tr key={b.id}>
                  <td><strong>{b.inventoryItem?.name}</strong></td>
                  <td style={{fontSize:'0.8rem',color:'var(--text-muted)'}}>{b.batchNumber ?? '—'}</td>
                  <td style={{fontWeight:600}}>{b.quantity}</td>
                  <td>₱{b.unitCost.toFixed(2)}</td>
                  <td>{b.storageLocation?.area ?? '—'}</td>
                  <td>{b.expiryDate ? new Date(b.expiryDate).toLocaleDateString() : '—'}</td>
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

  useEffect(() => {
    api<any[]>('/inventory/waste').then(setRecords).catch(() => {});
  }, []);

  return (
    <div>
      <div className="data-table">
        <table>
          <thead><tr><th>Item</th><th>Qty</th><th>Reason</th><th>Est. Cost</th><th>Date</th></tr></thead>
          <tbody>
            {records.length === 0 && <tr><td colSpan={5}><div className="empty-state"><p>No waste records.</p></div></td></tr>}
            {records.map(r => (
              <tr key={r.id}>
                <td><strong>{r.inventoryItem?.name}</strong></td>
                <td>{r.quantity}</td>
                <td><span className="badge badge-warning">{r.reason}</span></td>
                <td>₱{r.estimatedCost.toFixed(2)}</td>
                <td>{new Date(r.wasteDate).toLocaleDateString()}</td>
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

  useEffect(() => {
    api<any[]>('/inventory/suppliers').then(setSuppliers).catch(() => {});
  }, []);

  return (
    <div>
      <div className="data-table">
        <table>
          <thead><tr><th>Name</th><th>Contact Person</th><th>Contact</th><th>Email</th><th>TIN</th></tr></thead>
          <tbody>
            {suppliers.length === 0 && <tr><td colSpan={5}><div className="empty-state"><p>No suppliers yet.</p></div></td></tr>}
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

  useEffect(() => {
    api<any[]>('/hr/employees').then(setEmployees).catch(() => {});
  }, []);

  return (
    <div>
      <div className="toolbar">
        <input className="form-control" placeholder="Search employees..." />
        <div className="toolbar-spacer" />
        <button className="btn btn-primary">+ New Employee</button>
      </div>

      <div className="data-table">
        <table>
          <thead><tr><th>Code</th><th>Name</th><th>Position</th><th>Branch</th><th>Status</th><th>Hired</th></tr></thead>
          <tbody>
            {employees.length === 0 && <tr><td colSpan={6}><div className="empty-state"><p>No employees yet.</p></div></td></tr>}
            {employees.map(e => (
              <tr key={e.id}>
                <td style={{fontSize:'0.8rem',color:'var(--text-muted)'}}>{e.employeeCode}</td>
                <td><strong>{e.lastName}, {e.firstName}</strong></td>
                <td>{e.position}</td>
                <td>{e.branch?.name ?? '—'}</td>
                <td>
                  {e.status === 'Active' || e.status === 'Regularized' ? <span className="badge badge-success">{e.status}</span>
                    : e.status === 'Probationary' ? <span className="badge badge-warning">{e.status}</span>
                    : <span className="badge badge-danger">{e.status}</span>}
                </td>
                <td>{new Date(e.hireDate).toLocaleDateString()}</td>
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

  useEffect(() => {
    api<any[]>('/hr/attendance').then(setLogs).catch(() => {});
  }, []);

  return (
    <div>
      <div className="toolbar">
        <input className="form-control" type="date" />
        <div className="toolbar-spacer" />
      </div>

      <div className="data-table">
        <table>
          <thead><tr><th>Employee</th><th>Clock In</th><th>Clock Out</th><th>Hours</th><th>Verified</th></tr></thead>
          <tbody>
            {logs.length === 0 && <tr><td colSpan={5}><div className="empty-state"><p>No attendance records yet.</p></div></td></tr>}
            {logs.map(l => {
              const hours = l.clockOut ? Math.round((new Date(l.clockOut).getTime() - new Date(l.clockIn).getTime()) / 3600000 * 10) / 10 : null;
              return (
                <tr key={l.id}>
                  <td><strong>{l.employee?.lastName}, {l.employee?.firstName}</strong></td>
                  <td>{new Date(l.clockIn).toLocaleString()}</td>
                  <td>{l.clockOut ? new Date(l.clockOut).toLocaleString() : <span className="badge badge-warning">In Progress</span>}</td>
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

  useEffect(() => {
    api<any[]>('/hr/payroll').then(setPayrolls).catch(() => {});
  }, []);

  return (
    <div>
      <div className="toolbar">
        <span className="badge badge-info">Sun–Sat cycle, paid Tuesday</span>
      </div>

      <div className="data-table">
        <table>
          <thead><tr><th>Period</th><th>Employee</th><th>Gross</th><th>Deductions</th><th>Net Pay</th><th>Hours</th><th>Shifts</th></tr></thead>
          <tbody>
            {payrolls.length === 0 && <tr><td colSpan={7}><div className="empty-state"><p>No payroll entries yet. Generate a payroll period to begin.</p></div></td></tr>}
            {payrolls.map(p => (
              <tr key={p.id}>
                <td style={{fontSize:'0.8rem'}}>
                  {p.payrollPeriod ? `${new Date(p.payrollPeriod.weekStart).toLocaleDateString()} – ${new Date(p.payrollPeriod.weekEnd).toLocaleDateString()}` : '—'}
                </td>
                <td><strong>{p.employee?.lastName}, {p.employee?.firstName}</strong></td>
                <td>₱{p.grossPay.toFixed(2)}</td>
                <td>₱{p.totalDeductions.toFixed(2)}</td>
                <td style={{fontWeight:700,color:'var(--success)'}}>₱{p.netPay.toFixed(2)}</td>
                <td>{p.totalHoursWorked}h</td>
                <td>{p.totalShifts}</td>
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

  useEffect(() => {
    api<any[]>('/hr/payslips').then(setPayslips).catch(() => {});
  }, []);

  return (
    <div>
      <div className="data-table">
        <table>
          <thead><tr><th>Employee</th><th>Period</th><th>Generated</th><th>Delivered</th></tr></thead>
          <tbody>
            {payslips.length === 0 && <tr><td colSpan={4}><div className="empty-state"><p>No payslips generated yet.</p></div></td></tr>}
            {payslips.map(p => (
              <tr key={p.id}>
                <td><strong>{p.employee?.lastName}, {p.employee?.firstName}</strong></td>
                <td>{p.payroll?.payrollPeriod ? `${new Date(p.payroll.payrollPeriod.weekStart).toLocaleDateString()} – ${new Date(p.payroll.payrollPeriod.weekEnd).toLocaleDateString()}` : '—'}</td>
                <td>{new Date(p.generatedDate).toLocaleDateString()}</td>
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

  useEffect(() => {
    api<any[]>('/hr/memos').then(setMemos).catch(() => {});
  }, []);

  return (
    <div>
      <div className="data-table">
        <table>
          <thead><tr><th>Subject</th><th>Type</th><th>Date</th><th>Acknowledged</th></tr></thead>
          <tbody>
            {memos.length === 0 && <tr><td colSpan={4}><div className="empty-state"><p>No memos yet.</p></div></td></tr>}
            {memos.map(m => (
              <tr key={m.id}>
                <td><strong>{m.subject}</strong></td>
                <td><span className="badge badge-info">{m.type}</span></td>
                <td>{new Date(m.issueDate).toLocaleDateString()}</td>
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

  useEffect(() => {
    api<any[]>('/hr/incidents').then(setIncidents).catch(() => {});
  }, []);

  return (
    <div>
      <div className="data-table">
        <table>
          <thead><tr><th>Title</th><th>Employee</th><th>Severity</th><th>Status</th><th>Date</th></tr></thead>
          <tbody>
            {incidents.length === 0 && <tr><td colSpan={5}><div className="empty-state"><p>No incident reports.</p></div></td></tr>}
            {incidents.map(i => (
              <tr key={i.id}>
                <td><strong>{i.title}</strong></td>
                <td>{i.employee?.lastName}, {i.employee?.firstName}</td>
                <td>
                  {i.severity === 'Critical' || i.severity === 'High' ? <span className="badge badge-danger">{i.severity}</span>
                    : i.severity === 'Medium' ? <span className="badge badge-warning">{i.severity}</span>
                    : <span className="badge badge-info">{i.severity}</span>}
                </td>
                <td>{i.status}</td>
                <td>{new Date(i.incidentDate).toLocaleDateString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
