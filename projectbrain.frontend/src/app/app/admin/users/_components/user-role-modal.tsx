'use client';

import { useState, useEffect } from 'react';
import { User, UserRole } from '@/_lib/types';
import { XMarkIcon } from '@heroicons/react/24/outline';

interface UserRoleModalProps {
    user: User;
    isOpen: boolean;
    onClose: () => void;
    onSave: () => void;
}

const availableRoles: UserRole[] = ['user', 'coach', 'admin'];

export default function UserRoleModal({
    user,
    isOpen,
    onClose,
    onSave,
}: UserRoleModalProps) {
    const [selectedRoles, setSelectedRoles] = useState<UserRole[]>(user.roles);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (isOpen) {
            setSelectedRoles(user.roles);
            setError(null);
        }
    }, [user, isOpen]);

    const handleRoleToggle = (role: UserRole) => {
        setSelectedRoles((prev) => {
            if (prev.includes(role)) {
                return prev.filter((r) => r !== role);
            } else {
                return [...prev, role];
            }
        });
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setIsSubmitting(true);

        try {
            if (selectedRoles.length === 0) {
                setError('User must have at least one role');
                setIsSubmitting(false);
                return;
            }

            const response = await fetchWithAuth(
                `/api/admin/users/${user.id}/roles`,
                {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        roles: selectedRoles as string[],
                    }),
                }
            );

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(
                    errorData.error || 'Failed to update user roles'
                );
            }

            onSave();
        } catch (err) {
            setError(
                err instanceof Error
                    ? err.message
                    : 'Failed to update user roles'
            );
        } finally {
            setIsSubmitting(false);
        }
    };

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 z-50 overflow-y-auto">
            <div className="flex min-h-screen items-center justify-center p-4">
                <div
                    className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
                    onClick={onClose}
                ></div>

                <div className="relative bg-white rounded-lg shadow-xl max-w-md w-full">
                    <div className="sticky top-0 bg-white border-b border-gray-200 px-6 py-4 flex justify-between items-center">
                        <h2 className="text-xl font-semibold text-gray-900">
                            Edit User Roles
                        </h2>
                        <button
                            onClick={onClose}
                            className="text-gray-400 hover:text-gray-500"
                        >
                            <XMarkIcon className="h-6 w-6" />
                        </button>
                    </div>

                    <form onSubmit={handleSubmit} className="p-6 space-y-6">
                        {error && (
                            <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
                                {error}
                            </div>
                        )}

                        <div>
                            <p className="text-sm text-gray-600 mb-4">
                                Select roles for{' '}
                                <strong>{user.fullName}</strong>
                            </p>

                            <div className="space-y-3">
                                {availableRoles.map((role) => (
                                    <div
                                        key={role}
                                        className="flex items-center"
                                    >
                                        <input
                                            id={`role-${role}`}
                                            name={`role-${role}`}
                                            type="checkbox"
                                            checked={selectedRoles.includes(
                                                role
                                            )}
                                            onChange={() =>
                                                handleRoleToggle(role)
                                            }
                                            className="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300 rounded"
                                        />
                                        <label
                                            htmlFor={`role-${role}`}
                                            className="ml-3 block text-sm font-medium text-gray-900 capitalize"
                                        >
                                            {role}
                                        </label>
                                    </div>
                                ))}
                            </div>

                            {selectedRoles.length === 0 && (
                                <p className="mt-2 text-sm text-red-600">
                                    At least one role must be selected
                                </p>
                            )}
                        </div>

                        <div className="flex justify-end space-x-3 pt-4 border-t border-gray-200">
                            <button
                                type="button"
                                onClick={onClose}
                                className="px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50"
                            >
                                Cancel
                            </button>
                            <button
                                type="submit"
                                disabled={
                                    isSubmitting || selectedRoles.length === 0
                                }
                                className="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:bg-gray-400"
                            >
                                {isSubmitting ? 'Saving...' : 'Save Roles'}
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    );
}
