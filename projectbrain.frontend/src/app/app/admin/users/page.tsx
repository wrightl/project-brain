import { RoleGuard } from '@/_components/auth/role-guard';
import UserManagementComponent from './_components/user-management-component';

import { AppRoles } from '@/_lib/roles';
export default async function AdminUsersPage() {
    return (
        <RoleGuard allowedRoles={[AppRoles.Admin]}>
            <UserManagementComponent />
        </RoleGuard>
    );
}
