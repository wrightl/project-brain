import { RoleGuard } from '@/_components/auth/role-guard';
import ManageFilesComponent from '@/_components/manage-files/manage-files';

export default async function ManageFilesPage() {
    return (
        <RoleGuard allowedRoles={['user']}>
            <ManageFilesComponent manageSharedFiles={false} />
        </RoleGuard>
    );
}
