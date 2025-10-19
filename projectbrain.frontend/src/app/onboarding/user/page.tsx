import { RoleGuard } from '@/components/auth/role-guard';
import UserOnboardingForm from '@/components/onboarding/user-onboarding-form';

export default async function UserOnboardingPage() {
  return (
    <RoleGuard allowedRoles={['user']} redirectTo="/dashboard">
      <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-2xl w-full space-y-8">
          <div>
            <h2 className="text-center text-3xl font-extrabold text-gray-900">
              Complete Your Profile
            </h2>
            <p className="mt-2 text-center text-sm text-gray-600">
              Tell us about yourself to get started
            </p>
          </div>
          <div className="bg-white shadow rounded-lg p-8">
            <UserOnboardingForm />
          </div>
        </div>
      </div>
    </RoleGuard>
  );
}
