import { RoleGuard } from '@/_components/auth/role-guard';
import { CoachService, ClientWithConnectionStatus } from '@/_services/coach-service';
import ClientsList from './_components/clients-list';

export default async function CoachClientsPage() {
    let clients: ClientWithConnectionStatus[] = [];
    let error: string | null = null;

    try {
        clients = await CoachService.getConnectedClients();
    } catch (err) {
        error = err instanceof Error ? err.message : 'Failed to load clients';
        console.error('Error loading clients:', err);
    }

    return (
        <RoleGuard allowedRoles={['coach']}>
            <div className="space-y-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">
                        My Clients
                    </h1>
                    <p className="mt-1 text-sm text-gray-600">
                        View and manage your connected clients
                    </p>
                </div>

                <ClientsList clients={clients} error={error} />
            </div>
        </RoleGuard>
    );
}
