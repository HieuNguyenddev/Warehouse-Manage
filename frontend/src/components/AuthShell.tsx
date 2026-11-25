import type { ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { Link } from '@mui/material'
import AssignmentTurnedInRoundedIcon from '@mui/icons-material/AssignmentTurnedInRounded'
import Inventory2RoundedIcon from '@mui/icons-material/Inventory2Rounded'
import LocalPharmacyRoundedIcon from '@mui/icons-material/LocalPharmacyRounded'
import SecurityRoundedIcon from '@mui/icons-material/SecurityRounded'
import StorefrontIcon from '@mui/icons-material/Storefront'
import WarehouseIcon from '@mui/icons-material/Warehouse'
import '../styles/auth.css'

type Portal = 'admin' | 'customer'

interface AuthShellProps {
  portal: Portal
  headline: string
  description: string
  switchPrompt: string
  switchLabel: string
  switchTo: string
  children: ReactNode
}

const portalContent = {
  admin: {
    name: 'Cổng nhân viên',
    role: 'QUẢN TRỊ KHO DƯỢC',
    icon: WarehouseIcon,
    highlights: [
      { icon: Inventory2RoundedIcon, text: 'Theo dõi hàng hóa và tồn kho tập trung' },
      { icon: AssignmentTurnedInRoundedIcon, text: 'Quản lý nhập, xuất và kiểm kê rõ ràng' },
      { icon: SecurityRoundedIcon, text: 'Phân quyền thao tác theo vai trò' },
    ],
  },
  customer: {
    name: 'Cổng nhà thuốc',
    role: 'DÀNH CHO KHOA / PHÒNG',
    icon: StorefrontIcon,
    highlights: [
      { icon: LocalPharmacyRoundedIcon, text: 'Tra cứu danh mục thuốc và vật tư' },
      { icon: AssignmentTurnedInRoundedIcon, text: 'Gửi yêu cầu nhận thuốc thuận tiện' },
      { icon: SecurityRoundedIcon, text: 'Theo dõi thông tin tài khoản an toàn' },
    ],
  },
}

export function AuthShell({
  portal,
  headline,
  description,
  switchPrompt,
  switchLabel,
  switchTo,
  children,
}: AuthShellProps) {
  const content = portalContent[portal]
  const PortalIcon = content.icon

  return (
    <main className={`auth-page auth-page--${portal}`}>
      <section className="auth-card" aria-label={`Đăng nhập ${content.name.toLowerCase()}`}>
        <aside className="auth-aside">
          <div className="auth-brand">
            <span className="auth-brand__mark">
              <WarehouseIcon aria-hidden="true" />
            </span>
            <span className="auth-brand__text">
              Kho Dược
              <small>HỆ THỐNG NHÀ THUỐC BỆNH VIỆN</small>
            </span>
          </div>

          <div className="auth-aside__content">
            <span className="auth-eyebrow">
              <span className="auth-eyebrow__dot" />
              {content.role}
            </span>
            <h1>{headline}</h1>
            <p className="auth-aside__description">{description}</p>

            <ul className="auth-highlights">
              {content.highlights.map(({ icon: HighlightIcon, text }) => (
                <li key={text}>
                  <span className="auth-highlight__icon">
                    <HighlightIcon aria-hidden="true" />
                  </span>
                  <span>{text}</span>
                </li>
              ))}
            </ul>
          </div>

          <p className="auth-aside__footer">ĐỒNG HÀNH CÙNG CÔNG TÁC CHĂM SÓC SỨC KHỎE</p>
        </aside>

        <div className="auth-main">
          <div className="auth-mobile-brand">
            <span className="auth-brand__mark">
              <PortalIcon aria-hidden="true" />
            </span>
            <span className="auth-brand__text">
              Kho Dược
              <small>{content.name.toUpperCase()}</small>
            </span>
          </div>

          <div className="auth-form-heading">
            <span className="auth-form-heading__eyebrow">{content.name}</span>
            <h2>Đăng nhập</h2>
            <p>Sử dụng tài khoản của bạn để tiếp tục.</p>
          </div>

          {children}

          <div className="auth-switch">
            <span>{switchPrompt}</span>
            <Link component={RouterLink} to={switchTo} underline="hover">
              {switchLabel}
            </Link>
          </div>

          <div className="auth-security-note">
            <SecurityRoundedIcon aria-hidden="true" />
            <span>Thông tin đăng nhập được bảo vệ an toàn</span>
          </div>
        </div>
      </section>
    </main>
  )
}
