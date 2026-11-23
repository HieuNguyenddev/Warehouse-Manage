import { useEffect, useMemo, useState, type ReactNode } from 'react'
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
import TrendingUpIcon from '@mui/icons-material/TrendingUp'
import ArrowForwardIcon from '@mui/icons-material/ArrowForward'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import LocalShippingOutlinedIcon from '@mui/icons-material/LocalShippingOutlined'
import CategoryOutlinedIcon from '@mui/icons-material/CategoryOutlined'
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
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
import type { StockTransactionDto, WarehouseStockDto } from '../../types/api'

const CHART = ['#1565c0', '#00897b', '#ed6c02', '#7b1fa2', '#c62828', '#2e7d32', '#546e7a', '#00838f']

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

function SectionTitle({ title, action }: { title: string; action?: ReactNode }) {
  return (
    <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 1.25 }}>
      <Typography
        variant="caption"
        sx={{ fontWeight: 700, letterSpacing: 0.6, color: 'text.secondary', textTransform: 'uppercase', fontSize: 11 }}
      >
        {title}
      </Typography>
      {action}
    </Stack>
  )
}

function SurfaceCard({
  children,
  sx,
}: {
  children: ReactNode
  sx?: object
}) {
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

function ChartCard({
  title,
  subtitle,
  children,
  height = 280,
  action,
}: {
  title: string
  subtitle?: string
  children: ReactNode
  height?: number
  action?: ReactNode
}) {
  return (
    <SurfaceCard>
      <CardContent sx={{ p: 2, '&:last-child': { pb: 2 } }}>
        <Stack direction="row" alignItems="flex-start" justifyContent="space-between" sx={{ mb: 1.25 }}>
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
        <Box sx={{ width: '100%', height }}>
          <ResponsiveContainer width="100%" height="100%">
            {children as React.ReactElement}
          </ResponsiveContainer>
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
}: {
  label: string
  value: number
  icon: ReactNode
  color: string
  to: string
  hint?: string
}) {
  return (
    <SurfaceCard>
      <CardActionArea component={RouterLink} to={to} sx={{ height: '100%', borderRadius: 3 }}>
        <CardContent sx={{ p: 1.5, '&:last-child': { pb: 1.5 } }}>
          <Stack direction="row" justifyContent="space-between" alignItems="center" spacing={1}>
            <Avatar
              variant="rounded"
              sx={{
                width: 34,
                height: 34,
                borderRadius: 2,
                bgcolor: alpha(color, 0.12),
                color,
                '& .MuiSvgIcon-root': { fontSize: 18 },
              }}
            >
              {icon}
            </Avatar>
            <IconButton
              size="small"
              sx={{ color, bgcolor: alpha(color, 0.06), width: 26, height: 26 }}
              tabIndex={-1}
            >
              <ArrowForwardIcon sx={{ fontSize: 14 }} />
            </IconButton>
          </Stack>
          <Typography sx={{ mt: 1.25, fontWeight: 700, fontSize: 22, letterSpacing: -0.3, lineHeight: 1.1 }}>
            {value.toLocaleString('vi-VN')}
          </Typography>
          <Typography variant="caption" sx={{ mt: 0.5, fontWeight: 600, color: 'text.primary', display: 'block', fontSize: 12 }}>
            {label}
          </Typography>
          {hint ? (
            <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.15, fontSize: 10.5 }}>
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
          stocksApi.list({ pageNumber: 1, pageSize: 8, lowStockOnly: true }),
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
        { name: 'Thuốc/VT', value: stats.products },
        { name: 'Kho', value: stats.warehouses },
        { name: 'Khoa/phòng', value: stats.customers },
        { name: 'NCC', value: stats.suppliers },
        { name: 'Nhóm thuốc', value: stats.categories },
      ].filter((x) => x.value > 0),
    [stats],
  )

  const receiptBars = useMemo(
    () => [
      { name: 'Nhập nháp', value: stats.importDraft, fill: theme.palette.warning.main },
      { name: 'Nhập XN', value: stats.importConfirmed, fill: theme.palette.success.main },
      { name: 'Nhập hủy', value: stats.importCancelled, fill: theme.palette.error.main },
      { name: 'Xuất nháp', value: stats.exportDraft, fill: '#fb8c00' },
      { name: 'Xuất XN', value: stats.exportConfirmed, fill: theme.palette.secondary.main },
      { name: 'Xuất hủy', value: stats.exportCancelled, fill: '#ad1457' },
      { name: 'KK nháp', value: stats.stocktakeDraft, fill: '#7b1fa2' },
      { name: 'KK XN', value: stats.stocktakeConfirmed, fill: theme.palette.primary.main },
    ],
    [stats, theme],
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
    {
      label: 'Thuốc / vật tư',
      value: stats.products,
      icon: <Inventory2Icon fontSize="small" />,
      color: theme.palette.primary.main,
      to: '/admin/products',
      hint: 'Danh mục đang quản lý',
    },
    {
      label: 'Kho dược',
      value: stats.warehouses,
      icon: <WarehouseIcon fontSize="small" />,
      color: theme.palette.secondary.main,
      to: '/admin/warehouses',
      hint: 'Kho vật lý / logic',
    },
    {
      label: 'Cảnh báo tồn thấp',
      value: stats.lowStock,
      icon: <WarningAmberRoundedIcon fontSize="small" />,
      color: theme.palette.warning.main,
      to: '/admin/stocks?lowStockOnly=true',
      hint: 'Cần nhập bổ sung',
    },
    {
      label: 'Phiếu nhập nháp',
      value: stats.importDraft,
      icon: <InputIcon fontSize="small" />,
      color: '#7b1fa2',
      to: '/admin/imports',
      hint: 'Chờ xác nhận',
    },
    {
      label: 'Phiếu xuất nháp',
      value: stats.exportDraft,
      icon: <OutputIcon fontSize="small" />,
      color: theme.palette.error.main,
      to: '/admin/exports',
      hint: 'Chờ trừ tồn',
    },
    {
      label: 'Giao dịch kho',
      value: stats.transactions,
      icon: <FactCheckOutlinedIcon fontSize="small" />,
      color: '#00838f',
      to: '/admin/transactions',
      hint: 'Toàn bộ lịch sử',
    },
  ]

  if (loading) {
    return (
      <Box className="flex min-h-[55vh] flex-col items-center justify-center gap-3">
        <CircularProgress thickness={4} />
        <Typography color="text.secondary" variant="body2">
          Đang tải bảng điều khiển Material…
        </Typography>
        <Box sx={{ width: 220 }}>
          <LinearProgress />
        </Box>
      </Box>
    )
  }

  return (
    <Box sx={{ pb: 2 }}>
      {/* Header */}
      <Stack
        direction={{ xs: 'column', md: 'row' }}
        spacing={1.5}
        alignItems={{ md: 'center' }}
        justifyContent="space-between"
        sx={{ mb: 2 }}
      >
        <Stack direction="row" spacing={1.25} alignItems="center">
          <Avatar
            variant="rounded"
            sx={{
              width: 38,
              height: 38,
              borderRadius: 2,
              bgcolor: alpha(theme.palette.primary.main, 0.12),
              color: 'primary.main',
              '& .MuiSvgIcon-root': { fontSize: 20 },
            }}
          >
            <DashboardIcon />
          </Avatar>
          <Box>
            <Typography sx={{ fontWeight: 700, fontSize: 17, letterSpacing: -0.2, lineHeight: 1.25 }}>
              Bảng điều khiển
            </Typography>
            <Typography variant="caption" color="text.secondary" sx={{ fontSize: 12 }}>
              Kho nhà thuốc BV · tổng quan vận hành
            </Typography>
          </Box>
        </Stack>
        <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
          <MuiTooltip title="Làm mới dữ liệu">
            <IconButton
              size="small"
              onClick={() => setTick((x) => x + 1)}
              sx={{
                border: '1px solid',
                borderColor: 'divider',
                borderRadius: 2,
                bgcolor: 'background.paper',
              }}
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
            sx={{ borderRadius: 2, px: 1.5 }}
          >
            Báo cáo kho
          </Button>
        </Stack>
      </Stack>

      {error ? (
        <MuiAlert severity="error" variant="outlined" sx={{ mb: 2, borderRadius: 3 }}>
          {error}
        </MuiAlert>
      ) : null}

      {/* Hero */}
      <Paper
        elevation={0}
        sx={{
          mb: 2,
          borderRadius: 3,
          overflow: 'hidden',
          position: 'relative',
          color: '#fff',
          background: `linear-gradient(135deg, ${theme.palette.primary.dark} 0%, ${theme.palette.primary.main} 48%, ${theme.palette.secondary.main} 100%)`,
          boxShadow: `0 8px 24px ${alpha(theme.palette.primary.main, 0.22)}`,
        }}
      >
        <Box
          sx={{
            position: 'absolute',
            inset: 0,
            background:
              'radial-gradient(circle at 85% 20%, rgba(255,255,255,0.16), transparent 40%), radial-gradient(circle at 10% 80%, rgba(255,255,255,0.06), transparent 35%)',
            pointerEvents: 'none',
          }}
        />
        <Box className="relative flex flex-col gap-2.5 p-3 md:flex-row md:items-center md:justify-between">
          <Stack direction="row" spacing={1.25} alignItems="flex-start">
            <Avatar
              sx={{
                width: 40,
                height: 40,
                bgcolor: alpha('#fff', 0.16),
                backdropFilter: 'blur(6px)',
                '& .MuiSvgIcon-root': { fontSize: 20 },
              }}
            >
              <LocalPharmacyIcon />
            </Avatar>
            <Box>
              <Chip
                size="small"
                icon={<TrendingUpIcon sx={{ fontSize: '14px !important', color: '#fff !important' }} />}
                label="Vận hành kho dược"
                sx={{
                  mb: 0.75,
                  height: 22,
                  color: '#fff',
                  bgcolor: alpha('#fff', 0.14),
                  fontWeight: 600,
                  fontSize: 11,
                  '& .MuiChip-icon': { color: '#fff' },
                }}
              />
              <Typography sx={{ fontWeight: 700, fontSize: { xs: 15, md: 17 }, letterSpacing: -0.2, lineHeight: 1.3 }}>
                Trung tâm điều hành nhà thuốc BV
              </Typography>
              <Typography variant="caption" sx={{ mt: 0.4, opacity: 0.9, maxWidth: 520, display: 'block', fontSize: 11.5, lineHeight: 1.4 }}>
                Theo dõi tồn thấp, phiếu đã xác nhận và kiểm kê — in báo cáo quy định nhanh.
              </Typography>
            </Box>
          </Stack>

          <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
            {[
              { label: 'Nhập đã XN', value: stats.importConfirmed, icon: <InputIcon sx={{ fontSize: 14 }} /> },
              { label: 'Xuất đã XN', value: stats.exportConfirmed, icon: <OutputIcon sx={{ fontSize: 14 }} /> },
              { label: 'Tồn thấp', value: stats.lowStock, icon: <WarningAmberRoundedIcon sx={{ fontSize: 14 }} /> },
              { label: 'KK đã XN', value: stats.stocktakeConfirmed, icon: <FactCheckOutlinedIcon sx={{ fontSize: 14 }} /> },
            ].map((x) => (
              <Box
                key={x.label}
                sx={{
                  minWidth: 92,
                  px: 1.15,
                  py: 0.85,
                  borderRadius: 2,
                  bgcolor: alpha('#fff', 0.12),
                  border: `1px solid ${alpha('#fff', 0.14)}`,
                  backdropFilter: 'blur(8px)',
                }}
              >
                <Stack direction="row" spacing={0.5} alignItems="center" sx={{ opacity: 0.9, mb: 0.2 }}>
                  {x.icon}
                  <Typography sx={{ fontWeight: 600, fontSize: 10.5 }}>
                    {x.label}
                  </Typography>
                </Stack>
                <Typography sx={{ fontWeight: 700, fontSize: 16, lineHeight: 1.15 }}>
                  {x.value.toLocaleString('vi-VN')}
                </Typography>
              </Box>
            ))}
          </Stack>
        </Box>
      </Paper>

      {/* KPI */}
      <SectionTitle title="Chỉ số nhanh" />
      <Grid container spacing={2} sx={{ mb: 3 }}>
        {kpis.map((k) => (
          <Grid key={k.label} size={{ xs: 12, sm: 6, md: 4, lg: 2 }}>
            <KpiCard {...k} />
          </Grid>
        ))}
      </Grid>

      {/* Master snapshot chips */}
      <SectionTitle title="Danh mục hệ thống" />
      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap sx={{ mb: 3 }}>
        {[
          { label: 'Nhóm thuốc', value: stats.categories, icon: <CategoryOutlinedIcon fontSize="small" />, to: '/admin/categories' },
          { label: 'NCC dược', value: stats.suppliers, icon: <LocalShippingOutlinedIcon fontSize="small" />, to: '/admin/suppliers' },
          { label: 'Khoa / phòng', value: stats.customers, icon: <GroupsOutlinedIcon fontSize="small" />, to: '/admin/customers' },
        ].map((x) => (
          <Chip
            key={x.label}
            component={RouterLink}
            to={x.to}
            clickable
            icon={x.icon}
            label={`${x.label}: ${x.value.toLocaleString('vi-VN')}`}
            sx={{
              height: 30,
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

      <SectionTitle title="Phân tích & cảnh báo" />
      <Grid container spacing={2}>
        <Grid size={{ xs: 12, md: 5 }}>
          <ChartCard title="Cơ cấu danh mục" subtitle="Master data đang vận hành" height={260}>
            <PieChart>
              <Pie
                data={masterPie}
                dataKey="value"
                nameKey="name"
                innerRadius={58}
                outerRadius={98}
                paddingAngle={3}
                cornerRadius={6}
              >
                {masterPie.map((_, i) => (
                  <Cell key={i} fill={CHART[i % CHART.length]} />
                ))}
              </Pie>
              <Tooltip />
              <Legend />
            </PieChart>
          </ChartCard>
        </Grid>

        <Grid size={{ xs: 12, md: 7 }}>
          <ChartCard title="Trạng thái phiếu" subtitle="Nhập · xuất · kiểm kê" height={260}>
            <BarChart data={receiptBars} margin={{ top: 8, right: 8, left: 0, bottom: 36 }}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} stroke={alpha('#94a3b8', 0.35)} />
              <XAxis dataKey="name" interval={0} angle={-18} textAnchor="end" height={58} tick={{ fontSize: 11 }} />
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

        <Grid size={{ xs: 12, lg: 7 }}>
          <ChartCard title="Cảnh báo tồn thấp" subtitle="So sánh tồn hiện tại với ngưỡng min" height={280}>
            {lowStockChart.length ? (
              <BarChart data={lowStockChart} layout="vertical" margin={{ left: 8, right: 16 }}>
                <CartesianGrid strokeDasharray="3 3" horizontal={false} stroke={alpha('#94a3b8', 0.35)} />
                <XAxis type="number" tick={{ fontSize: 11 }} />
                <YAxis type="category" dataKey="name" width={88} tick={{ fontSize: 11 }} />
                <Tooltip
                  formatter={(value, name) => [value as number, name === 'tồn' ? 'Tồn hiện tại' : 'Min stock']}
                  labelFormatter={(label, payload) => {
                    const kho = payload?.[0]?.payload?.kho
                    return kho ? `${label} · ${kho}` : String(label)
                  }}
                  contentStyle={{ borderRadius: 12, border: '1px solid #e2e8f0' }}
                />
                <Legend />
                <Bar dataKey="tồn" fill={theme.palette.warning.main} radius={[0, 8, 8, 0]} />
                <Bar dataKey="min" fill="#90a4ae" radius={[0, 8, 8, 0]} />
              </BarChart>
            ) : (
              <Box className="flex h-full items-center justify-center">
                <Typography color="text.secondary">Không có mặt hàng tồn thấp</Typography>
              </Box>
            )}
          </ChartCard>
        </Grid>

        <Grid size={{ xs: 12, lg: 5 }}>
          <SurfaceCard>
            <CardContent sx={{ p: 2, '&:last-child': { pb: 1.25 } }}>
              <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 1 }}>
                <Box>
                  <Typography variant="body2" sx={{ fontWeight: 700, fontSize: 13.5 }}>
                    Ưu tiên xử lý
                  </Typography>
                  <Typography variant="caption" color="text.secondary" sx={{ fontSize: 11 }}>
                    Các SKU tồn thấp gần đây
                  </Typography>
                </Box>
                <Button
                  component={RouterLink}
                  to="/admin/stocks?lowStockOnly=true"
                  size="small"
                  endIcon={<ArrowForwardIcon />}
                  sx={{ borderRadius: 2 }}
                >
                  Xem tất cả
                </Button>
              </Stack>
              <Divider sx={{ mb: 0.5 }} />
              {lowStocks.length ? (
                <List dense disablePadding>
                  {lowStocks.slice(0, 6).map((s) => {
                    const pct = Math.min(
                      100,
                      Math.round((Number(s.quantity) / Math.max(1, Number(s.minStock))) * 100),
                    )
                    return (
                      <ListItem key={s.id} sx={{ px: 0, py: 1.1, alignItems: 'flex-start' }}>
                        <ListItemAvatar sx={{ minWidth: 48 }}>
                          <Avatar
                            variant="rounded"
                            sx={{
                              width: 40,
                              height: 40,
                              borderRadius: 2,
                              bgcolor: alpha(theme.palette.warning.main, 0.12),
                              color: 'warning.main',
                              fontSize: 12,
                              fontWeight: 800,
                            }}
                          >
                            {(s.productSku || 'SP').slice(0, 2)}
                          </Avatar>
                        </ListItemAvatar>
                        <ListItemText
                          primary={
                            <Typography variant="body2" sx={{ fontWeight: 600, fontSize: 13 }} noWrap>
                              {s.productName || s.productSku}
                            </Typography>
                          }
                          secondary={
                            <Box sx={{ mt: 0.35 }}>
                              <Typography variant="caption" color="text.secondary" display="block" sx={{ fontSize: 11 }}>
                                {s.warehouseCode} · tồn {Number(s.quantity).toLocaleString('vi-VN')} / min{' '}
                                {Number(s.minStock).toLocaleString('vi-VN')}
                              </Typography>
                              <LinearProgress
                                variant="determinate"
                                value={pct}
                                color={s.isOutOfStock ? 'error' : 'warning'}
                                sx={{ mt: 0.75, height: 6, borderRadius: 99 }}
                              />
                            </Box>
                          }
                        />
                      </ListItem>
                    )
                  })}
                </List>
              ) : (
                <Box className="flex min-h-[220px] items-center justify-center">
                  <Typography color="text.secondary" variant="body2">
                    Kho đang ổn định — không có cảnh báo
                  </Typography>
                </Box>
              )}
            </CardContent>
          </SurfaceCard>
        </Grid>

        <Grid size={{ xs: 12, md: 5 }}>
          <ChartCard title="Loại giao dịch" subtitle="50 phát sinh gần nhất" height={260}>
            {txTypeChart.length ? (
              <PieChart>
                <Pie data={txTypeChart} dataKey="value" nameKey="name" outerRadius={105} paddingAngle={2} cornerRadius={5}>
                  {txTypeChart.map((_, i) => (
                    <Cell key={i} fill={CHART[i % CHART.length]} />
                  ))}
                </Pie>
                <Tooltip contentStyle={{ borderRadius: 12, border: '1px solid #e2e8f0' }} />
                <Legend />
              </PieChart>
            ) : (
              <Box className="flex h-full items-center justify-center">
                <Typography color="text.secondary">Chưa có giao dịch</Typography>
              </Box>
            )}
          </ChartCard>
        </Grid>

        <Grid size={{ xs: 12, md: 7 }}>
          <ChartCard
            title="Xu hướng biến động tồn"
            subtitle={`Tổng ${stats.transactions.toLocaleString('vi-VN')} giao dịch · tăng / giảm / net`}
            height={260}
          >
            {txTrend.length ? (
              <AreaChart data={txTrend} margin={{ top: 8, right: 12, left: 0, bottom: 0 }}>
                <defs>
                  <linearGradient id="gUp" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor={theme.palette.success.main} stopOpacity={0.35} />
                    <stop offset="100%" stopColor={theme.palette.success.main} stopOpacity={0.02} />
                  </linearGradient>
                  <linearGradient id="gDown" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor={theme.palette.error.main} stopOpacity={0.28} />
                    <stop offset="100%" stopColor={theme.palette.error.main} stopOpacity={0.02} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke={alpha('#94a3b8', 0.35)} />
                <XAxis dataKey="date" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 11 }} />
                <Tooltip contentStyle={{ borderRadius: 12, border: '1px solid #e2e8f0' }} />
                <Legend />
                <Area type="monotone" dataKey="tăng" stroke={theme.palette.success.main} fill="url(#gUp)" strokeWidth={2} />
                <Area type="monotone" dataKey="giảm" stroke={theme.palette.error.main} fill="url(#gDown)" strokeWidth={2} />
                <Area
                  type="monotone"
                  dataKey="net"
                  stroke={theme.palette.primary.main}
                  fill="transparent"
                  strokeWidth={2.5}
                  strokeDasharray="5 4"
                />
              </AreaChart>
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
