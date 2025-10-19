import { RoleGuard } from '@/components/auth/role-guard';

export default async function CoachSettingsPage() {
  // TODO: Uncomment after Auth0 API is configured
  // const user = await getCurrentUser();

  return (
    <RoleGuard allowedRoles={['coach']}>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Coach Settings</h1>
          <p className="mt-1 text-sm text-gray-600">
            Manage your profile and preferences
          </p>
        </div>

        <div className="bg-white shadow rounded-lg p-6">
          <h2 className="text-lg font-medium text-gray-900 mb-4">
            Profile Information
          </h2>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700">
                Name
              </label>
              <p className="mt-1 text-sm text-gray-900">
                Not available (API not configured)
              </p>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700">
                Email
              </label>
              <p className="mt-1 text-sm text-gray-900">
                Not available (API not configured)
              </p>
            </div>
            <div className="pt-4">
              <button
                type="button"
                className="px-4 py-2 bg-indigo-600 text-white font-medium rounded-md hover:bg-indigo-700 transition-colors"
              >
                Edit Profile
              </button>
            </div>
          </div>
        </div>

        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
          <p className="text-sm text-yellow-800">
            <strong>Note:</strong> Profile data will be available after configuring the Auth0 API. See AUTH0_API_SETUP.md for instructions.
          </p>
        </div>
      </div>
    </RoleGuard>
  );
}
