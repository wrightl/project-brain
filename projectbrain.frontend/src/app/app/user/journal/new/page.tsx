import { RoleGuard } from '@/_components/auth/role-guard';
import JournalEntryEditor from '../[id]/_components/journal-entry-editor';

export default async function NewJournalEntryPage() {
    return (
        <RoleGuard allowedRoles={['user']}>
            <JournalEntryEditor />
        </RoleGuard>
    );
}

