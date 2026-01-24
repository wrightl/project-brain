import { RoleGuard } from '@/_components/auth/role-guard';
import SettingsComponent from './_components/settings-component';

export default async function AdminSettingsPage() {
    return (
        <RoleGuard allowedRoles={['admin']}>
            <SettingsComponent />
        </RoleGuard>
    );
}
