import { useEffect, useState } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import {
  Box,
  Button,
  CircularProgress,
  IconButton,
  Link,
  Stack,
  Typography,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import RemoveIcon from '@mui/icons-material/Remove'
import AddShoppingCartIcon from '@mui/icons-material/AddShoppingCart'
import ShoppingBagOutlinedIcon from '@mui/icons-material/ShoppingBagOutlined'
import { storeApi } from '../../api/endpoints'
import { ApiError } from '../../api/client'
import { Alert, money } from '../../components/ui'
import { useToast } from '../../components/ToastContext'
import { useCart } from '../../store/CartContext'
import { StoreProductImage, storeColors } from '../../store/storeUi'
import type { ProductDto } from '../../types/api'

export function StoreProductDetail() {
  const { id } = useParams()
  const toast = useToast()
  const cart = useCart()
  const nav = useNavigate()
  const [product, setProduct] = useState<ProductDto | null>(null)
  const [error, setError] = useState('')
  const [qty, setQty] = useState(1)
  const [activeImg, setActiveImg] = useState<string | null>(null)

  useEffect(() => {
    if (!id) return
    void storeApi
      .product(Number(id))
      .then((p) => {
        setProduct(p)
        const img = p.images?.find((x) => x.isPrimary) || p.images?.[0]
        setActiveImg(img?.imageUrl || null)
      })
      .catch((e) => setError(e instanceof ApiError ? e.message : 'Không tải được'))
  }, [id])

  if (error) {
    return (
      <Box className="mx-auto max-w-6xl px-4 py-8">
        <Alert>{error}</Alert>
      </Box>
    )
  }

  if (!product) {
    return (
      <Box className="flex justify-center py-24">
        <CircularProgress sx={{ color: storeColors.forest }} />
      </Box>
    )
  }

  return (
    <Box className="mx-auto max-w-6xl px-4 py-8">
      <Link
        component={RouterLink}
        to="/store/products"
        underline="hover"
        sx={{ color: storeColors.forest, fontWeight: 700 }}
      >
        ← Quay lại siêu thị
      </Link>

      <Box
        className="mt-4 grid grid-cols-1 gap-8 md:grid-cols-[1.05fr_1fr]"
        sx={{
          bgcolor: '#fff',
          borderRadius: 4,
          border: `1px solid ${storeColors.mist}`,
          p: { xs: 2, md: 3 },
          boxShadow: '0 16px 40px rgba(11,110,79,0.07)',
        }}
      >
        <Box>
          <Box className="overflow-hidden rounded-3xl" sx={{ height: { xs: 280, md: 420 } }}>
            <StoreProductImage src={activeImg} alt={product.name} height="100%" />
          </Box>
          {product.images?.length ? (
            <Stack direction="row" spacing={1} className="mt-3 flex-wrap">
              {product.images.map((i) => (
                <Box
                  key={i.id}
                  onClick={() => setActiveImg(i.imageUrl)}
                  className="cursor-pointer overflow-hidden rounded-xl"
                  sx={{
                    width: 72,
                    height: 72,
                    border:
                      activeImg === i.imageUrl
                        ? `2px solid ${storeColors.forest}`
                        : `1px solid ${storeColors.mist}`,
                  }}
                >
                  <StoreProductImage src={i.imageUrl} alt="" height={72} />
                </Box>
              ))}
            </Stack>
          ) : null}
        </Box>

        <Box className="flex flex-col">
          <Typography variant="body2" sx={{ color: '#5C7A68', fontWeight: 700 }}>
            {product.categoryName || 'Sản phẩm'} · {product.sku}
          </Typography>
          <Typography
            sx={{
              fontWeight: 800,
              fontSize: { xs: 28, md: 34 },
              lineHeight: 1.15,
              mt: 1,
            }}
          >
            {product.name}
          </Typography>
          <Typography className="mt-2" sx={{ color: '#5C7A68' }}>
            Đơn vị: {product.unitName || product.unitSymbol || '—'}
          </Typography>
          <Typography sx={{ mt: 2, fontWeight: 800, color: storeColors.coral, fontSize: 32 }}>
            {money(product.price)}
          </Typography>
          <Typography className="mt-3" sx={{ lineHeight: 1.6, color: '#3D5A4A' }}>
            {product.description || 'Sản phẩm chất lượng từ kho FreshMart, sẵn sàng giao nhanh.'}
          </Typography>

          <Stack direction="row" spacing={1.5} className="mt-6 items-center">
            <Typography sx={{ fontWeight: 700 }}>Số lượng</Typography>
            <Stack
              direction="row"
              spacing={1}
              className="items-center rounded-full px-1"
              sx={{ bgcolor: storeColors.mist }}
            >
              <IconButton size="small" onClick={() => setQty((q) => Math.max(1, q - 1))}>
                <RemoveIcon fontSize="small" />
              </IconButton>
              <Typography sx={{ minWidth: 28, textAlign: 'center', fontWeight: 800 }}>{qty}</Typography>
              <IconButton size="small" onClick={() => setQty((q) => q + 1)}>
                <AddIcon fontSize="small" />
              </IconButton>
            </Stack>
          </Stack>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} className="mt-5">
            <Button
              size="large"
              startIcon={<AddShoppingCartIcon />}
              onClick={() => {
                cart.addItem(product, qty)
                toast.success(`Đã thêm ${qty} × ${product.name}`)
              }}
              sx={{
                bgcolor: storeColors.forest,
                fontWeight: 800,
                px: 3,
                '&:hover': { bgcolor: storeColors.leaf },
              }}
            >
              Thêm vào giỏ
            </Button>
            <Button
              size="large"
              variant="outlined"
              startIcon={<ShoppingBagOutlinedIcon />}
              onClick={() => {
                cart.addItem(product, qty)
                nav('/store/cart')
              }}
              sx={{
                borderColor: storeColors.coral,
                color: storeColors.coral,
                fontWeight: 800,
                '&:hover': { borderColor: '#d04f00', bgcolor: 'rgba(232,93,4,0.06)' },
              }}
            >
              Mua ngay
            </Button>
          </Stack>
        </Box>
      </Box>
    </Box>
  )
}
