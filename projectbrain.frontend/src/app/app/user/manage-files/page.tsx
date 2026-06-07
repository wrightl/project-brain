import { RoleGuard } from '@/_components/auth/role-guard';
import ManageFilesComponent from '@/_components/manage-files/manage-files';

import { AppRoles } from '@/_lib/roles';
export default async function ManageFilesPage() {
    return (
        <RoleGuard allowedRoles={[AppRoles.User]}>
            <ManageFilesComponent manageSharedFiles={false} />
        </RoleGuard>
    );
}
