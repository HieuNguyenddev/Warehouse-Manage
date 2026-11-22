import { useRef, useState } from 'react'
import { IconButton, Tooltip } from '@mui/material'
import GroupsIcon from '@mui/icons-material/Groups'
import PhotoCameraIcon from '@mui/icons-material/PhotoCamera'
import { customersApi } from '../../api/endpoints'
import { mediaUrl } from '../../api/client'
import { ResourcePage } from '../../components/ResourcePage'
import { useToast } from '../../components/ToastContext'
import { Badge } from '../../components/ui'
import type { CustomerDto } from '../../types/api'

export function CustomersPage() {
  const toast = useToast()
  const fileRef = useRef<HTMLInputElement>(null)
  const [uploadId, setUploadId] = useState<number | null>(null)
  const reloadRef = useRef<(() => void) | null>(null)

  return (
    <>
      <input
        ref={fileRef}
        type="file"
        accept="image/*"
        hidden
        onChange={async (e) => {
          const file = e.target.files?.[0]
          if (!file || !uploadId) return
          try {
            await customersApi.uploadAvatar(uploadId, file)
            toast.success('Đã cập nhật avatar')
            reloadRef.current?.()
          } catch {
            toast.error('Upload avatar thất bại')
          }
          e.target.value = ''
          setUploadId(null)
        }}
      />
      <ResourcePage<CustomerDto>
        title="Customers"
        subtitle="Khách hàng + tài khoản Customer (tuỳ chọn)"
        icon={<GroupsIcon color="primary" />}
        list={customersApi.list}
        create={customersApi.create}
        update={customersApi.update}
        remove={customersApi.remove}
        toForm={(r) => ({
          name: r.name,
          contactName: r.contactName || '',
          phone: r.phone || '',
          email: r.email || '',
          address: r.address || '',
          taxCode: r.taxCode || '',
          note: r.note || '',
          isActive: r.isActive,
          username: '',
          password: '',
        })}
        columns={[
          { key: 'id', header: 'ID' },
          {
            key: 'avatarUrl',
            header: 'Avatar',
            render: (r) =>
              r.avatarUrl ? (
                <img src={mediaUrl(r.avatarUrl)} alt="" width={36} height={36} style={{ borderRadius: 8, objectFit: 'cover' }} />
              ) : (
                '—'
              ),
          },
          { key: 'name', header: 'Tên' },
          { key: 'phone', header: 'Phone' },
          { key: 'username', header: 'Username' },
          {
            key: 'isActive',
            header: 'TT',
            render: (r) => <Badge tone={r.isActive ? 'ok' : 'danger'}>{r.isActive ? 'Active' : 'Off'}</Badge>,
          },
        ]}
        fields={[
          { name: 'name', label: 'Tên KH', required: true },
          { name: 'contactName', label: 'Người liên hệ' },
          { name: 'phone', label: 'Phone' },
          { name: 'email', label: 'Email' },
          { name: 'address', label: 'Địa chỉ', type: 'textarea' },
          { name: 'taxCode', label: 'MST' },
          { name: 'note', label: 'Ghi chú', type: 'textarea' },
          { name: 'username', label: 'Username (tạo tài khoản Customer)' },
          { name: 'password', label: 'Password' },
          { name: 'isActive', label: 'Active', type: 'checkbox' },
        ]}
        extraActions={(row, reload) => {
          reloadRef.current = reload
          return (
            <Tooltip title="Upload avatar">
              <IconButton
                size="small"
                color="secondary"
                onClick={() => {
                  setUploadId(row.id)
                  fileRef.current?.click()
                }}
              >
                <PhotoCameraIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          )
        }}
      />
    </>
  )
}
