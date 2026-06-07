import { RoleGuard } from '@/_components/auth/role-guard';
import JournalList from './_components/journal-list';

import { AppRoles } from '@/_lib/roles';
export default async function JournalPage() {
    return (
        <RoleGuard allowedRoles={[AppRoles.User]}>
            <div className="space-y-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">
                        Journal Entries
                    </h1>
                    <p className="mt-1 text-sm text-gray-600">
                        View and manage your journal entries
                    </p>
                </div>

                <JournalList />
            </div>
        </RoleGuard>
    );
}

