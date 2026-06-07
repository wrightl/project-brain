import { RoleGuard } from '@/_components/auth/role-guard';
import StrategiesLibrary from './_components/strategies-library';

import { AppRoles } from '@/_lib/roles';
export default async function StrategiesPage() {
    return (
        <RoleGuard allowedRoles={[AppRoles.User]}>
            <div className="space-y-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">
                        Coping strategies
                    </h1>
                    <p className="mt-1 text-sm text-gray-600">
                        View, rate, and manage your saved strategies.
                    </p>
                </div>

                <StrategiesLibrary />
            </div>
        </RoleGuard>
    );
}

