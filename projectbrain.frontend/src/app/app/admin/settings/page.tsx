import { RoleGuard } from '@/_components/auth/role-guard';
import SettingsComponent from './_components/settings-component';

import { AppRoles } from '@/_lib/roles';
export default async function AdminSettingsPage() {
    return (
        <RoleGuard allowedRoles={[AppRoles.Admin]}>
            <SettingsComponent />
        </RoleGuard>
    );
}
