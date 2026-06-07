import { RoleGuard } from '@/_components/auth/role-guard';
import JournalEntryEditor from './_components/journal-entry-editor';

import { AppRoles } from '@/_lib/roles';
export default async function JournalEntryPage({
    params,
}: {
    params: { id: string };
}) {
    return (
        <RoleGuard allowedRoles={[AppRoles.User]}>
            <JournalEntryEditor entryId={(await params).id} />
        </RoleGuard>
    );
}
