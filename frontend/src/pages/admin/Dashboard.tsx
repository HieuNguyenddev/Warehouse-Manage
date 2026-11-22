import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import {
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Grid,
  Stack,
  Typography,
  Alert as MuiAlert,
} from '@mui/material'
import DashboardIcon from '@mui/icons-material/Dashboard'
import Inventory2Icon from '@mui/icons-material/Inventory2'
import WarehouseIcon from '@mui/icons-material/Warehouse'
import WarningAmberIcon from '@mui/icons-material/WarningAmber'
import InputIcon from '@mui/icons-material/Input'
import OutputIcon from '@mui/icons-material/Output'
import RefreshIcon from '@mui/icons-material/Refresh'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import {
  categoriesApi,
  customersApi,
  exportsApi,
  importsApi,
  productsApi,
  stocktakesApi,
  stocksApi,
  suppliersApi,
  transactionsApi,
  warehousesApi,
} from '../../api/endpoints'
import { ApiError } from '../../api/client'
import { PageHeader } from '../../components/ui'
import type { StockTransactionDto, WarehouseStockDto } from '../../types/api'

const COLORS = ['#1565c0', '#00897b', '#ed6c02', '#7b1fa2', '#c62828', '#2e7d32', '#455a64', '#00838f']

type Stats = {
  products: number
  warehouses: number
  customers: number
  suppliers: number
  categories: number
  lowStock: number
  importDraft: number
  exportDraft: number
  importConfirmed: number
  exportConfirmed: number
  importCancelled: number
  exportCancelled: number
  stocktakeDraft: number
  stocktakeConfirmed: number
  transactions: number
}

const emptyStats: Stats = {
  products: 0,
  warehouses: 0,
  customers: 0,
  suppliers: 0,
  categories: 0,
  lowStock: 0,
  importDraft: 0,
  exportDraft: 0,
  importConfirmed: 0,
  exportConfirmed: 0,
  importCancelled: 0,
  exportCancelled: 0,
  stocktakeDraft: 0,
  stocktakeConfirmed: 0,
  transactions: 0,
}

function ChartCard({ title, children, height = 300 }: { title: string; children: ReactNode; height?: number }) {
  return (
    <Card sx={{ height: '100%', borderRadius: 3 }}>
      <CardContent>
        <Typography variant="h6" sx={{ fontWeight: 700, mb: 2 }}>
          {title}
        </Typography>
        <Box sx={{ width: '100%', height }}>
          <ResponsiveContainer width="100%" height="100%">
            {children as React.ReactElement}
          </ResponsiveContainer>
        </Box>
      </CardContent>
    </Card>
  )
}

export function AdminDashboard() {
  const [stats, setStats] = useState<Stats>(emptyStats)
  const [lowStocks, setLowStocks] = useState<WarehouseStockDto[]>([])
  const [recentTx, setRecentTx] = useState<StockTransactionDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [tick, setTick] = useState(0)

  useEffect(() => {
    void (async () => {
      setLoading(true)
      setError('')
      try {
        const [
          products,
          warehouses,
          customers,
          suppliers,
          categories,
          lowStockPage,
          importDraft,
          exportDraft,
          importConfirmed,
          exportConfirmed,
          importCancelled,
          exportCancelled,
          stocktakeDraft,
          stocktakeConfirmed,
          transactions,
          recent,
        ] = await Promise.all([
          productsApi.list({ pageNumber: 1, pageSize: 1 }),
          warehousesApi.list({ pageNumber: 1, pageSize: 1 }),
          customersApi.list({ pageNumber: 1, pageSize: 1 }),
          suppliersApi.list({ pageNumber: 1, pageSize: 1 }),
          categoriesApi.list({ pageNumber: 1, pageSize: 1 }),
          stocksApi.list({ pageNumber: 1, pageSize: 12, lowStockOnly: true }),
          importsApi.list({ pageNumber: 1, pageSize: 1, status: 'Draft' }),
          exportsApi.list({ pageNumber: 1, pageSize: 1, status: 'Draft' }),
          importsApi.list({ pageNumber: 1, pageSize: 1, status: 'Confirmed' }),
          exportsApi.list({ pageNumber: 1, pageSize: 1, status: 'Confirmed' }),
          importsApi.list({ pageNumber: 1, pageSize: 1, status: 'Cancelled' }),
          exportsApi.list({ pageNumber: 1, pageSize: 1, status: 'Cancelled' }),
          stocktakesApi.list({ pageNumber: 1, pageSize: 1, status: 'Draft' }),
          stocktakesApi.list({ pageNumber: 1, pageSize: 1, status: 'Confirmed' }),
          transactionsApi.list({ pageNumber: 1, pageSize: 1 }),
          transactionsApi.list({ pageNumber: 1, pageSize: 50 }),
        ])

        setStats({
          products: products.totalRecords,
          warehouses: warehouses.totalRecords,
          customers: customers.totalRecords,
          suppliers: suppliers.totalRecords,
          categories: categories.totalRecords,
          lowStock: lowStockPage.totalRecords,
          importDraft: importDraft.totalRecords,
          exportDraft: exportDraft.totalRecords,
          importConfirmed: importConfirmed.totalRecords,
          exportConfirmed: exportConfirmed.totalRecords,
          importCancelled: importCancelled.totalRecords,
          exportCancelled: exportCancelled.totalRecords,
          stocktakeDraft: stocktakeDraft.totalRecords,
          stocktakeConfirmed: stocktakeConfirmed.totalRecords,
          transactions: transactions.totalRecords,
        })
        setLowStocks(lowStockPage.items)
        setRecentTx(recent.items)
      } catch (e) {
        setError(e instanceof ApiError ? e.message : 'Không tải được dashboard')
      } finally {
        setLoading(false)
      }
    })()
  }, [tick])

  const masterPie = useMemo(
    () =>
      [
        { name: 'Sản phẩm', value: stats.products },
        { name: 'Kho', value: stats.warehouses },
        { name: 'Khách hàng', value: stats.customers },
        { name: 'NCC', value: stats.suppliers },
        { name: 'Danh mục', value: stats.categories },
      ].filter((x) => x.value > 0),
    [stats],
  )

  const receiptBars = useMemo(
    () => [
      { name: 'Nhập Draft', value: stats.importDraft, fill: '#ed6c02' },
      { name: 'Nhập Confirmed', value: stats.importConfirmed, fill: '#2e7d32' },
      { name: 'Nhập Cancelled', value: stats.importCancelled, fill: '#c62828' },
      { name: 'Xuất Draft', value: stats.exportDraft, fill: '#fb8c00' },
      { name: 'Xuất Confirmed', value: stats.exportConfirmed, fill: '#00897b' },
      { name: 'Xuất Cancelled', value: stats.exportCancelled, fill: '#ad1457' },
      { name: 'Kiểm kê Draft', value: stats.stocktakeDraft, fill: '#6a1b9a' },
      { name: 'Kiểm kê Confirmed', value: stats.stocktakeConfirmed, fill: '#1565c0' },
    ],
    [stats],
  )

  const lowStockChart = useMemo(
    () =>
      lowStocks.map((s) => ({
        name: s.productSku || `#${s.productId}`,
        tồn: Number(s.quantity),
        min: Number(s.minStock),
        kho: s.warehouseCode,
      })),
    [lowStocks],
  )

  const txTypeChart = useMemo(() => {
    const map = new Map<string, number>()
    recentTx.forEach((t) => map.set(t.type, (map.get(t.type) || 0) + 1))
    return Array.from(map.entries()).map(([name, value]) => ({ name, value }))
  }, [recentTx])

  const txTrend = useMemo(() => {
    const map = new Map<string, { date: string; tăng: number; giảm: number; net: number }>()
    ;[...recentTx]
      .sort((a, b) => +new Date(a.createdAt) - +new Date(b.createdAt))
      .forEach((t) => {
        const key = new Date(t.createdAt).toLocaleDateString('vi-VN')
        const row = map.get(key) || { date: key, tăng: 0, giảm: 0, net: 0 }
        if (t.quantityChange >= 0) row.tăng += t.quantityChange
        else row.giảm += Math.abs(t.quantityChange)
        row.net += t.quantityChange
        map.set(key, row)
      })
    return Array.from(map.values())
  }, [recentTx])

  const kpis = [
    { label: 'Sản phẩm', value: stats.products, icon: <Inventory2Icon />, color: '#1565c0', to: '/admin/products' },
    { label: 'Kho hàng', value: stats.warehouses, icon: <WarehouseIcon />, color: '#00897b', to: '/admin/warehouses' },
    { label: 'Tồn thấp', value: stats.lowStock, icon: <WarningAmberIcon />, color: '#ed6c02', to: '/admin/stocks?lowStockOnly=true' },
    { label: 'Nhập Draft', value: stats.importDraft, icon: <InputIcon />, color: '#6a1b9a', to: '/admin/imports' },
    { label: 'Xuất Draft', value: stats.exportDraft, icon: <OutputIcon />, color: '#c62828', to: '/admin/exports' },
  ]

  if (loading) {
    return (
      <Box className="flex min-h-[50vh] flex-col items-center justify-center gap-3">
        <CircularProgress />
        <Typography color="text.secondary">Đang tải thống kê từ API...</Typography>
      </Box>
    )
  }

  return (
    <Box>
      <PageHeader
        icon={<DashboardIcon color="primary" />}
        title="Dashboard thống kê"
        subtitle="Biểu đồ bám dữ liệu Products, Stocks, Import/Export, Stocktakes, Transactions"
        actions={
          <Stack direction="row" spacing={1}>
            <Button startIcon={<RefreshIcon />} variant="outlined" onClick={() => setTick((x) => x + 1)}>
              Làm mới
            </Button>
            <Button component={RouterLink} to="/admin/imports" startIcon={<InputIcon />}>
              Nhập kho
            </Button>
            <Button component={RouterLink} to="/admin/exports" startIcon={<OutputIcon />} color="secondary">
              Xuất kho
            </Button>
          </Stack>
        }
      />

      {error ? <MuiAlert severity="error" className="mb-3">{error}</MuiAlert> : null}

      <Grid container spacing={2} className="mb-3">
        {kpis.map((k) => (
          <Grid key={k.label} size={{ xs: 12, sm: 6, md: 4, lg: 2 }}>
            <Card
              component={RouterLink}
              to={k.to}
              className="no-underline transition hover:-translate-y-0.5 hover:shadow-md"
              sx={{ borderLeft: `4px solid ${k.color}` }}
            >
              <CardContent className="flex items-center gap-3 !py-3">
                <Box sx={{ color: k.color, bgcolor: `${k.color}14`, p: 1, borderRadius: 2 }}>{k.icon}</Box>
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    {k.label}
                  </Typography>
                  <Typography variant="h5" sx={{ fontWeight: 800, lineHeight: 1.1 }}>
                    {k.value}
                  </Typography>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, md: 5 }}>
          <ChartCard title="Cơ cấu danh mục master data" height={320}>
            <PieChart>
              <Pie data={masterPie} dataKey="value" nameKey="name" innerRadius={55} outerRadius={100} paddingAngle={3}>
                {masterPie.map((_, i) => (
                  <Cell key={i} fill={COLORS[i % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip />
              <Legend />
            </PieChart>
          </ChartCard>
        </Grid>

        <Grid size={{ xs: 12, md: 7 }}>
          <ChartCard title="Trạng thái phiếu nhập / xuất / kiểm kê" height={320}>
            <BarChart data={receiptBars} margin={{ top: 8, right: 8, left: 0, bottom: 40 }}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="name" interval={0} angle={-20} textAnchor="end" height={60} tick={{ fontSize: 11 }} />
              <YAxis allowDecimals={false} />
              <Tooltip />
              <Bar dataKey="value" radius={[8, 8, 0, 0]}>
                {receiptBars.map((d, i) => (
                  <Cell key={i} fill={d.fill} />
                ))}
              </Bar>
            </BarChart>
          </ChartCard>
        </Grid>

        <Grid size={{ xs: 12, lg: 6 }}>
          <ChartCard title="Cảnh báo tồn thấp (quantity vs minStock)" height={340}>
            {lowStockChart.length ? (
              <BarChart data={lowStockChart} layout="vertical" margin={{ left: 16, right: 16 }}>
                <CartesianGrid strokeDasharray="3 3" horizontal={false} />
                <XAxis type="number" />
                <YAxis type="category" dataKey="name" width={90} tick={{ fontSize: 11 }} />
                <Tooltip
                  formatter={(value, name) => [value as number, name === 'tồn' ? 'Tồn hiện tại' : 'Min stock']}
                  labelFormatter={(label, payload) => {
                    const kho = payload?.[0]?.payload?.kho
                    return kho ? `${label} · ${kho}` : String(label)
                  }}
                />
                <Legend />
                <Bar dataKey="tồn" fill="#ed6c02" radius={[0, 6, 6, 0]} />
                <Bar dataKey="min" fill="#90a4ae" radius={[0, 6, 6, 0]} />
              </BarChart>
            ) : (
              <Box className="flex h-full items-center justify-center">
                <Typography color="text.secondary">Không có mặt hàng tồn thấp</Typography>
              </Box>
            )}
          </ChartCard>
        </Grid>

        <Grid size={{ xs: 12, lg: 6 }}>
          <ChartCard title="Phân bố loại giao dịch kho (50 giao dịch gần nhất)" height={340}>
            {txTypeChart.length ? (
              <PieChart>
                <Pie data={txTypeChart} dataKey="value" nameKey="name" outerRadius={110} label>
                  {txTypeChart.map((_, i) => (
                    <Cell key={i} fill={COLORS[i % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip />
                <Legend />
              </PieChart>
            ) : (
              <Box className="flex h-full items-center justify-center">
                <Typography color="text.secondary">Chưa có giao dịch</Typography>
              </Box>
            )}
          </ChartCard>
        </Grid>

        <Grid size={{ xs: 12 }}>
          <ChartCard title={`Xu hướng biến động tồn theo ngày · Tổng ${stats.transactions} giao dịch`} height={360}>
            {txTrend.length ? (
              <LineChart data={txTrend} margin={{ top: 8, right: 16, left: 0, bottom: 8 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" tick={{ fontSize: 12 }} />
                <YAxis />
                <Tooltip />
                <Legend />
                <Line type="monotone" dataKey="tăng" stroke="#2e7d32" strokeWidth={2} dot={{ r: 3 }} />
                <Line type="monotone" dataKey="giảm" stroke="#c62828" strokeWidth={2} dot={{ r: 3 }} />
                <Line type="monotone" dataKey="net" stroke="#1565c0" strokeWidth={2.5} strokeDasharray="4 4" />
              </LineChart>
            ) : (
              <Box className="flex h-full items-center justify-center">
                <Typography color="text.secondary">Chưa đủ dữ liệu xu hướng</Typography>
              </Box>
            )}
          </ChartCard>
        </Grid>
      </Grid>
    </Box>
  )
}
