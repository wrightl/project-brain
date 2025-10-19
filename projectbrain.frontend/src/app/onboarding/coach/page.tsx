import { RoleGuard } from '@/components/auth/role-guard';
import CoachOnboardingForm from '@/components/onboarding/coach-onboarding-form';

export default async function CoachOnboardingPage() {
  return (
    <RoleGuard allowedRoles={['coach']} redirectTo="/dashboard">
      <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-2xl w-full space-y-8">
          <div>
            <h2 className="text-center text-3xl font-extrabold text-gray-900">
              Complete Your Coach Profile
            </h2>
            <p className="mt-2 text-center text-sm text-gray-600">
              Tell us about your experience and help us connect you with users in your region
            </p>
          </div>
          <div className="bg-white shadow rounded-lg p-8">
            <CoachOnboardingForm />
          </div>
        </div>
      </div>
    </RoleGuard>
  );
}
