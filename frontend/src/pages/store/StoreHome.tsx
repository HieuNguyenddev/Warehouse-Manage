import { useEffect, useState } from 'react'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { Box, Button, Chip, Stack, Typography } from '@mui/material'
import LocalShippingOutlinedIcon from '@mui/icons-material/LocalShippingOutlined'
import VerifiedOutlinedIcon from '@mui/icons-material/VerifiedOutlined'
import ScheduleOutlinedIcon from '@mui/icons-material/ScheduleOutlined'
import ShoppingBagOutlinedIcon from '@mui/icons-material/ShoppingBagOutlined'
import AddShoppingCartIcon from '@mui/icons-material/AddShoppingCart'
import ArrowForwardIcon from '@mui/icons-material/ArrowForward'
import { storeApi } from '../../api/endpoints'
import { useAuth } from '../../auth/AuthContext'
import { useCart } from '../../store/CartContext'
import { StoreProductImage, storeColors } from '../../store/storeUi'
import { money } from '../../components/ui'
import { useToast } from '../../components/ToastContext'
import type { CategoryDto, ProductDto } from '../../types/api'

export function StoreHome() {
  const { user } = useAuth()
  const cart = useCart()
  const toast = useToast()
  const nav = useNavigate()
  const [cats, setCats] = useState<CategoryDto[]>([])
  const [featured, setFeatured] = useState<ProductDto[]>([])

  useEffect(() => {
    void (async () => {
      try {
        const [c, p] = await Promise.all([
          storeApi.categories({ pageNumber: 1, pageSize: 12 }),
          storeApi.products({ pageNumber: 1, pageSize: 48 }),
        ])
        setCats(c.items.filter((x) => x.isActive).slice(0, 8))
        // Featured: mỗi danh mục tối đa 1 SP → đa dạng, không trùng tên
        const seenName = new Set<string>()
        const seenCat = new Set<number>()
        const picked: ProductDto[] = []
        for (const item of p.items) {
          const nameKey = (item.name || '').trim().toLowerCase()
          if (!nameKey || seenName.has(nameKey)) continue
          if (item.categoryId && seenCat.has(item.categoryId) && picked.length >= 4) continue
          seenName.add(nameKey)
          if (item.categoryId) seenCat.add(item.categoryId)
          picked.push(item)
          if (picked.length >= 12) break
        }
        setFeatured(picked)
      } catch {
        setCats([])
        setFeatured([])
      }
    })()
  }, [])

  return (
    <Box>
      {/* Hero — full bleed */}
      <Box
        className="relative overflow-hidden"
        sx={{
          minHeight: { xs: 420, md: 520 },
          background: `
            radial-gradient(ellipse 80% 60% at 85% 20%, rgba(212,225,87,0.45), transparent 55%),
            linear-gradient(135deg, #0B6E4F 0%, #0F8A5F 45%, #126B4A 100%)
          `,
          color: '#fff',
        }}
      >
        <Box
          className="pointer-events-none absolute inset-0 opacity-30"
          sx={{
            backgroundImage:
              'url("data:image/svg+xml,%3Csvg width=\'60\' height=\'60\' viewBox=\'0 0 60 60\' xmlns=\'http://www.w3.org/2000/svg\'%3E%3Cg fill=\'none\' fill-rule=\'evenodd\'%3E%3Cg fill=\'%23ffffff\' fill-opacity=\'0.12\'%3E%3Cpath d=\'M36 34v-4h-2v4h-4v2h4v4h2v-4h4v-2h-4zm0-30V0h-2v4h-4v2h4v4h2V6h4V4h-4zM6 34v-4H4v4H0v2h4v4h2v-4h4v-2H6zM6 4V0H4v4H0v2h4v4h2V6h4V4H6z\'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")',
          }}
        />
        <Box className="relative mx-auto flex max-w-6xl flex-col gap-8 px-4 py-12 md:flex-row md:items-end md:py-16">
          <Box className="max-w-xl flex-1">
            <Typography
              sx={{
                fontWeight: 800,
                fontSize: { xs: '2.4rem', md: '3.4rem' },
                lineHeight: 1.05,
                letterSpacing: '-0.03em',
              }}
            >
              Nhà thuốc BV
            </Typography>
            <Typography className="mt-3 max-w-md" sx={{ opacity: 0.92, fontSize: 17, lineHeight: 1.5 }}>
              Xin chào {user?.fullName?.split(' ').slice(-1)[0] || 'bạn'} — tra cứu thuốc và vật tư
              từ kho dược bệnh viện, đặt nhận nhanh trong ngày.
            </Typography>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} className="mt-6">
              <Button
                size="large"
                endIcon={<ShoppingBagOutlinedIcon />}
                onClick={() => nav('/store/products')}
                sx={{
                  bgcolor: storeColors.citrus,
                  color: storeColors.ink,
                  fontWeight: 800,
                  px: 3,
                  '&:hover': { bgcolor: '#c5d63d' },
                }}
              >
                Mua sắm ngay
              </Button>
              <Button
                size="large"
                variant="outlined"
                onClick={() => nav('/store/cart')}
                sx={{
                  borderColor: 'rgba(255,255,255,0.55)',
                  color: '#fff',
                  fontWeight: 700,
                  '&:hover': { borderColor: '#fff', bgcolor: 'rgba(255,255,255,0.08)' },
                }}
              >
                Giỏ hàng ({cart.totalQty})
              </Button>
            </Stack>
          </Box>
          <Stack spacing={1.5} className="md:mb-2 md:w-72">
            {[
              { icon: <VerifiedOutlinedIcon />, text: 'Hàng chính hãng từ kho' },
              { icon: <ScheduleOutlinedIcon />, text: 'Giao nhanh khung giờ bạn chọn' },
              { icon: <LocalShippingOutlinedIcon />, text: 'Miễn phí đơn từ 300.000đ' },
            ].map((x) => (
              <Stack
                key={x.text}
                direction="row"
                spacing={1.5}
                className="items-center rounded-2xl px-3 py-2.5"
                sx={{ bgcolor: 'rgba(255,255,255,0.12)', backdropFilter: 'blur(6px)' }}
              >
                {x.icon}
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  {x.text}
                </Typography>
              </Stack>
            ))}
          </Stack>
        </Box>
      </Box>

      {/* Categories */}
      <Box className="mx-auto max-w-6xl px-4 py-10">
        <Stack direction="row" className="mb-4 items-end justify-between">
          <Box>
            <Typography sx={{ fontWeight: 800, fontSize: 24 }}>
              Danh mục nổi bật
            </Typography>
            <Typography variant="body2" sx={{ color: '#5C7A68' }}>
              Chọn nhanh theo nhu cầu mua sắm
            </Typography>
          </Box>
          <Button
            component={RouterLink}
            to="/store/products"
            endIcon={<ArrowForwardIcon />}
            sx={{ color: storeColors.forest, fontWeight: 700 }}
          >
            Tất cả
          </Button>
        </Stack>
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 md:grid-cols-8">
          {cats.map((c) => (
            <Box
              key={c.id}
              component={RouterLink}
              to={`/store/products?categoryId=${c.id}`}
              className="rounded-2xl p-3 text-center no-underline transition hover:-translate-y-0.5"
              sx={{
                bgcolor: '#fff',
                border: `1px solid ${storeColors.mist}`,
                color: storeColors.ink,
                textDecoration: 'none',
                boxShadow: '0 8px 24px rgba(11,110,79,0.06)',
              }}
            >
              <Box
                className="mx-auto mb-2 grid h-10 w-10 place-items-center rounded-full text-sm font-bold"
                sx={{ bgcolor: storeColors.mist, color: storeColors.forest }}
              >
                {c.name.slice(0, 1)}
              </Box>
              <Typography variant="caption" sx={{ fontWeight: 700, lineHeight: 1.2, display: 'block' }}>
                {c.name}
              </Typography>
            </Box>
          ))}
        </div>
      </Box>

      {/* Featured products */}
      <Box className="mx-auto max-w-6xl px-4 pb-14">
        <Stack direction="row" className="mb-4 items-end justify-between">
          <Box>
            <Typography sx={{ fontWeight: 800, fontSize: 24 }}>
              Gợi ý hôm nay
            </Typography>
            <Typography variant="body2" sx={{ color: '#5C7A68' }}>
              Thêm vào giỏ chỉ với một chạm
            </Typography>
          </Box>
        </Stack>
        <div className="grid grid-cols-2 gap-3 md:grid-cols-4 md:gap-4">
          {featured.map((p) => {
            const img = p.images?.find((x) => x.isPrimary) || p.images?.[0]
            return (
              <Box
                key={p.id}
                className="group flex flex-col overflow-hidden rounded-3xl bg-white transition hover:-translate-y-1"
                sx={{
                  border: `1px solid ${storeColors.mist}`,
                  boxShadow: '0 10px 30px rgba(11,110,79,0.07)',
                }}
              >
                <Box
                  component={RouterLink}
                  to={`/store/products/${p.id}`}
                  className="relative block overflow-hidden"
                  sx={{ height: 160, textDecoration: 'none' }}
                >
                  <StoreProductImage src={img?.imageUrl} alt={p.name} height={160} />
                  {p.categoryName ? (
                    <Chip
                      size="small"
                      label={p.categoryName}
                      sx={{
                        position: 'absolute',
                        left: 10,
                        top: 10,
                        bgcolor: 'rgba(255,255,255,0.92)',
                        fontWeight: 700,
                        fontSize: 11,
                      }}
                    />
                  ) : null}
                </Box>
                <Box className="flex flex-1 flex-col gap-2 p-3">
                  <Typography
                    component={RouterLink}
                    to={`/store/products/${p.id}`}
                    sx={{
                      fontWeight: 800,
                      color: storeColors.ink,
                      textDecoration: 'none',
                      fontSize: 15,
                      lineHeight: 1.3,
                      minHeight: 40,
                    }}
                  >
                    {p.name}
                  </Typography>
                  <Typography variant="caption" sx={{ color: '#5C7A68' }}>
                    {p.unitSymbol || 'sp'}
                  </Typography>
                  <Stack direction="row" className="mt-auto items-center justify-between gap-2">
                    <Typography sx={{ fontWeight: 800, color: storeColors.coral, fontSize: 16 }}>
                      {money(p.price)}
                    </Typography>
                    <Button
                      size="small"
                      variant="contained"
                      startIcon={<AddShoppingCartIcon />}
                      onClick={() => {
                        cart.addItem(p, 1)
                        toast.success(`Đã thêm ${p.name}`)
                      }}
                      sx={{
                        bgcolor: storeColors.forest,
                        minWidth: 0,
                        px: 1.25,
                        '&:hover': { bgcolor: storeColors.leaf },
                        '& .MuiButton-startIcon': { mr: { xs: 0, sm: 0.75 } },
                      }}
                    >
                      <Box component="span" className="hidden sm:inline">
                        Mua
                      </Box>
                    </Button>
                  </Stack>
                </Box>
              </Box>
            )
          })}
        </div>
      </Box>
    </Box>
  )
}
