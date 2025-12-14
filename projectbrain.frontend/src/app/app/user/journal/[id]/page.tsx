import { RoleGuard } from '@/_components/auth/role-guard';
import JournalEntryEditor from './_components/journal-entry-editor';

export default async function JournalEntryPage({
    params,
}: {
    params: { id: string };
}) {
    return (
        <RoleGuard allowedRoles={['user']}>
            <JournalEntryEditor entryId={(await params).id} />
        </RoleGuard>
    );
}
