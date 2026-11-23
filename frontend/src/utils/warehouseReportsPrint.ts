import { openPrintHtml } from './printWindow'

export type ReportColumn = { key: string; label: string; align?: 'left' | 'right' | 'center' }

const ORG = {
  unit: 'BỆNH VIỆN ĐA KHOA DEMO',
  dept: 'KHOA DƯỢC / NHÀ THUỐC BỆNH VIỆN',
  address: '123 Nguyễn Trãi, Thanh Xuân, Hà Nội',
}

function esc(s: string) {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
}

function money(n: number) {
  return Number(n || 0).toLocaleString('vi-VN')
}

export function printWarehouseReport(opts: {
  reportCode: string
  title: string
  subtitle?: string
  periodLabel: string
  warehouseLabel: string
  columns: ReportColumn[]
  rows: Record<string, string | number>[]
  footerNote?: string
  summaryLines?: string[]
}) {
  const {
    reportCode,
    title,
    subtitle,
    periodLabel,
    warehouseLabel,
    columns,
    rows,
    footerNote,
    summaryLines,
  } = opts

  const head = columns
    .map(
      (c) =>
        `<th style="text-align:${c.align || 'left'}">${esc(c.label)}</th>`,
    )
    .join('')

  const body = rows
    .map((r, i) => {
      const cells = columns
        .map((c) => {
          const raw = c.key === 'stt' ? i + 1 : r[c.key]
          const moneyKeys = new Set(['unitPrice', 'lineTotal', 'value'])
          let val: string
          if (typeof raw === 'number') {
            val = moneyKeys.has(c.key) ? `${money(raw)} đ` : Number(raw).toLocaleString('vi-VN')
          } else {
            val = String(raw ?? '')
          }
          return `<td style="text-align:${c.align || 'left'}">${esc(val)}</td>`
        })
        .join('')
      return `<tr>${cells}</tr>`
    })
    .join('')

  const summaryHtml = (summaryLines || [])
    .map((l) => `<div>${esc(l)}</div>`)
    .join('')

  const html = `<!doctype html>
<html lang="vi"><head><meta charset="utf-8"/>
<title>${esc(title)}</title>
<style>
  *{box-sizing:border-box}
  body{font-family:"Times New Roman",Times,serif;color:#111;padding:18px 22px;font-size:12.5px}
  .nation{text-align:center;line-height:1.35;margin-bottom:6px}
  .nation .cap{font-weight:700;text-transform:uppercase}
  .nation .sub{font-style:italic}
  .org{text-align:left;margin:8px 0 4px;font-size:12px}
  h1{text-align:center;font-size:16px;margin:10px 0 4px;text-transform:uppercase}
  .meta{text-align:center;margin-bottom:12px;font-size:12px}
  .meta div{margin:2px 0}
  table{width:100%;border-collapse:collapse;margin-top:8px}
  th,td{border:1px solid #222;padding:5px 6px;vertical-align:top}
  th{background:#f2f2f2;font-size:11.5px}
  .sum{margin-top:10px;font-size:12.5px}
  .note{margin-top:8px;font-size:11.5px;color:#333}
  .signs{display:flex;justify-content:space-between;margin-top:28px;text-align:center}
  .signs .s{width:30%}
  .signs .role{font-weight:700}
  .toolbar{margin-bottom:12px}
  @media print{.toolbar{display:none} body{padding:0}}
</style></head><body>
  <div class="toolbar">
    <button onclick="window.print()">In báo cáo</button>
    <button onclick="window.close()">Đóng</button>
  </div>

  <div class="nation">
    <div class="cap">Cộng hòa Xã hội Chủ nghĩa Việt Nam</div>
    <div class="sub">Độc lập – Tự do – Hạnh phúc</div>
    <div>──────── ✦ ────────</div>
  </div>

  <div class="org">
    <div><b>Đơn vị:</b> ${esc(ORG.unit)}</div>
    <div><b>Bộ phận:</b> ${esc(ORG.dept)}</div>
    <div><b>Địa chỉ:</b> ${esc(ORG.address)}</div>
  </div>

  <h1>${esc(title)}</h1>
  ${subtitle ? `<div class="meta"><i>${esc(subtitle)}</i></div>` : ''}
  <div class="meta">
    <div><b>Mã báo cáo:</b> ${esc(reportCode)} &nbsp;&nbsp; <b>Ngày lập:</b> ${esc(new Date().toLocaleString('vi-VN'))}</div>
    <div><b>Kỳ báo cáo:</b> ${esc(periodLabel)}</div>
    <div><b>Phạm vi kho:</b> ${esc(warehouseLabel)}</div>
  </div>

  <table>
    <thead><tr>${head}</tr></thead>
    <tbody>${body || `<tr><td colspan="${columns.length}" style="text-align:center">Không có dữ liệu</td></tr>`}</tbody>
  </table>

  ${summaryHtml ? `<div class="sum">${summaryHtml}</div>` : ''}
  ${footerNote ? `<div class="note"><b>Ghi chú:</b> ${esc(footerNote)}</div>` : ''}

  <div class="signs">
    <div class="s"><div class="role">Người lập biểu</div><div><i>(Ký, họ tên)</i></div><br/><br/><br/></div>
    <div class="s"><div class="role">Thủ kho</div><div><i>(Ký, họ tên)</i></div><br/><br/><br/></div>
    <div class="s"><div class="role">Thủ trưởng đơn vị</div><div><i>(Ký, họ tên, đóng dấu)</i></div><br/><br/><br/></div>
  </div>
</body></html>`

  openPrintHtml(html, { width: 1000, height: 740 })
}
