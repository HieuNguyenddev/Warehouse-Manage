import { useEffect, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { Box, Button, Stack, Typography } from '@mui/material'
import ReceiptLongIcon from '@mui/icons-material/ReceiptLong'
import { storeApi } from '../../api/endpoints'
import { ApiError } from '../../api/client'
import { Alert, money, Pager } from '../../components/ui'
import { storeColors } from '../../store/storeUi'
import type { InvoiceDto } from '../../types/api'

export function StoreOrders() {
  const [rows, setRows] = useState<InvoiceDto[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [error, setError] = useState('')

  useEffect(() => {
    void (async () => {
      try {
        const data = await storeApi.invoices({ pageNumber: page, pageSize: 10 })
        setRows(data.items)
        setTotalPages(data.totalPages || 1)
        setError('')
      } catch (e) {
        setError(e instanceof ApiError ? e.message : 'Không tải được đơn hàng')
      }
    })()
  }, [page])

  return (
    <Box className="mx-auto max-w-4xl px-4 py-8">
      <Typography sx={{ fontFamily: '"Sora", sans-serif', fontWeight: 800, fontSize: 28 }}>
        Đơn hàng của tôi
      </Typography>
      <Typography sx={{ color: '#5C7A68', mb: 3 }}>Hóa đơn và phiếu xuất kho đã phát hành</Typography>

      {error ? <Alert>{error}</Alert> : null}

      <Stack spacing={1.5}>
        {rows.map((inv) => (
          <Box
            key={inv.id}
            className="rounded-3xl bg-white p-4"
            sx={{ border: `1px solid ${storeColors.mist}` }}
          >
            <Stack direction={{ xs: 'column', sm: 'row' }} className="items-start justify-between gap-3">
              <Box>
                <Stack direction="row" spacing={1} className="items-center">
                  <ReceiptLongIcon sx={{ color: storeColors.forest }} fontSize="small" />
                  <Typography sx={{ fontWeight: 800 }}>{inv.code}</Typography>
                </Stack>
                <Typography variant="body2" sx={{ color: '#5C7A68', mt: 0.5 }}>
                  {new Date(inv.invoiceDate).toLocaleString('vi-VN')} · PX {inv.exportReceiptCode} ·{' '}
                  {inv.warehouseName}
                </Typography>
                <Typography variant="body2" sx={{ color: '#5C7A68' }}>
                  {inv.totalQuantity} sp · {inv.details.length} dòng
                </Typography>
              </Box>
              <Box className="text-right">
                <Typography sx={{ fontWeight: 800, color: storeColors.coral, fontSize: 18 }}>
                  {money(inv.totalAmount)}
                </Typography>
                <Button
                  component={RouterLink}
                  to={`/store/invoices/${inv.id}`}
                  size="small"
                  sx={{ mt: 1, color: storeColors.forest, fontWeight: 700 }}
                >
                  Xem hóa đơn
                </Button>
              </Box>
            </Stack>
          </Box>
        ))}
      </Stack>

      {!rows.length && !error ? (
        <Typography className="py-16 text-center" color="text.secondary">
          Chưa có đơn hàng nào
        </Typography>
      ) : null}

      <Pager page={page} totalPages={totalPages} onChange={setPage} />
    </Box>
  )
}
