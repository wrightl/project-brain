import { RoleGuard } from '@/_components/auth/role-guard';
import ManageFilesComponent from '@/_components/manage-files/manage-files';

import { AppRoles } from '@/_lib/roles';
export default async function AdminUploadPage() {
    return (
        <RoleGuard allowedRoles={[AppRoles.Admin]}>
            <ManageFilesComponent manageSharedFiles={true} />
        </RoleGuard>
    );
}
