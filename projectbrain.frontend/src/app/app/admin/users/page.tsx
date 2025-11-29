import { RoleGuard } from '@/_components/auth/role-guard';
import UserManagementComponent from './_components/user-management-component';

export default async function AdminUsersPage() {
    return (
        <RoleGuard allowedRoles={['admin']}>
            <UserManagementComponent />
        </RoleGuard>
    );
}
