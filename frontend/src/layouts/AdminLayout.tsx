import { useState, type ReactNode } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import {
  AppBar,
  Box,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  Button,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import MenuIcon from '@mui/icons-material/Menu'
import DashboardIcon from '@mui/icons-material/Dashboard'
import CategoryIcon from '@mui/icons-material/Category'
import InventoryIcon from '@mui/icons-material/Inventory2'
import WarehouseIcon from '@mui/icons-material/Warehouse'
import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings'
import ManageAccountsIcon from '@mui/icons-material/ManageAccounts'
import StraightenIcon from '@mui/icons-material/Straighten'
import LocalShippingIcon from '@mui/icons-material/LocalShipping'
import GroupsIcon from '@mui/icons-material/Groups'
import InputIcon from '@mui/icons-material/Input'
import OutputIcon from '@mui/icons-material/Output'
import FactCheckIcon from '@mui/icons-material/FactCheck'
import HistoryIcon from '@mui/icons-material/History'
import LayersIcon from '@mui/icons-material/Layers'
import LogoutIcon from '@mui/icons-material/Logout'
import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined'
import { useAuth } from '../auth/AuthContext'

const drawerWidth = 260

const groups: {
  title: string
  items: { to: string; label: string; end?: boolean; icon: ReactNode }[]
}[] = [
  {
    title: 'Tổng quan',
    items: [
      { to: '/admin', label: 'Bảng điều khiển', end: true, icon: <DashboardIcon fontSize="small" /> },
      { to: '/admin/reports', label: 'Báo cáo kho', icon: <AssessmentOutlinedIcon fontSize="small" /> },
    ],
  },
  {
    title: 'Danh mục',
    items: [
      { to: '/admin/roles', label: 'Vai trò', icon: <AdminPanelSettingsIcon fontSize="small" /> },
      { to: '/admin/users', label: 'Người dùng', icon: <ManageAccountsIcon fontSize="small" /> },
      { to: '/admin/categories', label: 'Nhóm thuốc', icon: <CategoryIcon fontSize="small" /> },
      { to: '/admin/units', label: 'Đơn vị tính', icon: <StraightenIcon fontSize="small" /> },
      { to: '/admin/suppliers', label: 'NCC dược', icon: <LocalShippingIcon fontSize="small" /> },
      { to: '/admin/customers', label: 'Khoa / phòng', icon: <GroupsIcon fontSize="small" /> },
      { to: '/admin/warehouses', label: 'Kho dược', icon: <WarehouseIcon fontSize="small" /> },
      { to: '/admin/products', label: 'Thuốc / VT', icon: <InventoryIcon fontSize="small" /> },
    ],
  },
  {
    title: 'Kho & tồn',
    items: [
      { to: '/admin/stocks', label: 'Tồn kho', icon: <LayersIcon fontSize="small" /> },
      { to: '/admin/imports', label: 'Phiếu nhập', icon: <InputIcon fontSize="small" /> },
      { to: '/admin/exports', label: 'Phiếu xuất', icon: <OutputIcon fontSize="small" /> },
      { to: '/admin/stocktakes', label: 'Kiểm kho', icon: <FactCheckIcon fontSize="small" /> },
      { to: '/admin/transactions', label: 'Lịch sử tồn', icon: <HistoryIcon fontSize="small" /> },
    ],
  },
]

export function AdminLayout() {
  const { user, logout } = useAuth()
  const nav = useNavigate()
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))
  const [mobileOpen, setMobileOpen] = useState(false)

  const drawer = (
    <Box className="flex h-full flex-col">
      <Toolbar className="gap-2">
        <WarehouseIcon color="primary" />
        <Box>
          <Typography sx={{ fontWeight: 700 }}>Kho Dược BV</Typography>
          <Typography variant="caption" color="text.secondary">
            Quản lý kho nhà thuốc bệnh viện
          </Typography>
        </Box>
      </Toolbar>
      <Divider />
      <Box className="flex-1 overflow-auto px-2 py-2">
        {groups.map((g) => (
          <Box key={g.title} className="mb-2">
            <Typography
              variant="caption"
              color="text.secondary"
              className="px-3 py-1 font-semibold tracking-wide uppercase"
            >
              {g.title}
            </Typography>
            <List dense disablePadding>
              {g.items.map((item) => (
                <ListItemButton
                  key={item.to}
                  component={NavLink}
                  to={item.to}
                  end={item.end}
                  onClick={() => setMobileOpen(false)}
                  sx={{
                    borderRadius: 2,
                    mb: 0.25,
                    '&.active': {
                      bgcolor: 'primary.main',
                      color: 'primary.contrastText',
                      '& .MuiListItemIcon-root': { color: 'inherit' },
                    },
                  }}
                >
                  <ListItemIcon sx={{ minWidth: 36 }}>{item.icon}</ListItemIcon>
                  <ListItemText primary={item.label} />
                </ListItemButton>
              ))}
            </List>
          </Box>
        ))}
      </Box>
      <Divider />
      <Box className="space-y-2 p-3">
        <Typography variant="body2" sx={{ fontWeight: 600 }}>
          {user?.fullName}
        </Typography>
        <Typography variant="caption" color="text.secondary" sx={{ display: "block" }}>
          {user?.roleName} · @{user?.username}
        </Typography>
        <Button
          fullWidth
          variant="outlined"
          startIcon={<LogoutIcon />}
          onClick={() => {
            logout()
            nav('/admin/login')
          }}
        >
          Đăng xuất
        </Button>
      </Box>
    </Box>
  )

  return (
    <Box className="flex min-h-screen bg-slate-50">
      <AppBar
        position="fixed"
        color="inherit"
        elevation={0}
        className="border-b border-slate-200"
        sx={{ width: { md: `calc(100% - ${drawerWidth}px)` }, ml: { md: `${drawerWidth}px` } }}
      >
        <Toolbar>
          {isMobile ? (
            <IconButton edge="start" onClick={() => setMobileOpen(true)} className="mr-2">
              <MenuIcon />
            </IconButton>
          ) : null}
          <Typography variant="h6" className="flex-1" sx={{ fontWeight: 600 }}>
            Hệ thống quản lý kho nhà thuốc bệnh viện
          </Typography>
        </Toolbar>
      </AppBar>

      <Box component="nav" sx={{ width: { md: drawerWidth }, flexShrink: { md: 0 } }}>
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{ display: { xs: 'block', md: 'none' }, '& .MuiDrawer-paper': { width: drawerWidth } }}
        >
          {drawer}
        </Drawer>
        <Drawer
          variant="permanent"
          open
          sx={{
            display: { xs: 'none', md: 'block' },
            '& .MuiDrawer-paper': { width: drawerWidth, boxSizing: 'border-box' },
          }}
        >
          {drawer}
        </Drawer>
      </Box>

      <Box
        component="main"
        className="min-w-0 flex-1 p-4 md:p-6"
        sx={{ width: { md: `calc(100% - ${drawerWidth}px)` }, mt: 8 }}
      >
        <Outlet />
      </Box>
    </Box>
  )
}
