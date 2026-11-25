import { useState, type ReactNode } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import {
  AppBar,
  Avatar,
  Box,
  Divider,
  Drawer,
  ListSubheader,
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
import LightModeOutlinedIcon from '@mui/icons-material/LightModeOutlined'
import DarkModeOutlinedIcon from '@mui/icons-material/DarkModeOutlined'
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
import { useAppearance } from '../components/AppearanceContext'
import '../styles/admin.css'

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
      { to: '/admin/imports', label: 'Phiếu nhập (lô/HSD)', icon: <InputIcon fontSize="small" /> },
      { to: '/admin/exports', label: 'Phiếu xuất / lĩnh', icon: <OutputIcon fontSize="small" /> },
      { to: '/admin/stocktakes', label: 'Kiểm kho', icon: <FactCheckIcon fontSize="small" /> },
      { to: '/admin/transactions', label: 'Lịch sử tồn', icon: <HistoryIcon fontSize="small" /> },
    ],
  },
]

export function AdminLayout() {
  const { user, logout } = useAuth()
  const { mode, toggleMode } = useAppearance()
  const nav = useNavigate()
  const location = useLocation()
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))
  const [mobileOpen, setMobileOpen] = useState(false)
  const pageTitle =
    groups.flatMap((group) => group.items).find((item) =>
      item.end ? location.pathname === item.to : location.pathname.startsWith(item.to),
    )?.label ?? 'Quản lý kho'

  const drawer = (
    <Box className="admin-sidebar">
      <Box className="admin-sidebar__brand">
        <Box className="admin-sidebar__brand-mark">
          <WarehouseIcon />
        </Box>
        <Box className="admin-sidebar__brand-copy">
          <Typography className="admin-sidebar__brand-name">Kho Dược</Typography>
          <Typography className="admin-sidebar__brand-caption">QUẢN LÝ NHÀ THUỐC BV</Typography>
        </Box>
      </Box>
      <Divider className="admin-sidebar__divider" />
      <Box component="nav" aria-label="Điều hướng quản lý" className="admin-sidebar__navigation">
        {groups.map((g) => (
          <Box key={g.title} className="admin-sidebar__group">
            <ListSubheader component="div" disableSticky className="admin-sidebar__group-title">
              {g.title}
            </ListSubheader>
            <List disablePadding>
              {g.items.map((item) => (
                <ListItemButton
                  key={item.to}
                  component={NavLink}
                  to={item.to}
                  end={item.end}
                  onClick={() => setMobileOpen(false)}
                  sx={{
                    borderRadius: '10px',
                    minHeight: 43,
                    px: 1.4,
                    mb: 0.45,
                    '&.active': {
                      color: '#fff',
                      background: 'linear-gradient(100deg, #2563eb, #347be7)',
                      boxShadow: '0 5px 16px rgba(37, 99, 235, .25)',
                      '& .MuiListItemIcon-root': { color: '#fff' },
                      '&:hover': { background: 'linear-gradient(100deg, #2563eb, #347be7)' },
                    },
                  }}
                  className="admin-sidebar__link"
                >
                  <ListItemIcon sx={{ minWidth: 35, color: 'inherit' }}>{item.icon}</ListItemIcon>
                  <ListItemText
                    primary={
                      <Typography component="span" sx={{ fontSize: 13.5, fontWeight: 550 }}>
                        {item.label}
                      </Typography>
                    }
                  />
                </ListItemButton>
              ))}
            </List>
          </Box>
        ))}
      </Box>
      <Box className="admin-sidebar__footer">
        <Divider className="admin-sidebar__divider" />
        <Box className="admin-sidebar__user">
          <Avatar className="admin-sidebar__avatar">
            {user?.fullName?.trim().charAt(0).toUpperCase() || 'U'}
          </Avatar>
          <Box className="admin-sidebar__user-copy">
            <Typography className="admin-sidebar__user-name">{user?.fullName || 'Nhân viên'}</Typography>
            <Typography className="admin-sidebar__user-role">
              {user?.roleName || 'Tài khoản quản lý'}
            </Typography>
          </Box>
        </Box>
        <Button
          fullWidth
          className="admin-sidebar__logout"
          startIcon={<LogoutIcon />}
          onClick={() => {
            logout()
            nav('/admin/login')
          }}
        >
          Đăng xuất
        </Button>
        <Button
          fullWidth
          className="admin-sidebar__theme-toggle"
          startIcon={mode === 'light' ? <DarkModeOutlinedIcon /> : <LightModeOutlinedIcon />}
          onClick={toggleMode}
          aria-label={`Chuyển sang giao diện ${mode === 'light' ? 'tối' : 'sáng'}`}
          aria-pressed={mode === 'dark'}
        >
          Giao diện {mode === 'light' ? 'tối' : 'sáng'}
          <span className={`admin-sidebar__theme-indicator ${mode}`} />
        </Button>
      </Box>
    </Box>
  )

  return (
    <Box className={`admin-layout ${mode === 'dark' ? 'admin-layout--dark' : ''}`}>
      <AppBar
        position="fixed"
        color="inherit"
        elevation={0}
        className="admin-topbar"
        sx={{
          width: { md: `calc(100% - ${drawerWidth}px)` },
          ml: { md: `${drawerWidth}px` },
          bgcolor: 'background.paper',
          color: 'text.primary',
          borderBottom: 1,
          borderColor: 'divider',
        }}
      >
        <Toolbar className="admin-topbar__toolbar">
          {isMobile ? (
            <IconButton
              edge="start"
              onClick={() => setMobileOpen(true)}
              className="mr-2"
              aria-label="Mở thanh điều hướng"
            >
              <MenuIcon />
            </IconButton>
          ) : null}
          <Box className="admin-topbar__title">
            <Typography variant="h6" sx={{ fontWeight: 700 }}>
              {pageTitle}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Hệ thống quản lý kho nhà thuốc bệnh viện
            </Typography>
          </Box>
          <Typography className="admin-topbar__portal">
            <WarehouseIcon fontSize="small" />
            CỔNG QUẢN LÝ
          </Typography>
        </Toolbar>
      </AppBar>

      <Box component="nav" sx={{ width: { md: drawerWidth }, flexShrink: { md: 0 } }}>
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: 'block', md: 'none' },
            '& .MuiDrawer-paper': {
              width: drawerWidth,
              boxSizing: 'border-box',
              bgcolor: '#101a2b',
              color: '#e5edf8',
              borderRight: '1px solid rgba(255,255,255,.07)',
            },
          }}
        >
          {drawer}
        </Drawer>
        <Drawer
          variant="permanent"
          open
          sx={{
            display: { xs: 'none', md: 'block' },
            '& .MuiDrawer-paper': {
              width: drawerWidth,
              boxSizing: 'border-box',
              bgcolor: '#101a2b',
              color: '#e5edf8',
              borderRight: '1px solid rgba(255,255,255,.07)',
            },
          }}
        >
          {drawer}
        </Drawer>
      </Box>

      <Box
        component="main"
        className="admin-content min-w-0 flex-1 p-4 md:p-6"
        sx={{
          width: { md: `calc(100% - ${drawerWidth}px)` },
          mt: 8,
          bgcolor: 'background.default',
          color: 'text.primary',
        }}
      >
        <Outlet />
      </Box>
    </Box>
  )
}
