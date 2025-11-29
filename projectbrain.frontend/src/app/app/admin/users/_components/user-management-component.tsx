'use client';

import { User, UserRole } from '@/_lib/types';
import { useEffect, useState } from 'react';
import {
    PencilIcon,
    TrashIcon,
    UserCircleIcon,
} from '@heroicons/react/24/outline';
import UserEditModal from './user-edit-modal';
import UserRoleModal from './user-role-modal';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';

export default function UserManagementComponent() {
    const [users, setUsers] = useState<User[]>([]);
    const [filteredUsers, setFilteredUsers] = useState<User[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [selectedUser, setSelectedUser] = useState<User | null>(null);
    const [isEditModalOpen, setIsEditModalOpen] = useState(false);
    const [isRoleModalOpen, setIsRoleModalOpen] = useState(false);
    const [roleFilter, setRoleFilter] = useState<string>('');
    const [statusFilter, setStatusFilter] = useState<string>('');
    const [searchQuery, setSearchQuery] = useState<string>('');

    useEffect(() => {
        loadUsers();
    }, []);

    useEffect(() => {
        filterUsers();
    }, [users, roleFilter, statusFilter, searchQuery]);

    const loadUsers = async () => {
        try {
            setLoading(true);
            setError(null);
            const response = await fetchWithAuth('/api/admin/users');
            if (!response.ok) {
                throw new Error('Failed to load users');
            }
            const allUsers = await response.json();
            setUsers(allUsers);
        } catch (err) {
            setError(
                err instanceof Error ? err.message : 'Failed to load users'
            );
            console.error('Error loading users:', err);
        } finally {
            setLoading(false);
        }
    };

    const filterUsers = () => {
        let filtered = [...users];

        if (roleFilter) {
            filtered = filtered.filter((user) =>
                user.roles.includes(roleFilter as UserRole)
            );
        }

        if (statusFilter) {
            if (statusFilter === 'onboarded') {
                filtered = filtered.filter((user) => user.isOnboarded);
            } else if (statusFilter === 'pending') {
                filtered = filtered.filter((user) => !user.isOnboarded);
            }
        }

        if (searchQuery) {
            const query = searchQuery.toLowerCase();
            filtered = filtered.filter(
                (user) =>
                    user.fullName.toLowerCase().includes(query) ||
                    user.email.toLowerCase().includes(query)
            );
        }

        setFilteredUsers(filtered);
    };

    const handleEdit = (user: User) => {
        setSelectedUser(user);
        setIsEditModalOpen(true);
    };

    const handleRoleEdit = (user: User) => {
        setSelectedUser(user);
        setIsRoleModalOpen(true);
    };

    const handleDelete = async (user: User) => {
        if (
            !confirm(
                `Are you sure you want to delete ${user.fullName}? This action cannot be undone.`
            )
        ) {
            return;
        }

        try {
            const response = await fetchWithAuth(
                `/api/admin/users/${user.id}`,
                {
                    method: 'DELETE',
                }
            );
            if (!response.ok) {
                throw new Error('Failed to delete user');
            }
            await loadUsers();
        } catch (err) {
            alert(err instanceof Error ? err.message : 'Failed to delete user');
        }
    };

    const handleUserUpdated = () => {
        setIsEditModalOpen(false);
        setSelectedUser(null);
        loadUsers();
    };

    const handleRolesUpdated = () => {
        setIsRoleModalOpen(false);
        setSelectedUser(null);
        loadUsers();
    };

    return (
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
                            value={roleFilter}
                            onChange={(e) => setRoleFilter(e.target.value)}
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
                            value={statusFilter}
                            onChange={(e) => setStatusFilter(e.target.value)}
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
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            placeholder="Search by name or email"
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                    </div>
                </div>
            </div>

            {/* Error Message */}
            {error && (
                <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
                    {error}
                </div>
            )}

            {/* Users Table */}
            <div className="bg-white shadow rounded-lg overflow-hidden">
                <div className="px-6 py-4 border-b border-gray-200 flex justify-between items-center">
                    <h2 className="text-lg font-medium text-gray-900">
                        All Users ({filteredUsers.length})
                    </h2>
                    <button
                        onClick={loadUsers}
                        className="text-sm text-indigo-600 hover:text-indigo-800"
                    >
                        Refresh
                    </button>
                </div>

                {loading ? (
                    <div className="p-6 text-center text-gray-500">
                        <p>Loading users...</p>
                    </div>
                ) : filteredUsers.length === 0 ? (
                    <div className="p-6 text-center text-gray-500">
                        <UserCircleIcon className="mx-auto h-12 w-12 text-gray-400" />
                        <p className="mt-2">No users found</p>
                    </div>
                ) : (
                    <div className="overflow-x-auto">
                        <table className="min-w-full divide-y divide-gray-200">
                            <thead className="bg-gray-50">
                                <tr>
                                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                        User
                                    </th>
                                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                        Email
                                    </th>
                                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                        Roles
                                    </th>
                                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                        Status
                                    </th>
                                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                        Last Activity
                                    </th>
                                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                        Actions
                                    </th>
                                </tr>
                            </thead>
                            <tbody className="bg-white divide-y divide-gray-200">
                                {filteredUsers.map((user) => (
                                    <tr key={user.id}>
                                        <td className="px-6 py-4 whitespace-nowrap">
                                            <div className="text-sm font-medium text-gray-900">
                                                {user.fullName}
                                            </div>
                                        </td>
                                        <td className="px-6 py-4 whitespace-nowrap">
                                            <div className="text-sm text-gray-500">
                                                {user.email}
                                            </div>
                                        </td>
                                        <td className="px-6 py-4 whitespace-nowrap">
                                            <div className="flex flex-wrap gap-1">
                                                {user.roles.map((role) => (
                                                    <span
                                                        key={role}
                                                        className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-100 text-indigo-800"
                                                    >
                                                        {role}
                                                    </span>
                                                ))}
                                            </div>
                                        </td>
                                        <td className="px-6 py-4 whitespace-nowrap">
                                            <span
                                                className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                                                    user.isOnboarded
                                                        ? 'bg-green-100 text-green-800'
                                                        : 'bg-yellow-100 text-yellow-800'
                                                }`}
                                            >
                                                {user.isOnboarded
                                                    ? 'Onboarded'
                                                    : 'Pending'}
                                            </span>
                                        </td>
                                        <td className="px-6 py-4 whitespace-nowrap">
                                            <div className="text-sm text-gray-500">
                                                {user.lastActivityAt
                                                    ? new Date(
                                                          user.lastActivityAt
                                                      ).toLocaleString()
                                                    : 'Never'}
                                            </div>
                                        </td>
                                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                                            <div className="flex space-x-2">
                                                {/* <button
                                                    onClick={() =>
                                                        handleEdit(user)
                                                    }
                                                    className="text-indigo-600 hover:text-indigo-900"
                                                    title="Edit user"
                                                >
                                                    <PencilIcon className="h-5 w-5" />
                                                </button> */}
                                                <button
                                                    onClick={() =>
                                                        handleRoleEdit(user)
                                                    }
                                                    className="text-blue-600 hover:text-blue-900"
                                                    title="Edit roles"
                                                >
                                                    <UserCircleIcon className="h-5 w-5" />
                                                </button>
                                                <button
                                                    onClick={() =>
                                                        handleDelete(user)
                                                    }
                                                    className="text-red-600 hover:text-red-900"
                                                    title="Delete user"
                                                >
                                                    <TrashIcon className="h-5 w-5" />
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>

            {/* Edit Modal */}
            {selectedUser && (
                <>
                    <UserEditModal
                        user={selectedUser}
                        isOpen={isEditModalOpen}
                        onClose={() => {
                            setIsEditModalOpen(false);
                            setSelectedUser(null);
                        }}
                        onSave={handleUserUpdated}
                    />
                    <UserRoleModal
                        user={selectedUser}
                        isOpen={isRoleModalOpen}
                        onClose={() => {
                            setIsRoleModalOpen(false);
                            setSelectedUser(null);
                        }}
                        onSave={handleRolesUpdated}
                    />
                </>
            )}
        </div>
    );
}
