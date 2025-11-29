import { getUserEmail } from '@/_lib/auth';
import UserOnboardingWizard from '../_components/user-onboarding-wizard';

// Force dynamic rendering to allow access to request-time APIs
export const dynamic = 'force-dynamic';

export default async function UserOnboardingPage() {
    const userEmail = await getUserEmail();

    return (
        <div className="min-h-screen bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
            <div className="max-w-4xl mx-auto space-y-8">
                <div>
                    <h2 className="text-center text-3xl font-extrabold text-gray-900">
                        Complete Your Profile
                    </h2>
                    <p className="mt-2 text-center text-sm text-gray-600">
                        Tell us about yourself to get started
                    </p>
                </div>
                <UserOnboardingWizard userEmail={userEmail || ''} />
            </div>
        </div>
    );
}
