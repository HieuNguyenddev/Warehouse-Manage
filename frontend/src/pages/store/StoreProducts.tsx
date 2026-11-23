import { useEffect, useState } from 'react'
import { Link as RouterLink, useSearchParams } from 'react-router-dom'
import {
  Box,
  Button,
  Chip,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import AddShoppingCartIcon from '@mui/icons-material/AddShoppingCart'
import { storeApi } from '../../api/endpoints'
import { ApiError } from '../../api/client'
import { Alert, Pager, money } from '../../components/ui'
import { useToast } from '../../components/ToastContext'
import { useCart } from '../../store/CartContext'
import { StoreProductImage, storeColors } from '../../store/storeUi'
import type { CategoryDto, ProductDto } from '../../types/api'

export function StoreProducts() {
  const toast = useToast()
  const cart = useCart()
  const [searchParams, setSearchParams] = useSearchParams()
  const initialCat = Number(searchParams.get('categoryId') || 0) || ''
  const [rows, setRows] = useState<ProductDto[]>([])
  const [cats, setCats] = useState<CategoryDto[]>([])
  const [categoryId, setCategoryId] = useState<number | ''>(initialCat === '' ? '' : Number(initialCat))
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [error, setError] = useState('')

  useEffect(() => {
    void storeApi
      .categories({ pageNumber: 1, pageSize: 100 })
      .then((d) => setCats(d.items))
      .catch(() => setCats([]))
  }, [])

  useEffect(() => {
    const fromUrl = Number(searchParams.get('categoryId') || 0) || ''
    if (fromUrl !== categoryId && fromUrl !== '') {
      setCategoryId(fromUrl)
      setPage(1)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams])

  useEffect(() => {
    void (async () => {
      try {
        const data = await storeApi.products({
          pageNumber: page,
          pageSize: 12,
          search,
          categoryId: categoryId === '' ? undefined : categoryId,
        })
        setRows(data.items)
        setTotalPages(data.totalPages || 1)
        setError('')
      } catch (e) {
        setError(e instanceof ApiError ? e.message : 'Không tải được sản phẩm')
      }
    })()

    const next = new URLSearchParams()
    if (categoryId) next.set('categoryId', String(categoryId))
    setSearchParams(next, { replace: true })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, search, categoryId])

  return (
    <Box className="mx-auto max-w-6xl px-4 py-8">
      <Stack direction={{ xs: 'column', md: 'row' }} className="mb-6 items-start justify-between gap-4">
        <Box>
          <Typography sx={{ fontWeight: 800, fontSize: 28 }}>
            Siêu thị
          </Typography>
          <Typography sx={{ color: '#5C7A68' }}>Chọn hàng · thêm giỏ · thanh toán nhanh</Typography>
        </Box>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} className="w-full md:w-auto md:min-w-[420px]">
          <TextField
            size="small"
            placeholder="Tìm rau củ, sữa, đồ khô..."
            value={search}
            onChange={(e) => {
              setPage(1)
              setSearch(e.target.value)
            }}
            fullWidth
            slotProps={{
              input: {
                startAdornment: <SearchIcon sx={{ mr: 1, color: '#5C7A68' }} fontSize="small" />,
              },
            }}
            sx={{ bgcolor: '#fff', borderRadius: 2 }}
          />
          <TextField
            select
            size="small"
            label="Danh mục"
            value={categoryId}
            onChange={(e) => {
              setPage(1)
              setCategoryId(e.target.value === '' ? '' : Number(e.target.value))
            }}
            className="sm:w-52"
            sx={{ bgcolor: '#fff', borderRadius: 2 }}
          >
            <MenuItem value="">Tất cả</MenuItem>
            {cats.map((c) => (
              <MenuItem key={c.id} value={c.id}>
                {c.name}
              </MenuItem>
            ))}
          </TextField>
        </Stack>
      </Stack>

      {error ? <Alert>{error}</Alert> : null}

      <div className="grid grid-cols-2 gap-3 md:grid-cols-3 lg:grid-cols-4 md:gap-4">
        {rows.map((p) => {
          const img = p.images?.find((x) => x.isPrimary) || p.images?.[0]
          return (
            <Box
              key={p.id}
              className="flex flex-col overflow-hidden rounded-3xl bg-white transition hover:-translate-y-1"
              sx={{
                border: `1px solid ${storeColors.mist}`,
                boxShadow: '0 10px 28px rgba(11,110,79,0.06)',
              }}
            >
              <Box
                component={RouterLink}
                to={`/store/products/${p.id}`}
                sx={{ height: 170, display: 'block', position: 'relative', textDecoration: 'none' }}
              >
                <StoreProductImage src={img?.imageUrl} alt={p.name} height={170} />
                {p.categoryName ? (
                  <Chip
                    size="small"
                    label={p.categoryName}
                    sx={{
                      position: 'absolute',
                      left: 10,
                      top: 10,
                      bgcolor: 'rgba(255,255,255,0.94)',
                      fontWeight: 700,
                      fontSize: 11,
                    }}
                  />
                ) : null}
              </Box>
              <Box className="flex flex-1 flex-col gap-1.5 p-3">
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
                  {p.sku} · {p.unitSymbol || 'sp'}
                </Typography>
                <Typography sx={{ fontWeight: 800, color: storeColors.coral, fontSize: 17, mt: 0.5 }}>
                  {money(p.price)}
                </Typography>
                <Button
                  fullWidth
                  startIcon={<AddShoppingCartIcon />}
                  onClick={() => {
                    cart.addItem(p, 1)
                    toast.success('Đã thêm vào giỏ')
                  }}
                  sx={{
                    mt: 'auto',
                    bgcolor: storeColors.forest,
                    fontWeight: 800,
                    '&:hover': { bgcolor: storeColors.leaf },
                  }}
                >
                  Mua hàng
                </Button>
              </Box>
            </Box>
          )
        })}
      </div>

      {!rows.length && !error ? (
        <Typography className="py-16 text-center" color="text.secondary">
          Không tìm thấy sản phẩm phù hợp
        </Typography>
      ) : null}

      <Pager page={page} totalPages={totalPages} onChange={setPage} />
    </Box>
  )
}
