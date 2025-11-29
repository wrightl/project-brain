import { RoleGuard } from '@/_components/auth/role-guard';
import VoiceNotesList from './_components/voicenotes-list';

export default async function VoiceNotesPage() {
    return (
        <RoleGuard allowedRoles={['user']}>
            <div className="space-y-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">
                        Voice Notes
                    </h1>
                    <p className="mt-1 text-sm text-gray-600">
                        View and manage your recorded voice notes
                    </p>
                </div>

                <VoiceNotesList />
            </div>
        </RoleGuard>
    );
}
