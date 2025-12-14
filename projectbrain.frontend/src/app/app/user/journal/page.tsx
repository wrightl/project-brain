import { RoleGuard } from '@/_components/auth/role-guard';
import JournalList from './_components/journal-list';

export default async function JournalPage() {
    return (
        <RoleGuard allowedRoles={['user']}>
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

