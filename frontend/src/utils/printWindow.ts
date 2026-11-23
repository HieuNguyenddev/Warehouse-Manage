/**
 * Mở cửa sổ in và ghi HTML.
 * Không dùng feature "noopener" — trình duyệt sẽ trả null và để lại tab about:blank trống.
 */
export function openPrintHtml(html: string, opts?: { width?: number; height?: number; autoPrint?: boolean }) {
  const width = opts?.width ?? 960
  const height = opts?.height ?? 720
  const left = Math.max(0, Math.round(window.screenX + (window.outerWidth - width) / 2))
  const top = Math.max(0, Math.round(window.screenY + (window.outerHeight - height) / 2))

  const w = window.open(
    '',
    '_blank',
    `width=${width},height=${height},left=${left},top=${top},menubar=no,toolbar=no,location=no,status=no,scrollbars=yes,resizable=yes`,
  )

  if (!w) {
    // Popup bị chặn — fallback: blob URL cùng tab gesture
    const blob = new Blob([html], { type: 'text/html;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.target = '_blank'
    a.rel = 'noopener'
    a.click()
    setTimeout(() => URL.revokeObjectURL(url), 60_000)
    return null
  }

  w.document.open()
  w.document.write(html)
  w.document.close()
  try {
    w.focus()
  } catch {
    /* ignore */
  }
  // Giảm rủi ro tab mới điều khiển được opener
  try {
    ;(w as Window & { opener: Window | null }).opener = null
  } catch {
    /* ignore */
  }
  return w
}

/** Mở cửa sổ trống ngay trong click handler (trước await), trả về để ghi HTML sau. */
export function openPrintShell(opts?: { width?: number; height?: number }) {
  const width = opts?.width ?? 960
  const height = opts?.height ?? 720
  const w = window.open(
    '',
    '_blank',
    `width=${width},height=${height},menubar=no,toolbar=no,location=no,status=no,scrollbars=yes,resizable=yes`,
  )
  if (w) {
    w.document.write(
      `<!doctype html><html><head><meta charset="utf-8"/><title>Đang tải...</title></head>
       <body style="font-family:system-ui,sans-serif;padding:24px;color:#334155">
         <p>Đang tải phiếu in…</p>
       </body></html>`,
    )
    w.document.close()
  }
  return w
}

export function writePrintHtml(w: Window, html: string) {
  w.document.open()
  w.document.write(html)
  w.document.close()
  try {
    w.focus()
  } catch {
    /* ignore */
  }
  try {
    ;(w as Window & { opener: Window | null }).opener = null
  } catch {
    /* ignore */
  }
}
