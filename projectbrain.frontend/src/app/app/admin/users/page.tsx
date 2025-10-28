import { RoleGuard } from '@/_components/auth/role-guard';
import { UsersIcon } from '@heroicons/react/24/outline';

export default async function AdminUsersPage() {
    // TODO: Implement API endpoint to fetch all users
    // For now, displaying placeholder

    return (
        <RoleGuard allowedRoles={['admin']}>
            <div className="space-y-6">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">
                        User Management
                    </h1>
                    <p className="mt-1 text-sm text-gray-600">
                        View and manage all users and coaches in the system
                    </p>
                </div>

                {/* Filters */}
                <div className="bg-white shadow rounded-lg p-4">
                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                        <div>
                            <label
                                htmlFor="role-filter"
                                className="block text-sm font-medium text-gray-700"
                            >
                                Role
                            </label>
                            <select
                                id="role-filter"
                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            >
                                <option value="">All Roles</option>
                                <option value="user">User</option>
                                <option value="coach">Coach</option>
                                <option value="admin">Admin</option>
                            </select>
                        </div>
                        <div>
                            <label
                                htmlFor="status-filter"
                                className="block text-sm font-medium text-gray-700"
                            >
                                Status
                            </label>
                            <select
                                id="status-filter"
                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            >
                                <option value="">All Statuses</option>
                                <option value="onboarded">Onboarded</option>
                                <option value="pending">Pending</option>
                            </select>
                        </div>
                        <div>
                            <label
                                htmlFor="search"
                                className="block text-sm font-medium text-gray-700"
                            >
                                Search
                            </label>
                            <input
                                type="text"
                                id="search"
                                placeholder="Search by name or email"
                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            />
                        </div>
                    </div>
                </div>

                {/* Users Table */}
                <div className="bg-white shadow rounded-lg overflow-hidden">
                    <div className="px-6 py-4 border-b border-gray-200">
                        <h2 className="text-lg font-medium text-gray-900">
                            All Users
                        </h2>
                    </div>
                    <div className="p-6 text-center text-gray-500">
                        <UsersIcon className="mx-auto h-12 w-12 text-gray-400" />
                        <p className="mt-2">No users found</p>
                        <p className="text-sm">
                            User management API endpoint needs to be implemented
                        </p>
                    </div>
                </div>
            </div>
        </RoleGuard>
    );
}
