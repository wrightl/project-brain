import { RoleGuard } from '@/_components/auth/role-guard';
import JournalEntryEditor from '../[id]/_components/journal-entry-editor';

import { AppRoles } from '@/_lib/roles';
export default async function NewJournalEntryPage() {
    return (
        <RoleGuard allowedRoles={[AppRoles.User]}>
            <JournalEntryEditor />
        </RoleGuard>
    );
}

