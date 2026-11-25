import {
  cloneElement,
  isValidElement,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactElement,
  type ReactNode,
} from 'react'
import { Link as RouterLink } from 'react-router-dom'
import {
  alpha,
  Alert as MuiAlert,
  Avatar,
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  Chip,
  CircularProgress,
  Divider,
  Grid,
  IconButton,
  LinearProgress,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  Paper,
  Stack,
  Tooltip as MuiTooltip,
  Typography,
  useTheme,
} from '@mui/material'
import DashboardIcon from '@mui/icons-material/Dashboard'
import Inventory2Icon from '@mui/icons-material/Inventory2'
import WarehouseIcon from '@mui/icons-material/Warehouse'
import WarningAmberRoundedIcon from '@mui/icons-material/WarningAmberRounded'
import InputIcon from '@mui/icons-material/Input'
import OutputIcon from '@mui/icons-material/Output'
import RefreshIcon from '@mui/icons-material/Refresh'
import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined'
import FactCheckOutlinedIcon from '@mui/icons-material/FactCheckOutlined'
import LocalPharmacyIcon from '@mui/icons-material/LocalPharmacy'
import ArrowForwardIcon from '@mui/icons-material/ArrowForward'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import LocalShippingOutlinedIcon from '@mui/icons-material/LocalShippingOutlined'
import CategoryOutlinedIcon from '@mui/icons-material/CategoryOutlined'
import EventBusyOutlinedIcon from '@mui/icons-material/EventBusyOutlined'
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
  stockLotsApi,
  stocktakesApi,
  stocksApi,
  suppliersApi,
  transactionsApi,
  warehousesApi,
} from '../../api/endpoints'
import { ApiError } from '../../api/client'
import type { StockLotDto, StockTransactionDto, WarehouseStockDto } from '../../types/api'

const CHART = ['#1565c0', '#00897b', '#ed6c02', '#7b1fa2', '#c62828', '#2e7d32', '#546e7a', '#00838f']

type Stats = {
  products: number
  warehouses: number
  customers: number
  suppliers: number
  categories: number
  lowStock: number
  nearExpiry: number
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
  nearExpiry: 0,
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

function SectionTitle({ title, subtitle, action }: { title: string; subtitle?: string; action?: ReactNode }) {
  return (
    <Stack
      direction={{ xs: 'column', sm: 'row' }}
      spacing={1}
      sx={{ mb: 1.5, alignItems: { sm: 'flex-end' }, justifyContent: 'space-between' }}
    >
      <Box>
        <Typography sx={{ fontWeight: 800, fontSize: 14.5, letterSpacing: -0.2 }}>{title}</Typography>
        {subtitle ? (
          <Typography variant="caption" color="text.secondary" sx={{ fontSize: 11.5 }}>
            {subtitle}
          </Typography>
        ) : null}
      </Box>
      {action}
    </Stack>
  )
}

function SurfaceCard({ children, sx }: { children: ReactNode; sx?: object }) {
  return (
    <Card
      elevation={0}
      sx={{
        height: '100%',
        borderRadius: 3,
        border: '1px solid',
        borderColor: 'divider',
        bgcolor: 'background.paper',
        boxShadow: '0 1px 2px rgba(15,23,42,0.03), 0 6px 16px rgba(15,23,42,0.04)',
        ...sx,
      }}
    >
      {children}
    </Card>
  )
}

/** Đo width thật — Recharts 3 ResponsiveContainer hay ra chart trống (size ≤ 0). */
function useChartWidth() {
  const ref = useRef<HTMLDivElement | null>(null)
  const [width, setWidth] = useState(0)
  useEffect(() => {
    const el = ref.current
    if (!el) return
    const apply = () => setWidth(Math.max(0, Math.round(el.getBoundingClientRect().width)))
    apply()
    const ro = new ResizeObserver(apply)
    ro.observe(el)
    return () => ro.disconnect()
  }, [])
  return { ref, width }
}

function ChartCard({
  title,
  subtitle,
  children,
  height = 260,
  action,
  empty,
}: {
  title: string
  subtitle?: string
  children?: ReactNode
  height?: number
  action?: ReactNode
  empty?: boolean
}) {
  const { ref, width } = useChartWidth()
  return (
    <SurfaceCard>
      <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
        <Stack direction="row" sx={{ mb: 1.25, alignItems: 'flex-start', justifyContent: 'space-between' }}>
          <Box>
            <Typography variant="body2" sx={{ fontWeight: 700, fontSize: 13.5 }}>
              {title}
            </Typography>
            {subtitle ? (
              <Typography variant="caption" color="text.secondary" sx={{ fontSize: 11 }}>
                {subtitle}
              </Typography>
            ) : null}
          </Box>
          {action}
        </Stack>
        <Box ref={ref} sx={{ width: '100%', height, minHeight: height }}>
          {empty ? (
            <Box sx={{ height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              {children}
            </Box>
          ) : width > 0 && isValidElement(children) ? (
            cloneElement(children as ReactElement<{ width?: number; height?: number }>, { width, height })
          ) : null}
        </Box>
      </CardContent>
    </SurfaceCard>
  )
}

function KpiCard({
  label,
  value,
  icon,
  color,
  to,
  hint,
  emphasize,
}: {
  label: string
  value: number
  icon: ReactNode
  color: string
  to: string
  hint?: string
  emphasize?: boolean
}) {
  return (
    <SurfaceCard
      sx={
        emphasize && value > 0
          ? { borderColor: alpha(color, 0.45), bgcolor: alpha(color, 0.04) }
          : undefined
      }
    >
      <CardActionArea component={RouterLink} to={to} sx={{ height: '100%', borderRadius: 3 }}>
        <CardContent sx={{ p: 1.75, '&:last-child': { pb: 1.75 } }}>
          <Stack direction="row" spacing={1} sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 1.25 }}>
            <Avatar
              variant="rounded"
              sx={{
                width: 36,
                height: 36,
                borderRadius: 2,
                bgcolor: alpha(color, 0.12),
                color,
                '& .MuiSvgIcon-root': { fontSize: 18 },
              }}
            >
              {icon}
            </Avatar>
            <ArrowForwardIcon sx={{ fontSize: 16, color: alpha(color, 0.7) }} />
          </Stack>
          <Typography sx={{ fontWeight: 800, fontSize: 24, letterSpacing: -0.4, lineHeight: 1.1, color }}>
            {value.toLocaleString('vi-VN')}
          </Typography>
          <Typography sx={{ mt: 0.6, fontWeight: 700, fontSize: 12.5, color: 'text.primary' }}>{label}</Typography>
          {hint ? (
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.25, fontSize: 10.5 }}>
              {hint}
            </Typography>
          ) : null}
        </CardContent>
      </CardActionArea>
    </SurfaceCard>
  )
}

export function AdminDashboard() {
  const theme = useTheme()
  const [stats, setStats] = useState<Stats>(emptyStats)
  const [lowStocks, setLowStocks] = useState<WarehouseStockDto[]>([])
  const [nearExpiryLots, setNearExpiryLots] = useState<StockLotDto[]>([])
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
          nearExpiryPage,
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
          stocksApi.list({ pageNumber: 1, pageSize: 8, lowStockOnly: true }),
          stockLotsApi.nearExpiry({ withinDays: 90, pageNumber: 1, pageSize: 8 }),
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
          nearExpiry: nearExpiryPage.totalRecords,
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
        setNearExpiryLots(nearExpiryPage.items)
        setRecentTx(recent.items)
      } catch (e) {
        setError(e instanceof ApiError ? e.message : 'Không tải được dashboard')
      } finally {
        setLoading(false)
      }
    })()
  }, [tick])

  const receiptBars = useMemo(
    () => [
      { name: 'Nhập nháp', value: stats.importDraft, fill: theme.palette.warning.main },
      { name: 'Nhập XN', value: stats.importConfirmed, fill: theme.palette.success.main },
      { name: 'Xuất nháp', value: stats.exportDraft, fill: '#fb8c00' },
      { name: 'Xuất XN', value: stats.exportConfirmed, fill: theme.palette.secondary.main },
      { name: 'KK nháp', value: stats.stocktakeDraft, fill: '#7b1fa2' },
      { name: 'KK XN', value: stats.stocktakeConfirmed, fill: theme.palette.primary.main },
    ],
    [stats, theme],
  )

  const receiptPie = useMemo(
    () =>
      [
        { name: 'Nhập XN', value: stats.importConfirmed },
        { name: 'Xuất XN', value: stats.exportConfirmed },
        { name: 'Nhập nháp', value: stats.importDraft },
        { name: 'Xuất nháp', value: stats.exportDraft },
        { name: 'KK XN', value: stats.stocktakeConfirmed },
        { name: 'KK nháp', value: stats.stocktakeDraft },
      ].filter((x) => x.value > 0),
    [stats],
  )

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

  /** KPI ưu tiên: cảnh báo trước, rồi vận hành / danh mục */
  const alertKpis = [
    {
      label: 'Tồn thấp',
      value: stats.lowStock,
      icon: <WarningAmberRoundedIcon fontSize="small" />,
      color: theme.palette.warning.main,
      to: '/admin/stocks?lowStockOnly=true',
      hint: 'Cần nhập bổ sung',
      emphasize: true,
    },
    {
      label: 'Cận hạn / hết hạn',
      value: stats.nearExpiry,
      icon: <EventBusyOutlinedIcon fontSize="small" />,
      color: '#c62828',
      to: '/admin/stocks',
      hint: 'Lô trong 90 ngày',
      emphasize: true,
    },
    {
      label: 'Nhập nháp',
      value: stats.importDraft,
      icon: <InputIcon fontSize="small" />,
      color: '#7b1fa2',
      to: '/admin/imports',
      hint: 'Chờ xác nhận',
      emphasize: true,
    },
    {
      label: 'Xuất nháp',
      value: stats.exportDraft,
      icon: <OutputIcon fontSize="small" />,
      color: theme.palette.error.main,
      to: '/admin/exports',
      hint: 'Chờ trừ tồn',
      emphasize: true,
    },
  ]

  const masterKpis = [
    {
      label: 'Thuốc / VT',
      value: stats.products,
      icon: <Inventory2Icon fontSize="small" />,
      color: theme.palette.primary.main,
      to: '/admin/products',
      hint: 'Danh mục',
    },
    {
      label: 'Kho dược',
      value: stats.warehouses,
      icon: <WarehouseIcon fontSize="small" />,
      color: theme.palette.secondary.main,
      to: '/admin/warehouses',
      hint: 'Kho vật lý',
    },
    {
      label: 'Giao dịch',
      value: stats.transactions,
      icon: <FactCheckOutlinedIcon fontSize="small" />,
      color: '#00838f',
      to: '/admin/transactions',
      hint: 'Lịch sử tồn',
    },
    {
      label: 'Nhập đã XN',
      value: stats.importConfirmed,
      icon: <InputIcon fontSize="small" />,
      color: theme.palette.success.main,
      to: '/admin/imports',
      hint: 'Đã cộng tồn',
    },
  ]

  const quickLinks = [
    { label: 'Nhóm thuốc', value: stats.categories, icon: <CategoryOutlinedIcon fontSize="small" />, to: '/admin/categories' },
    { label: 'NCC dược', value: stats.suppliers, icon: <LocalShippingOutlinedIcon fontSize="small" />, to: '/admin/suppliers' },
    { label: 'Khoa / phòng', value: stats.customers, icon: <GroupsOutlinedIcon fontSize="small" />, to: '/admin/customers' },
    { label: 'Báo cáo kho', value: null, icon: <AssessmentOutlinedIcon fontSize="small" />, to: '/admin/reports' },
  ]

  if (loading) {
    return (
      <Box className="flex min-h-[55vh] flex-col items-center justify-center gap-3">
        <CircularProgress thickness={4} />
        <Typography color="text.secondary" variant="body2">
          Đang tải bảng điều khiển…
        </Typography>
        <Box sx={{ width: 220 }}>
          <LinearProgress />
        </Box>
      </Box>
    )
  }

  return (
    <Box sx={{ pb: 3 }}>
      {/* ===== Header gộp 1 khối ===== */}
      <Paper
        elevation={0}
        sx={{
          mb: 2.5,
          p: { xs: 2, md: 2.25 },
          borderRadius: 3,
          border: '1px solid',
          borderColor: 'divider',
          background: `linear-gradient(135deg, ${alpha(theme.palette.primary.main, 0.06)} 0%, ${alpha(theme.palette.secondary.main, 0.05)} 100%)`,
        }}
      >
        <Stack
          direction={{ xs: 'column', md: 'row' }}
          spacing={2}
          sx={{ alignItems: { md: 'center' }, justifyContent: 'space-between' }}
        >
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
            <Avatar
              variant="rounded"
              sx={{
                width: 44,
                height: 44,
                borderRadius: 2.5,
                bgcolor: theme.palette.primary.main,
                color: '#fff',
              }}
            >
              <DashboardIcon />
            </Avatar>
            <Box>
              <Typography sx={{ fontWeight: 800, fontSize: 18, letterSpacing: -0.3, lineHeight: 1.25 }}>
                Bảng điều khiển kho dược
              </Typography>
              <Typography variant="caption" color="text.secondary" sx={{ fontSize: 12.5 }}>
                Nhà thuốc BV · cảnh báo tồn / HSD · phiếu nhập xuất
              </Typography>
            </Box>
          </Stack>

          <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
            <Chip
              size="small"
              icon={<LocalPharmacyIcon sx={{ fontSize: '16px !important' }} />}
              label={`XN nhập ${stats.importConfirmed.toLocaleString('vi-VN')} · xuất ${stats.exportConfirmed.toLocaleString('vi-VN')}`}
              sx={{ fontWeight: 600, bgcolor: 'background.paper', border: '1px solid', borderColor: 'divider' }}
            />
            <MuiTooltip title="Làm mới">
              <IconButton
                size="small"
                onClick={() => setTick((x) => x + 1)}
                sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 2, bgcolor: 'background.paper' }}
              >
                <RefreshIcon fontSize="small" />
              </IconButton>
            </MuiTooltip>
            <Button
              component={RouterLink}
              to="/admin/reports"
              size="small"
              variant="contained"
              startIcon={<AssessmentOutlinedIcon />}
              sx={{ borderRadius: 2, px: 1.75, fontWeight: 700 }}
            >
              Báo cáo
            </Button>
          </Stack>
        </Stack>
      </Paper>

      {error ? (
        <MuiAlert severity="error" variant="outlined" sx={{ mb: 2, borderRadius: 3 }}>
          {error}
        </MuiAlert>
      ) : null}

      {/* ===== 1. Cảnh báo cần xử lý ===== */}
      <SectionTitle title="Cần xử lý ngay" subtitle="Tồn thấp · cận hạn · phiếu nháp" />
      <Grid container spacing={1.75} sx={{ mb: 3 }}>
        {alertKpis.map((k) => (
          <Grid key={k.label} size={{ xs: 12, sm: 6, md: 3 }}>
            <KpiCard {...k} />
          </Grid>
        ))}
      </Grid>

      {/* ===== 2. Tổng quan vận hành ===== */}
      <SectionTitle title="Tổng quan vận hành" subtitle="Danh mục & phiếu đã xác nhận" />
      <Grid container spacing={1.75} sx={{ mb: 2 }}>
        {masterKpis.map((k) => (
          <Grid key={k.label} size={{ xs: 12, sm: 6, md: 3 }}>
            <KpiCard {...k} />
          </Grid>
        ))}
      </Grid>
      <Stack direction="row" spacing={1} useFlexGap sx={{ mb: 3, flexWrap: 'wrap' }}>
        {quickLinks.map((x) => (
          <Chip
            key={x.label}
            component={RouterLink}
            to={x.to}
            clickable
            icon={x.icon}
            label={x.value == null ? x.label : `${x.label}: ${x.value.toLocaleString('vi-VN')}`}
            sx={{
              height: 32,
              borderRadius: 2,
              fontWeight: 600,
              fontSize: 12,
              bgcolor: 'background.paper',
              border: '1px solid',
              borderColor: 'divider',
              textDecoration: 'none',
            }}
          />
        ))}
      </Stack>

      {/* ===== 3. Ưu tiên + trạng thái phiếu ===== */}
      <SectionTitle title="Theo dõi phiếu & ưu tiên" />
      <Grid container spacing={2} sx={{ mb: 1 }}>
        <Grid size={{ xs: 12, lg: 5 }}>
          <SurfaceCard>
            <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
              <Stack direction="row" sx={{ mb: 1.25, justifyContent: 'space-between', alignItems: 'center' }}>
                <Box>
                  <Typography sx={{ fontWeight: 800, fontSize: 13.5 }}>Ưu tiên xử lý</Typography>
                  <Typography variant="caption" color="text.secondary" sx={{ fontSize: 11 }}>
                    Lô cận hạn (FEFO) · tồn thấp
                  </Typography>
                </Box>
                <Button
                  component={RouterLink}
                  to="/admin/stocks?lowStockOnly=true"
                  size="small"
                  endIcon={<ArrowForwardIcon />}
                  sx={{ borderRadius: 2, fontWeight: 700 }}
                >
                  Xem kho
                </Button>
              </Stack>
              <Divider sx={{ mb: 1 }} />

              <Typography variant="caption" sx={{ fontWeight: 800, color: 'error.main', letterSpacing: 0.3 }}>
                CẬN HẠN / HẾT HẠN
              </Typography>
              {nearExpiryLots.length ? (
                <List dense disablePadding sx={{ mt: 0.5, mb: 1.5 }}>
                  {nearExpiryLots.slice(0, 4).map((lot) => (
                    <ListItem key={lot.id} sx={{ px: 0, py: 0.65 }}>
                      <ListItemText
                        primary={`${lot.productSku || '—'} · ${lot.lotNumber}`}
                        secondary={`HSD ${lot.expiryDate?.slice(0, 10) || '—'} · còn ${lot.daysToExpiry ?? '?'} ngày · SL ${lot.quantity}`}
                        slotProps={{
                          primary: { sx: { fontSize: 12.5, fontWeight: 700 } },
                          secondary: { sx: { fontSize: 11 } },
                        }}
                      />
                      <Chip
                        size="small"
                        label={lot.isExpired ? 'Hết hạn' : 'Cận hạn'}
                        color={lot.isExpired ? 'error' : 'warning'}
                        sx={{ fontWeight: 700, height: 24 }}
                      />
                    </ListItem>
                  ))}
                </List>
              ) : (
                <Typography variant="body2" color="text.secondary" sx={{ my: 1.5, fontSize: 13 }}>
                  Không có lô cận hạn trong 90 ngày.
                </Typography>
              )}

              <Divider sx={{ mb: 1 }} />
              <Typography variant="caption" sx={{ fontWeight: 800, color: 'warning.main', letterSpacing: 0.3 }}>
                TỒN THẤP
              </Typography>
              {lowStocks.length ? (
                <List dense disablePadding sx={{ mt: 0.5 }}>
                  {lowStocks.slice(0, 5).map((s) => {
                    const pct = Math.min(
                      100,
                      Math.round((Number(s.quantity) / Math.max(1, Number(s.minStock))) * 100),
                    )
                    return (
                      <ListItem key={s.id} sx={{ px: 0, py: 0.9, alignItems: 'flex-start' }}>
                        <ListItemAvatar sx={{ minWidth: 44 }}>
                          <Avatar
                            variant="rounded"
                            sx={{
                              width: 36,
                              height: 36,
                              borderRadius: 2,
                              bgcolor: alpha(theme.palette.warning.main, 0.12),
                              color: 'warning.main',
                              fontSize: 11,
                              fontWeight: 800,
                            }}
                          >
                            {(s.productSku || 'SP').slice(0, 2)}
                          </Avatar>
                        </ListItemAvatar>
                        <ListItemText
                          primary={
                            <Typography variant="body2" sx={{ fontWeight: 700, fontSize: 12.5 }} noWrap>
                              {s.productName || s.productSku}
                            </Typography>
                          }
                          secondary={
                            <Box sx={{ mt: 0.35 }}>
                              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', fontSize: 11 }}>
                                {s.warehouseCode} · {Number(s.quantity).toLocaleString('vi-VN')} / min{' '}
                                {Number(s.minStock).toLocaleString('vi-VN')}
                              </Typography>
                              <LinearProgress
                                variant="determinate"
                                value={pct}
                                color={s.isOutOfStock ? 'error' : 'warning'}
                                sx={{ mt: 0.6, height: 5, borderRadius: 99 }}
                              />
                            </Box>
                          }
                        />
                      </ListItem>
                    )
                  })}
                </List>
              ) : (
                <Typography variant="body2" color="text.secondary" sx={{ my: 1.5, fontSize: 13 }}>
                  Kho ổn định — không cảnh báo tồn thấp.
                </Typography>
              )}
            </CardContent>
          </SurfaceCard>
        </Grid>

        <Grid size={{ xs: 12, lg: 7 }}>
          <ChartCard title="Trạng thái phiếu" subtitle="Nhập · xuất · kiểm kê" height={320}>
            <BarChart data={receiptBars} margin={{ top: 8, right: 8, left: 0, bottom: 28 }}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} stroke={alpha('#94a3b8', 0.35)} />
              <XAxis dataKey="name" interval={0} angle={-16} textAnchor="end" height={52} tick={{ fontSize: 11 }} />
              <YAxis allowDecimals={false} tick={{ fontSize: 11 }} />
              <Tooltip
                contentStyle={{
                  borderRadius: 12,
                  border: '1px solid #e2e8f0',
                  boxShadow: '0 8px 24px rgba(15,23,42,0.08)',
                }}
              />
              <Bar dataKey="value" radius={[10, 10, 4, 4]}>
                {receiptBars.map((d, i) => (
                  <Cell key={i} fill={d.fill} />
                ))}
              </Bar>
            </BarChart>
          </ChartCard>
        </Grid>
      </Grid>

      <Grid container spacing={2} sx={{ mt: 0.25 }}>
        <Grid size={{ xs: 12, md: 5 }}>
          {receiptPie.length ? (
            <ChartCard title="Cơ cấu phiếu" subtitle="Tỷ lệ theo trạng thái" height={280}>
              <PieChart>
                <Pie
                  data={receiptPie}
                  dataKey="value"
                  nameKey="name"
                  cx="50%"
                  cy="45%"
                  innerRadius={52}
                  outerRadius={92}
                  paddingAngle={3}
                >
                  {receiptPie.map((_, i) => (
                    <Cell key={i} fill={CHART[i % CHART.length]} />
                  ))}
                </Pie>
                <Tooltip contentStyle={{ borderRadius: 12, border: '1px solid #e2e8f0' }} />
                <Legend verticalAlign="bottom" height={36} />
              </PieChart>
            </ChartCard>
          ) : (
            <ChartCard title="Cơ cấu phiếu" subtitle="Tỷ lệ theo trạng thái" height={280} empty>
              <Typography color="text.secondary">Chưa có phiếu</Typography>
            </ChartCard>
          )}
        </Grid>

        <Grid size={{ xs: 12, md: 7 }}>
          {txTrend.length ? (
            <ChartCard
              title="Xu hướng biến động tồn"
              subtitle={`${stats.transactions.toLocaleString('vi-VN')} giao dịch · tăng / giảm / net`}
              height={280}
            >
              <LineChart data={txTrend} margin={{ top: 8, right: 16, left: 0, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke={alpha('#94a3b8', 0.35)} />
                <XAxis dataKey="date" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 11 }} />
                <Tooltip contentStyle={{ borderRadius: 12, border: '1px solid #e2e8f0' }} />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="tăng"
                  stroke={theme.palette.success.main}
                  strokeWidth={2.2}
                  dot={{ r: 3 }}
                  activeDot={{ r: 5 }}
                />
                <Line
                  type="monotone"
                  dataKey="giảm"
                  stroke={theme.palette.error.main}
                  strokeWidth={2.2}
                  dot={{ r: 3 }}
                  activeDot={{ r: 5 }}
                />
                <Line
                  type="monotone"
                  dataKey="net"
                  stroke={theme.palette.primary.main}
                  strokeWidth={2.4}
                  strokeDasharray="5 4"
                  dot={false}
                />
              </LineChart>
            </ChartCard>
          ) : (
            <ChartCard
              title="Xu hướng biến động tồn"
              subtitle={`${stats.transactions.toLocaleString('vi-VN')} giao dịch · tăng / giảm / net`}
              height={280}
              empty
            >
              <Typography color="text.secondary">Chưa đủ dữ liệu xu hướng</Typography>
            </ChartCard>
          )}
        </Grid>
      </Grid>
    </Box>
  )
}
