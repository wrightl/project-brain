import { RoleGuard } from '@/_components/auth/role-guard';
import ManageFilesComponent from '@/_components/manage-files/manage-files';

export default async function AdminUploadPage() {
    return (
        <RoleGuard allowedRoles={['admin']}>
            <ManageFilesComponent manageSharedFiles={true} />
        </RoleGuard>
    );
}
