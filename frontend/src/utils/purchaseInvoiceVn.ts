import type { ImportReceiptDto } from '../types/api'
import { openPrintHtml, writePrintHtml } from './printWindow'

const BUYER = {
  name: 'BỆNH VIỆN ĐA KHOA DEMO — KHOA DƯỢC / NHÀ THUỐC',
  taxCode: '0100123456',
  address: '123 Nguyễn Trãi, Thanh Xuân, Hà Nội',
  phone: '024 1234 5678',
  bank: 'Vietcombank — STK 0123456789',
}

function money(n: number) {
  return Number(n || 0).toLocaleString('vi-VN')
}

function fmtDate(iso?: string | null) {
  if (!iso) return '—'
  try {
    return new Date(iso).toLocaleString('vi-VN')
  } catch {
    return iso
  }
}

/** Xuất CSV (UTF-8 BOM) — mở bằng Excel. */
export function exportPurchaseInvoiceCsv(detail: ImportReceiptDto) {
  const inv = detail.purchaseInvoiceCode || detail.code
  const lines: string[] = []
  lines.push('HÓA ĐƠN MUA HÀNG (NHẬP KHO DƯỢC)')
  lines.push(`Số HĐ,${csv(inv)}`)
  lines.push(`Số phiếu nhập,${csv(detail.code)}`)
  lines.push(`Ngày,${csv(fmtDate(detail.receiptDate))}`)
  lines.push(`NCC,${csv(detail.supplierName || '')}`)
  lines.push(`MST NCC,${csv(detail.supplierTaxCode || '')}`)
  lines.push(`Địa chỉ NCC,${csv(detail.supplierAddress || '')}`)
  lines.push('')
  lines.push('STT,SKU,Tên thuốc/VT,ĐVT,SL,Đơn giá mua,Thành tiền')
  detail.details.forEach((d, i) => {
    lines.push(
      [
        i + 1,
        csv(d.productSku || ''),
        csv(d.productName || ''),
        csv(d.unitSymbol || ''),
        d.quantity,
        d.unitPrice,
        d.lineTotal,
      ].join(','),
    )
  })
  lines.push('')
  lines.push(`Giá mua hàng,${detail.goodsAmount ?? detail.totalAmount}`)
  lines.push(`Chiết khấu thương mại,${detail.tradeDiscount ?? 0}`)
  lines.push(`Chi phí vận chuyển,${detail.shippingFee ?? 0}`)
  lines.push(`Thuế không được hoàn lại,${detail.nonRefundableTax ?? 0}`)
  lines.push(`Tổng thanh toán,${detail.totalAmount}`)

  const blob = new Blob(['\uFEFF' + lines.join('\n')], { type: 'text/csv;charset=utf-8;' })
  const a = document.createElement('a')
  a.href = URL.createObjectURL(blob)
  a.download = `${inv}.csv`
  a.click()
  URL.revokeObjectURL(a.href)
}

function csv(s: string) {
  if (/[",\n]/.test(s)) return `"${s.replace(/"/g, '""')}"`
  return s
}

/**
 * HTML hóa đơn mua hàng (mẫu VN) — dùng chung cho in sync / sau await.
 */
export function buildPurchaseInvoiceHtml(detail: ImportReceiptDto) {
  const inv = detail.purchaseInvoiceCode || `DRAFT-${detail.code}`
  const goods = Number(detail.goodsAmount ?? 0)
  const discount = Number(detail.tradeDiscount ?? 0)
  const shipping = Number(detail.shippingFee ?? 0)
  const tax = Number(detail.nonRefundableTax ?? 0)
  const payable = Number(detail.totalAmount ?? goods - discount + shipping + tax)

  const rows = detail.details
    .map(
      (d, i) => `<tr>
      <td class="c">${i + 1}</td>
      <td>${esc(d.productSku || '')}</td>
      <td>${esc(d.productName || '')}</td>
      <td class="c">${esc(d.unitSymbol || '')}</td>
      <td class="r">${money(d.quantity)}</td>
      <td class="r">${money(d.unitPrice)}</td>
      <td class="r">${money(d.lineTotal)}</td>
    </tr>`,
    )
    .join('')

  return `<!doctype html>
<html lang="vi"><head><meta charset="utf-8"/>
<title>Hóa đơn ${esc(inv)}</title>
<style>
  *{box-sizing:border-box}
  body{font-family:"Times New Roman",Times,serif;color:#111;padding:18px 22px;font-size:13px}
  .nation{text-align:center;line-height:1.35;margin-bottom:8px}
  .nation .cap{font-weight:700;text-transform:uppercase;letter-spacing:.02em}
  .nation .sub{font-style:italic}
  h1{text-align:center;font-size:18px;margin:10px 0 4px;text-transform:uppercase}
  .inv-meta{text-align:center;margin-bottom:12px}
  .inv-meta div{margin:2px 0}
  .grid2{display:grid;grid-template-columns:1fr 1fr;gap:10px 18px;margin:10px 0 14px}
  .box{border:1px solid #333;padding:8px 10px;min-height:88px}
  .box b{display:inline-block;min-width:72px}
  table{width:100%;border-collapse:collapse;margin-top:6px}
  th,td{border:1px solid #333;padding:5px 6px;vertical-align:top}
  th{background:#f3f3f3;font-size:12px}
  .c{text-align:center}.r{text-align:right}
  .totals{width:100%;margin-top:10px;border-collapse:collapse}
  .totals td{border:none;padding:3px 0}
  .totals .lab{width:70%}.totals .val{width:30%;text-align:right;font-variant-numeric:tabular-nums}
  .totals .grand{font-weight:700;font-size:14px;border-top:1px solid #333;padding-top:6px}
  .signs{display:flex;justify-content:space-between;margin-top:28px;text-align:center}
  .signs .s{width:30%}
  .signs .role{font-weight:700}
  .note{margin-top:8px;font-size:12px;color:#333}
  .toolbar{margin-bottom:12px}
  @media print{.toolbar{display:none} body{padding:0}}
</style></head><body>
  <div class="toolbar">
    <button onclick="window.print()">In hóa đơn</button>
    <button onclick="window.close()">Đóng</button>
  </div>

  <div class="nation">
    <div class="cap">Cộng hòa Xã hội Chủ nghĩa Việt Nam</div>
    <div class="sub">Độc lập – Tự do – Hạnh phúc</div>
    <div>──────── ✦ ────────</div>
  </div>

  <h1>Hóa đơn mua hàng</h1>
  <div class="inv-meta">
    <div><b>Mẫu số:</b> 01GTKT0/001 &nbsp;&nbsp; <b>Ký hiệu:</b> AA/26E</div>
    <div><b>Số:</b> ${esc(inv)} &nbsp;&nbsp; <b>Ngày lập:</b> ${esc(fmtDate(detail.receiptDate))}</div>
    <div><b>Kèm phiếu nhập kho:</b> ${esc(detail.code)} &nbsp;&nbsp; <b>Trạng thái:</b> ${esc(detail.status)}</div>
  </div>

  <div class="grid2">
    <div class="box">
      <div><b>Bên bán (NCC):</b> ${esc(detail.supplierName || '—')}</div>
      <div><b>MST:</b> ${esc(detail.supplierTaxCode || '—')}</div>
      <div><b>Địa chỉ:</b> ${esc(detail.supplierAddress || '—')}</div>
      <div><b>Điện thoại:</b> ${esc(detail.supplierPhone || '—')}</div>
      <div><b>Email:</b> ${esc(detail.supplierEmail || '—')}</div>
    </div>
    <div class="box">
      <div><b>Bên mua:</b> ${esc(BUYER.name)}</div>
      <div><b>MST:</b> ${esc(BUYER.taxCode)}</div>
      <div><b>Địa chỉ:</b> ${esc(BUYER.address)}</div>
      <div><b>Điện thoại:</b> ${esc(BUYER.phone)}</div>
      <div><b>TKNH:</b> ${esc(BUYER.bank)}</div>
      <div><b>Kho nhận:</b> ${esc([detail.warehouseCode, detail.warehouseName].filter(Boolean).join(' — '))}</div>
    </div>
  </div>

  <table>
    <thead>
      <tr>
        <th style="width:36px">STT</th>
        <th style="width:90px">Mã hàng</th>
        <th>Tên thuốc / vật tư y tế</th>
        <th style="width:50px">ĐVT</th>
        <th style="width:70px">Số lượng</th>
        <th style="width:100px">Đơn giá mua</th>
        <th style="width:110px">Thành tiền</th>
      </tr>
    </thead>
    <tbody>${rows}</tbody>
  </table>

  <table class="totals">
    <tr><td class="lab">1. Giá mua hàng</td><td class="val">${money(goods)} đ</td></tr>
    <tr><td class="lab">2. Chiết khấu thương mại</td><td class="val">− ${money(discount)} đ</td></tr>
    <tr><td class="lab">3. Chi phí vận chuyển</td><td class="val">${money(shipping)} đ</td></tr>
    <tr><td class="lab">4. Thuế không được hoàn lại</td><td class="val">${money(tax)} đ</td></tr>
    <tr><td class="lab grand">Tổng cộng thanh toán (1 − 2 + 3 + 4)</td><td class="val grand">${money(payable)} đ</td></tr>
  </table>

  ${detail.note ? `<div class="note"><b>Ghi chú:</b> ${esc(detail.note)}</div>` : ''}
  ${detail.confirmedAt ? `<div class="note"><b>Xác nhận nhập kho:</b> ${esc(fmtDate(detail.confirmedAt))}</div>` : ''}

  <div class="signs">
    <div class="s"><div class="role">Người lập phiếu</div><div><i>(Ký, họ tên)</i></div><br/><br/><br/></div>
    <div class="s"><div class="role">Thủ kho</div><div><i>(Ký, họ tên)</i></div><br/><br/><br/></div>
    <div class="s"><div class="role">Kế toán / Thủ trưởng đơn vị</div><div><i>(Ký, họ tên, đóng dấu)</i></div><br/><br/><br/></div>
  </div>
</body></html>`
}

/** In / xem trước hóa đơn mua hàng. Có thể ghi vào cửa sổ đã mở sẵn (sau await). */
export function printPurchaseInvoiceVn(detail: ImportReceiptDto, target?: Window | null) {
  const html = buildPurchaseInvoiceHtml(detail)
  if (target) writePrintHtml(target, html)
  else openPrintHtml(html, { width: 960, height: 720 })
}

function esc(s: string) {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
}
