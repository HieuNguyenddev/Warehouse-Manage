import { useEffect, useState } from 'react'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import {
  Box,
  Button,
  Divider,
  IconButton,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutlined'
import AddIcon from '@mui/icons-material/Add'
import RemoveIcon from '@mui/icons-material/Remove'
import ShoppingBagOutlinedIcon from '@mui/icons-material/ShoppingBagOutlined'
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutlineOutlined'
import { useCart } from '../../store/CartContext'
import { StoreProductImage, storeColors } from '../../store/storeUi'
import { money } from '../../components/ui'
import { useToast } from '../../components/ToastContext'
import { useAuth } from '../../auth/AuthContext'
import { storeApi } from '../../api/endpoints'
import { ApiError } from '../../api/client'
import type { InvoiceDto, ProductDto } from '../../types/api'

function idsKey(ids: number[]) {
  return [...ids].sort((a, b) => a - b).join(',')
}

export function StoreCart() {
  const cart = useCart()
  const toast = useToast()
  const { isCustomer } = useAuth()
  const nav = useNavigate()
  const [note, setNote] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [done, setDone] = useState<InvoiceDto | null>(null)

  // Đồng bộ giá bán trong giỏ theo catalog store (sau khi admin Confirm phiếu nhập đổi giá)
  useEffect(() => {
    if (!cart.items.length) return
    const ids = cart.items.map((x) => x.productId)
    void (async () => {
      try {
        const products: ProductDto[] = []
        await Promise.all(
          ids.map(async (id) => {
            try {
              products.push(await storeApi.product(id))
            } catch {
              /* SP có thể đã ngừng bán */
            }
          }),
        )
        if (products.length) cart.syncPrices(products)
      } catch {
        /* giữ giá cũ trong giỏ */
      }
    })()
    // chỉ chạy lại khi đổi tập SP trong giỏ
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [idsKey(cart.items.map((x) => x.productId))])

  const shipping = cart.totalAmount >= 300000 ? 0 : 15000
  const grandTotal = cart.totalAmount + shipping

  async function checkout() {
    if (!cart.items.length || submitting) return
    if (!isCustomer) {
      toast.error('Vui lòng đăng nhập để đặt hàng')
      nav('/store/login')
      return
    }

    setSubmitting(true)
    try {
      const result = await storeApi.checkout({
        items: cart.items.map((x) => ({ productId: x.productId, quantity: x.quantity })),
        note: note.trim() || null,
      })
      cart.clear()
      setDone(result.invoice)
      toast.success(`Đã ghi phiếu xuất ${result.exportReceipt.code} và trừ tồn kho`)
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : 'Đặt hàng thất bại')
    } finally {
      setSubmitting(false)
    }
  }

  if (done) {
    return (
      <Box className="mx-auto max-w-xl px-4 py-16 text-center">
        <CheckCircleOutlineIcon sx={{ fontSize: 72, color: storeColors.forest }} />
        <Typography sx={{ fontWeight: 800, fontSize: 28, mt: 2 }}>
          Đặt hàng thành công
        </Typography>
        <Typography className="mt-2" sx={{ color: '#5C7A68' }}>
          Hóa đơn <strong>{done.code}</strong> · Phiếu xuất <strong>{done.exportReceiptCode}</strong>
        </Typography>
        <Typography className="mt-1" sx={{ color: '#5C7A68' }}>
          Kho {done.warehouseCode} · {done.totalQuantity} sp · {money(done.totalAmount)}
        </Typography>
        <Typography variant="body2" className="mt-2" sx={{ color: '#5C7A68' }}>
          Đơn đã ghi vào xuất kho ({done.exportReceiptCode}), trừ tồn kho và lập hóa đơn.
        </Typography>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} className="mt-6 justify-center">
          <Button
            variant="contained"
            onClick={() => nav(`/store/invoices/${done.id}`)}
            sx={{ bgcolor: storeColors.forest, '&:hover': { bgcolor: storeColors.leaf } }}
          >
            Xem hóa đơn
          </Button>
          <Button
            variant="outlined"
            onClick={() => nav('/store/products')}
            sx={{ borderColor: storeColors.forest, color: storeColors.forest }}
          >
            Tiếp tục mua
          </Button>
        </Stack>
      </Box>
    )
  }

  return (
    <Box className="mx-auto max-w-6xl px-4 py-8">
      <Typography sx={{ fontWeight: 800, fontSize: 28 }}>
        Giỏ hàng
      </Typography>
      <Typography sx={{ color: '#5C7A68', mb: 3 }}>
        {cart.totalQty} sản phẩm trong giỏ
      </Typography>

      {!cart.items.length ? (
        <Box
          className="rounded-3xl bg-white py-16 text-center"
          sx={{ border: `1px solid ${storeColors.mist}` }}
        >
          <ShoppingBagOutlinedIcon sx={{ fontSize: 56, color: '#9BB5A6' }} />
          <Typography className="mt-2" color="text.secondary">
            Giỏ hàng trống
          </Typography>
          <Button
            className="mt-4"
            component={RouterLink}
            to="/store/products"
            variant="contained"
            sx={{ bgcolor: storeColors.forest, '&:hover': { bgcolor: storeColors.leaf } }}
          >
            Đi mua sắm
          </Button>
        </Box>
      ) : (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-[1fr_340px]">
          <Stack spacing={1.5}>
            {cart.items.map((item) => (
              <Box
                key={item.productId}
                className="flex gap-3 rounded-3xl bg-white p-3"
                sx={{ border: `1px solid ${storeColors.mist}` }}
              >
                <Box
                  component={RouterLink}
                  to={`/store/products/${item.productId}`}
                  className="h-24 w-24 overflow-hidden rounded-2xl"
                >
                  <StoreProductImage src={item.imageUrl} alt={item.name} height={96} />
                </Box>
                <Box className="min-w-0 flex-1">
                  <Typography
                    component={RouterLink}
                    to={`/store/products/${item.productId}`}
                    sx={{ fontWeight: 800, color: storeColors.ink, textDecoration: 'none' }}
                  >
                    {item.name}
                  </Typography>
                  <Typography variant="body2" sx={{ color: '#5C7A68' }}>
                    {item.sku} · {money(item.price)} / {item.unitSymbol || 'sp'}
                  </Typography>
                  <Stack direction="row" spacing={0.5} className="mt-2 items-center">
                    <IconButton size="small" onClick={() => cart.setQty(item.productId, item.quantity - 1)}>
                      <RemoveIcon fontSize="small" />
                    </IconButton>
                    <Typography sx={{ minWidth: 24, textAlign: 'center', fontWeight: 800 }}>
                      {item.quantity}
                    </Typography>
                    <IconButton size="small" onClick={() => cart.setQty(item.productId, item.quantity + 1)}>
                      <AddIcon fontSize="small" />
                    </IconButton>
                    <IconButton color="error" size="small" onClick={() => cart.removeItem(item.productId)}>
                      <DeleteOutlineIcon fontSize="small" />
                    </IconButton>
                  </Stack>
                </Box>
                <Typography sx={{ fontWeight: 800, color: storeColors.coral }}>
                  {money(item.price * item.quantity)}
                </Typography>
              </Box>
            ))}
          </Stack>

          <Box
            className="h-fit rounded-3xl bg-white p-4"
            sx={{ border: `1px solid ${storeColors.mist}`, position: 'sticky', top: 88 }}
          >
            <Typography sx={{ fontWeight: 800, mb: 2 }}>
              Thanh toán
            </Typography>
            <Stack spacing={1} className="mb-3">
              <Stack direction="row" className="justify-between">
                <Typography color="text.secondary">Tạm tính</Typography>
                <Typography sx={{ fontWeight: 700 }}>{money(cart.totalAmount)}</Typography>
              </Stack>
              <Stack direction="row" className="justify-between">
                <Typography color="text.secondary">Phí giao</Typography>
                <Typography sx={{ fontWeight: 700, color: storeColors.forest }}>
                  {shipping === 0 ? 'Miễn phí' : money(shipping)}
                </Typography>
              </Stack>
              <Divider />
              <Stack direction="row" className="justify-between">
                <Typography sx={{ fontWeight: 800 }}>Tổng cộng</Typography>
                <Typography sx={{ fontWeight: 800, color: storeColors.coral, fontSize: 20 }}>
                  {money(grandTotal)}
                </Typography>
              </Stack>
            </Stack>
            <TextField
              fullWidth
              size="small"
              multiline
              minRows={2}
              label="Ghi chú giao hàng"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              className="mb-3"
            />
            {!isCustomer ? (
              <Typography variant="body2" sx={{ color: storeColors.coral, mb: 1.5 }}>
                Cần đăng nhập khách hàng để trừ kho và xuất hóa đơn.
              </Typography>
            ) : null}
            <Button
              fullWidth
              size="large"
              variant="contained"
              disabled={submitting}
              onClick={() => void checkout()}
              sx={{
                bgcolor: storeColors.coral,
                fontWeight: 800,
                py: 1.3,
                '&:hover': { bgcolor: '#d04f00' },
              }}
            >
              {submitting ? 'Đang xử lý...' : isCustomer ? 'Đặt hàng' : 'Đăng nhập để đặt hàng'}
            </Button>
            <Button
              fullWidth
              className="mt-2"
              onClick={() => cart.clear()}
              sx={{ color: '#5C7A68' }}
            >
              Xóa giỏ hàng
            </Button>
          </Box>
        </div>
      )}
    </Box>
  )
}
