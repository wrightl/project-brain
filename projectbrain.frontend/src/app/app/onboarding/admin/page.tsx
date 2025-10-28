import { RoleGuard } from '@/_components/auth/role-guard';
import { getUserEmail, getAccessToken } from '@/_lib/auth';
import AdminOnboardingForm from '../_components/admin-onboarding-form';

// Force dynamic rendering to allow access to request-time APIs
export const dynamic = 'force-dynamic';

export default async function AdminOnboardingPage() {
    const userEmail = await getUserEmail();
    const accessToken = await getAccessToken();

    return (
        <RoleGuard allowedRoles={['admin']} redirectTo="/dashboard">
            <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
                <div className="max-w-2xl w-full space-y-8">
                    <div>
                        <h2 className="text-center text-3xl font-extrabold text-gray-900">
                            Complete Your Admin Profile
                        </h2>
                        <p className="mt-2 text-center text-sm text-gray-600">
                            Set up your administrator account
                        </p>
                    </div>
                    <div className="bg-white shadow rounded-lg p-8">
                        <AdminOnboardingForm
                            userEmail={userEmail || ''}
                            accessToken={accessToken || ''}
                        />
                    </div>
                </div>
            </div>
        </RoleGuard>
    );
}
