import { useEffect, useState } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import {
  Box,
  Button,
  CircularProgress,
  Divider,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { storeApi } from '../../api/endpoints'
import { ApiError } from '../../api/client'
import { Alert, money } from '../../components/ui'
import { storeColors } from '../../store/storeUi'
import type { InvoiceDto } from '../../types/api'

export function StoreInvoice() {
  const { id } = useParams()
  const [invoice, setInvoice] = useState<InvoiceDto | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    if (!id) return
    void storeApi
      .invoice(Number(id))
      .then(setInvoice)
      .catch((e) => setError(e instanceof ApiError ? e.message : 'Không tải được hóa đơn'))
  }, [id])

  if (error) {
    return (
      <Box className="mx-auto max-w-3xl px-4 py-8">
        <Alert>{error}</Alert>
      </Box>
    )
  }

  if (!invoice) {
    return (
      <Box className="flex justify-center py-24">
        <CircularProgress sx={{ color: storeColors.forest }} />
      </Box>
    )
  }

  return (
    <Box className="mx-auto max-w-3xl px-4 py-8">
      <Stack direction={{ xs: 'column', sm: 'row' }} className="mb-4 items-start justify-between gap-3">
        <Box>
          <Typography sx={{ fontWeight: 800, fontSize: 28 }}>
            Hóa đơn {invoice.code}
          </Typography>
          <Typography sx={{ color: '#5C7A68' }}>
            {new Date(invoice.invoiceDate).toLocaleString('vi-VN')} · {invoice.status}
          </Typography>
        </Box>
        <Button component={RouterLink} to="/store/orders" variant="outlined" sx={{ borderColor: storeColors.forest, color: storeColors.forest }}>
          Đơn của tôi
        </Button>
      </Stack>

      <Box
        className="rounded-3xl bg-white p-5"
        sx={{ border: `1px solid ${storeColors.mist}`, boxShadow: '0 12px 32px rgba(11,110,79,0.06)' }}
      >
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={3} className="mb-4">
          <Box className="flex-1">
            <Typography variant="caption" sx={{ color: '#5C7A68', fontWeight: 700 }}>
              KHÁCH HÀNG
            </Typography>
            <Typography sx={{ fontWeight: 800 }}>{invoice.customerName}</Typography>
            {invoice.customerPhone ? <Typography variant="body2">{invoice.customerPhone}</Typography> : null}
            {invoice.customerEmail ? <Typography variant="body2">{invoice.customerEmail}</Typography> : null}
            {invoice.customerAddress ? <Typography variant="body2">{invoice.customerAddress}</Typography> : null}
            {invoice.customerTaxCode ? <Typography variant="body2">MST: {invoice.customerTaxCode}</Typography> : null}
          </Box>
          <Box className="flex-1">
            <Typography variant="caption" sx={{ color: '#5C7A68', fontWeight: 700 }}>
              XUẤT KHO
            </Typography>
            <Typography sx={{ fontWeight: 800 }}>
              {invoice.exportReceiptCode || `#${invoice.exportReceiptId}`}
            </Typography>
            <Typography variant="body2">
              Kho: {invoice.warehouseCode} — {invoice.warehouseName}
            </Typography>
            <Typography variant="body2">Thanh toán: {invoice.paymentMethod}</Typography>
            {invoice.note ? <Typography variant="body2">Ghi chú: {invoice.note}</Typography> : null}
          </Box>
        </Stack>

        <Divider className="mb-3" />

        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>SP</TableCell>
              <TableCell align="right">SL</TableCell>
              <TableCell align="right">Đơn giá</TableCell>
              <TableCell align="right">Thành tiền</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {invoice.details.map((d) => (
              <TableRow key={d.id}>
                <TableCell>
                  <Typography sx={{ fontWeight: 700 }}>{d.productName}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {d.productSku} · {d.unitSymbol || 'sp'}
                  </Typography>
                </TableCell>
                <TableCell align="right">{d.quantity}</TableCell>
                <TableCell align="right">{money(d.unitPrice)}</TableCell>
                <TableCell align="right" sx={{ fontWeight: 700 }}>
                  {money(d.lineTotal)}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>

        <Stack spacing={1} className="mt-4 ml-auto max-w-xs">
          <Stack direction="row" className="justify-between">
            <Typography color="text.secondary">Tạm tính</Typography>
            <Typography sx={{ fontWeight: 700 }}>{money(invoice.subTotal)}</Typography>
          </Stack>
          <Stack direction="row" className="justify-between">
            <Typography color="text.secondary">Phí giao</Typography>
            <Typography sx={{ fontWeight: 700 }}>
              {invoice.shippingFee > 0 ? money(invoice.shippingFee) : 'Miễn phí'}
            </Typography>
          </Stack>
          <Divider />
          <Stack direction="row" className="justify-between">
            <Typography sx={{ fontWeight: 800 }}>Tổng thanh toán</Typography>
            <Typography sx={{ fontWeight: 800, color: storeColors.coral, fontSize: 20 }}>
              {money(invoice.totalAmount)}
            </Typography>
          </Stack>
        </Stack>
      </Box>
    </Box>
  )
}
