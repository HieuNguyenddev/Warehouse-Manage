import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings'
import ManageAccountsIcon from '@mui/icons-material/ManageAccounts'
import CategoryIcon from '@mui/icons-material/Category'
import StraightenIcon from '@mui/icons-material/Straighten'
import LocalShippingIcon from '@mui/icons-material/LocalShipping'
import WarehouseIcon from '@mui/icons-material/Warehouse'
import { categoriesApi, rolesApi, suppliersApi, unitsApi, usersApi, warehousesApi } from '../../api/endpoints'
import { ResourcePage } from '../../components/ResourcePage'
import { Badge } from '../../components/ui'
import type { CategoryDto, RoleDto, SupplierDto, UnitDto, UserDto, WarehouseDto } from '../../types/api'

export function RolesPage() {
  return (
    <ResourcePage<RoleDto>
      title="Vai trò"
      subtitle="Phân quyền kho dược bệnh viện"
      icon={<AdminPanelSettingsIcon color="primary" />}
      list={rolesApi.list}
      create={rolesApi.create}
      update={rolesApi.update}
      remove={rolesApi.remove}
      columns={[
        { key: 'id', header: 'ID' },
        { key: 'name', header: 'Tên' },
        { key: 'description', header: 'Mô tả' },
      ]}
      fields={[
        { name: 'name', label: 'Tên role', required: true },
        { name: 'description', label: 'Mô tả', type: 'textarea' },
      ]}
    />
  )
}

export function UsersPage() {
  return (
    <ResourcePage<UserDto>
      title="Người dùng"
      subtitle="Tài khoản nội bộ khoa dược / kho thuốc"
      icon={<ManageAccountsIcon color="primary" />}
      list={usersApi.list}
      create={usersApi.create}
      update={usersApi.update}
      remove={usersApi.remove}
      toForm={(r) => ({
        username: r.username,
        fullName: r.fullName,
        email: r.email,
        phone: r.phone || '',
        roleId: r.roleId,
        isActive: r.isActive,
        password: '',
      })}
      columns={[
        { key: 'id', header: 'ID' },
        { key: 'username', header: 'Username' },
        { key: 'fullName', header: 'Họ tên' },
        { key: 'email', header: 'Email' },
        { key: 'roleName', header: 'Role' },
        {
          key: 'isActive',
          header: 'TT',
          render: (r) => <Badge tone={r.isActive ? 'ok' : 'danger'}>{r.isActive ? 'Active' : 'Off'}</Badge>,
        },
      ]}
      fields={[
        { name: 'username', label: 'Username', required: true },
        { name: 'password', label: 'Password (để trống nếu không đổi)', type: 'text' },
        { name: 'fullName', label: 'Họ tên', required: true },
        { name: 'email', label: 'Email', required: true },
        { name: 'phone', label: 'Phone' },
        { name: 'roleId', label: 'RoleId', type: 'number', required: true },
        { name: 'isActive', label: 'Active', type: 'checkbox' },
      ]}
    />
  )
}

export function CategoriesPage() {
  return (
    <ResourcePage<CategoryDto>
      title="Nhóm thuốc"
      subtitle="Phân nhóm thuốc / vật tư kho nhà thuốc bệnh viện"
      icon={<CategoryIcon color="primary" />}
      list={categoriesApi.list}
      create={categoriesApi.create}
      update={categoriesApi.update}
      remove={categoriesApi.remove}
      columns={[
        { key: 'id', header: 'ID' },
        { key: 'name', header: 'Tên' },
        { key: 'description', header: 'Mô tả' },
        {
          key: 'isActive',
          header: 'TT',
          render: (r) => <Badge tone={r.isActive ? 'ok' : 'danger'}>{r.isActive ? 'Active' : 'Off'}</Badge>,
        },
      ]}
      fields={[
        { name: 'name', label: 'Tên', required: true },
        { name: 'description', label: 'Mô tả', type: 'textarea' },
        { name: 'isActive', label: 'Active', type: 'checkbox' },
      ]}
    />
  )
}

export function UnitsPage() {
  return (
    <ResourcePage<UnitDto>
      title="Đơn vị tính"
      subtitle="Đơn vị đóng gói thuốc / vật tư (viên, vỉ, hộp, ống…)"
      icon={<StraightenIcon color="primary" />}
      list={unitsApi.list}
      create={unitsApi.create}
      update={unitsApi.update}
      remove={unitsApi.remove}
      columns={[
        { key: 'id', header: 'ID' },
        { key: 'name', header: 'Tên' },
        { key: 'symbol', header: 'Ký hiệu' },
        {
          key: 'isActive',
          header: 'TT',
          render: (r) => <Badge tone={r.isActive ? 'ok' : 'danger'}>{r.isActive ? 'Active' : 'Off'}</Badge>,
        },
      ]}
      fields={[
        { name: 'name', label: 'Tên', required: true },
        { name: 'symbol', label: 'Symbol', required: true },
        { name: 'description', label: 'Mô tả', type: 'textarea' },
        { name: 'isActive', label: 'Active', type: 'checkbox' },
      ]}
    />
  )
}

export function SuppliersPage() {
  return (
    <ResourcePage<SupplierDto>
      title="NCC dược"
      subtitle="Nhà cung cấp thuốc và vật tư y tế"
      icon={<LocalShippingIcon color="primary" />}
      list={suppliersApi.list}
      create={suppliersApi.create}
      update={suppliersApi.update}
      remove={suppliersApi.remove}
      columns={[
        { key: 'id', header: 'ID' },
        { key: 'name', header: 'Tên NCC' },
        { key: 'contactName', header: 'Liên hệ' },
        { key: 'phone', header: 'Phone' },
        { key: 'email', header: 'Email' },
      ]}
      fields={[
        { name: 'name', label: 'Tên', required: true },
        { name: 'contactName', label: 'Người liên hệ' },
        { name: 'phone', label: 'Phone' },
        { name: 'email', label: 'Email' },
        { name: 'address', label: 'Địa chỉ', type: 'textarea' },
        { name: 'taxCode', label: 'MST' },
        { name: 'note', label: 'Ghi chú', type: 'textarea' },
        { name: 'isActive', label: 'Active', type: 'checkbox' },
      ]}
    />
  )
}

export function WarehousesPage() {
  return (
    <ResourcePage<WarehouseDto>
      title="Kho dược"
      subtitle="Kho thuốc thường / lạnh / gây nghiện / vật tư trong bệnh viện"
      icon={<WarehouseIcon color="primary" />}
      list={warehousesApi.list}
      create={warehousesApi.create}
      update={warehousesApi.update}
      remove={warehousesApi.remove}
      columns={[
        { key: 'id', header: 'ID' },
        { key: 'code', header: 'Mã' },
        { key: 'name', header: 'Tên kho' },
        { key: 'managerName', header: 'Quản lý' },
        { key: 'phone', header: 'Phone' },
      ]}
      fields={[
        { name: 'code', label: 'Mã kho', required: true },
        { name: 'name', label: 'Tên kho', required: true },
        { name: 'address', label: 'Địa chỉ', type: 'textarea' },
        { name: 'phone', label: 'Phone' },
        { name: 'managerName', label: 'Người quản lý' },
        { name: 'note', label: 'Ghi chú', type: 'textarea' },
        { name: 'isActive', label: 'Active', type: 'checkbox' },
      ]}
    />
  )
}
