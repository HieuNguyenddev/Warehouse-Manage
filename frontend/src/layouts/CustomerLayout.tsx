import { useState } from 'react'
import { Link as RouterLink, NavLink, Outlet, useNavigate } from 'react-router-dom'
import {
  AppBar,
  Badge,
  Box,
  Button,
  Divider,
  Drawer,
  IconButton,
  Stack,
  Toolbar,
  Typography,
} from '@mui/material'
import HomeIcon from '@mui/icons-material/Home'
import StorefrontIcon from '@mui/icons-material/Storefront'
import Inventory2Icon from '@mui/icons-material/Inventory2'
import ReceiptLongIcon from '@mui/icons-material/ReceiptLong'
import PersonIcon from '@mui/icons-material/Person'
import LogoutIcon from '@mui/icons-material/Logout'
import ShoppingCartOutlinedIcon from '@mui/icons-material/ShoppingCartOutlined'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import AddIcon from '@mui/icons-material/Add'
import RemoveIcon from '@mui/icons-material/Remove'
import CloseIcon from '@mui/icons-material/Close'
import { useAuth } from '../auth/AuthContext'
import { useCart } from '../store/CartContext'
import { StoreProductImage, storeColors } from '../store/storeUi'
import { money } from '../components/ui'

const links = [
  { to: '/store', label: 'Trang chủ', end: true, icon: <HomeIcon fontSize="small" /> },
  { to: '/store/products', label: 'Danh mục thuốc', icon: <Inventory2Icon fontSize="small" /> },
  { to: '/store/orders', label: 'Đơn hàng', icon: <ReceiptLongIcon fontSize="small" /> },
  { to: '/store/profile', label: 'Tài khoản', icon: <PersonIcon fontSize="small" /> },
]

export function CustomerLayout() {
  const { user, logout, isCustomer } = useAuth()
  const cart = useCart()
  const nav = useNavigate()
  const [cartOpen, setCartOpen] = useState(false)

  return (
    <Box
      className="min-h-screen"
      sx={{
        bgcolor: storeColors.cream,
        color: storeColors.ink,
      }}
    >
      <AppBar
        position="sticky"
        elevation={0}
        sx={{
          bgcolor: 'rgba(244,247,242,0.92)',
          color: storeColors.ink,
          borderBottom: `1px solid ${storeColors.mist}`,
          backdropFilter: 'blur(10px)',
        }}
      >
        <Toolbar className="gap-2 md:gap-3" sx={{ minHeight: { xs: 64, md: 72 } }}>
          <Stack
            direction="row"
            spacing={1.25}
            component={RouterLink}
            to="/store"
            className="mr-auto items-center no-underline"
            sx={{ color: 'inherit', textDecoration: 'none' }}
          >
            <Box
              sx={{
                width: 40,
                height: 40,
                borderRadius: 2,
                bgcolor: storeColors.forest,
                color: '#fff',
                display: 'grid',
                placeItems: 'center',
              }}
            >
              <StorefrontIcon fontSize="small" />
            </Box>
            <Box className="hidden sm:block">
              <Typography sx={{ fontWeight: 800, lineHeight: 1.1 }}>
                Nhà thuốc BV
              </Typography>
              <Typography variant="caption" sx={{ color: '#5C7A68' }}>
                Kho dược bệnh viện
              </Typography>
            </Box>
          </Stack>

          <Stack direction="row" spacing={0.5} className="hidden items-center md:flex">
            {links.map((l) => (
              <Button
                key={l.to}
                component={NavLink}
                to={l.to}
                end={l.end}
                variant="text"
                color="inherit"
                startIcon={l.icon}
                sx={{
                  color: '#3D5A4A',
                  '&.active': { color: storeColors.forest, fontWeight: 800 },
                }}
              >
                {l.label}
              </Button>
            ))}
          </Stack>

          <IconButton
            aria-label="Giỏ hàng"
            onClick={() => setCartOpen(true)}
            sx={{
              bgcolor: storeColors.mist,
              '&:hover': { bgcolor: '#dce8df' },
            }}
          >
            <Badge badgeContent={cart.totalQty} color="error" max={99}>
              <ShoppingCartOutlinedIcon />
            </Badge>
          </IconButton>

          {isCustomer ? (
            <>
              <Typography variant="body2" className="hidden lg:block" sx={{ color: '#5C7A68', px: 1 }}>
                {user?.fullName}
              </Typography>
              <Button
                variant="outlined"
                size="small"
                startIcon={<LogoutIcon />}
                onClick={() => {
                  logout()
                  nav('/store/login')
                }}
                sx={{
                  borderColor: storeColors.forest,
                  color: storeColors.forest,
                  display: { xs: 'none', sm: 'inline-flex' },
                }}
              >
                Thoát
              </Button>
            </>
          ) : (
            <Button
              variant="contained"
              size="small"
              onClick={() => nav('/store/login')}
              sx={{
                bgcolor: storeColors.forest,
                fontWeight: 700,
                display: { xs: 'none', sm: 'inline-flex' },
                '&:hover': { bgcolor: storeColors.leaf },
              }}
            >
              Đăng nhập
            </Button>
          )}
        </Toolbar>
      </AppBar>

      <Box component="main" sx={{ minHeight: 'calc(100vh - 72px)' }}>
        <Outlet />
      </Box>

      <Box
        component="footer"
        sx={{
          mt: 6,
          py: 4,
          bgcolor: storeColors.forest,
          color: 'rgba(255,255,255,0.9)',
        }}
      >
        <Box className="mx-auto flex max-w-6xl flex-col gap-2 px-4 sm:flex-row sm:items-center sm:justify-between">
          <Typography sx={{ fontWeight: 700 }}>Nhà thuốc BV</Typography>
          <Typography variant="body2" sx={{ opacity: 0.85 }}>
            Cấp phát thuốc · Vật tư y tế · Kho dược bệnh viện
          </Typography>
        </Box>
      </Box>

      <Drawer
        anchor="right"
        open={cartOpen}
        onClose={() => setCartOpen(false)}
        slotProps={{
          paper: {
            sx: { width: { xs: '100%', sm: 400 }, bgcolor: storeColors.cream },
          },
        }}
      >
        <Box className="flex h-full flex-col">
          <Stack direction="row" className="items-center justify-between p-4">
            <Typography sx={{ fontWeight: 800 }}>
              Giỏ hàng ({cart.totalQty})
            </Typography>
            <IconButton onClick={() => setCartOpen(false)}>
              <CloseIcon />
            </IconButton>
          </Stack>
          <Divider />
          <Box className="flex-1 overflow-auto p-3">
            {!cart.items.length ? (
              <Box className="py-16 text-center">
                <ShoppingCartOutlinedIcon sx={{ fontSize: 48, color: '#9BB5A6' }} />
                <Typography className="mt-2" color="text.secondary">
                  Giỏ còn trống — vào danh mục thuốc để chọn sản phẩm
                </Typography>
                <Button
                  className="mt-4"
                  variant="contained"
                  onClick={() => {
                    setCartOpen(false)
                    nav('/store/products')
                  }}
                  sx={{ bgcolor: storeColors.forest, '&:hover': { bgcolor: storeColors.leaf } }}
                >
                  Mua sắm ngay
                </Button>
              </Box>
            ) : (
              <Stack spacing={1.5}>
                {cart.items.map((item) => (
                  <Box
                    key={item.productId}
                    className="flex gap-3 rounded-2xl bg-white p-2"
                    sx={{ border: `1px solid ${storeColors.mist}` }}
                  >
                    <Box className="h-16 w-16 overflow-hidden rounded-xl">
                      <StoreProductImage src={item.imageUrl} alt={item.name} height={64} />
                    </Box>
                    <Box className="min-w-0 flex-1">
                      <Typography noWrap sx={{ fontWeight: 700, fontSize: 14 }}>
                        {item.name}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {money(item.price)} / {item.unitSymbol || 'sp'}
                      </Typography>
                      <Stack direction="row" spacing={0.5} className="mt-1 items-center">
                        <IconButton size="small" onClick={() => cart.setQty(item.productId, item.quantity - 1)}>
                          <RemoveIcon fontSize="small" />
                        </IconButton>
                        <Typography sx={{ minWidth: 20, textAlign: 'center', fontWeight: 700 }}>
                          {item.quantity}
                        </Typography>
                        <IconButton size="small" onClick={() => cart.setQty(item.productId, item.quantity + 1)}>
                          <AddIcon fontSize="small" />
                        </IconButton>
                        <IconButton
                          size="small"
                          color="error"
                          onClick={() => cart.removeItem(item.productId)}
                        >
                          <DeleteOutlineIcon fontSize="small" />
                        </IconButton>
                      </Stack>
                    </Box>
                    <Typography sx={{ fontWeight: 800, color: storeColors.forest, fontSize: 14 }}>
                      {money(item.price * item.quantity)}
                    </Typography>
                  </Box>
                ))}
              </Stack>
            )}
          </Box>
          {cart.items.length ? (
            <Box className="border-t p-4" sx={{ borderColor: storeColors.mist, bgcolor: '#fff' }}>
              <Stack direction="row" className="mb-3 items-center justify-between">
                <Typography color="text.secondary">Tạm tính</Typography>
                <Typography sx={{ fontWeight: 800, fontSize: 18, color: storeColors.coral }}>
                  {money(cart.totalAmount)}
                </Typography>
              </Stack>
              <Button
                fullWidth
                variant="contained"
                onClick={() => {
                  setCartOpen(false)
                  nav('/store/cart')
                }}
                sx={{ bgcolor: storeColors.coral, '&:hover': { bgcolor: '#d04f00' }, py: 1.25 }}
              >
                Xem giỏ & thanh toán
              </Button>
            </Box>
          ) : null}
        </Box>
      </Drawer>
    </Box>
  )
}
