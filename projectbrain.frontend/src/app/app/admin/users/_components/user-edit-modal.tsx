'use client';

import { useState, useEffect } from 'react';
import { User } from '@/_lib/types';
import { XMarkIcon } from '@heroicons/react/24/outline';

interface UserEditModalProps {
    user: User;
    isOpen: boolean;
    onClose: () => void;
    onSave: () => void;
}

export default function UserEditModal({
    user,
    isOpen,
    onClose,
    onSave,
}: UserEditModalProps) {
    const [formData, setFormData] = useState({
        fullName: user.fullName,
        doB: user.doB,
        isOnboarded: user.isOnboarded,
        preferredPronoun: user.preferredPronoun || '',
        neurodivergentDetails: user.neurodivergentDetails || '',
        streetAddress: user.streetAddress || '',
        addressLine2: user.addressLine2 || '',
        city: user.city || '',
        stateProvince: user.stateProvince || '',
        postalCode: user.postalCode || '',
        country: user.country || '',
    });
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (isOpen) {
            setFormData({
                fullName: user.fullName,
                doB: user.doB,
                isOnboarded: user.isOnboarded,
                preferredPronoun: user.preferredPronoun || '',
                neurodivergentDetails: user.neurodivergentDetails || '',
                streetAddress: user.streetAddress || '',
                addressLine2: user.addressLine2 || '',
                city: user.city || '',
                stateProvince: user.stateProvince || '',
                postalCode: user.postalCode || '',
                country: user.country || '',
            });
            setError(null);
        }
    }, [user, isOpen]);

    const handleChange = (
        e: React.ChangeEvent<
            HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
        >
    ) => {
        const { name, value, type } = e.target;
        setFormData((prev) => ({
            ...prev,
            [name]:
                type === 'checkbox'
                    ? (e.target as HTMLInputElement).checked
                    : value,
        }));
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setIsSubmitting(true);

        try {
            const response = await fetchWithAuth(
                `/api/admin/users/${user.id}`,
                {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        fullName: formData.fullName,
                        doB: formData.doB,
                        isOnboarded: formData.isOnboarded,
                        preferredPronoun:
                            formData.preferredPronoun || undefined,
                        neurodivergentDetails:
                            formData.neurodivergentDetails || undefined,
                        streetAddress: formData.streetAddress || undefined,
                        addressLine2: formData.addressLine2 || undefined,
                        city: formData.city || undefined,
                        stateProvince: formData.stateProvince || undefined,
                        postalCode: formData.postalCode || undefined,
                        country: formData.country || undefined,
                    }),
                }
            );

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.error || 'Failed to update user');
            }

            onSave();
        } catch (err) {
            setError(
                err instanceof Error ? err.message : 'Failed to update user'
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

                <div className="relative bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
                    <div className="sticky top-0 bg-white border-b border-gray-200 px-6 py-4 flex justify-between items-center">
                        <h2 className="text-xl font-semibold text-gray-900">
                            Edit User
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

                        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                            <div className="sm:col-span-2">
                                <label
                                    htmlFor="email"
                                    className="block text-sm font-medium text-gray-700"
                                >
                                    Email
                                </label>
                                <input
                                    type="email"
                                    id="email"
                                    value={user.email}
                                    disabled
                                    className="mt-1 block w-full rounded-md border-gray-300 bg-gray-50 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm cursor-not-allowed"
                                />
                            </div>

                            <div>
                                <label
                                    htmlFor="fullName"
                                    className="block text-sm font-medium text-gray-700"
                                >
                                    Full Name *
                                </label>
                                <input
                                    type="text"
                                    id="fullName"
                                    name="fullName"
                                    required
                                    value={formData.fullName}
                                    onChange={handleChange}
                                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                />
                            </div>

                            <div>
                                <label
                                    htmlFor="doB"
                                    className="block text-sm font-medium text-gray-700"
                                >
                                    Date of Birth *
                                </label>
                                <input
                                    type="date"
                                    id="doB"
                                    name="doB"
                                    required
                                    value={formData.doB}
                                    onChange={handleChange}
                                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                />
                            </div>

                            <div>
                                <label
                                    htmlFor="preferredPronoun"
                                    className="block text-sm font-medium text-gray-700"
                                >
                                    Preferred Pronoun
                                </label>
                                <input
                                    type="text"
                                    id="preferredPronoun"
                                    name="preferredPronoun"
                                    value={formData.preferredPronoun}
                                    onChange={handleChange}
                                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                />
                            </div>

                            <div className="sm:col-span-2">
                                <label
                                    htmlFor="neurodivergentDetails"
                                    className="block text-sm font-medium text-gray-700"
                                >
                                    Neurodivergent Details
                                </label>
                                <textarea
                                    id="neurodivergentDetails"
                                    name="neurodivergentDetails"
                                    rows={3}
                                    value={formData.neurodivergentDetails}
                                    onChange={handleChange}
                                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                />
                            </div>

                            <div className="sm:col-span-2 border-t border-gray-200 pt-4">
                                <h3 className="text-sm font-medium text-gray-900 mb-4">
                                    Address Information
                                </h3>
                                <div className="space-y-4">
                                    <div>
                                        <label
                                            htmlFor="streetAddress"
                                            className="block text-sm font-medium text-gray-700"
                                        >
                                            Street Address
                                        </label>
                                        <input
                                            type="text"
                                            id="streetAddress"
                                            name="streetAddress"
                                            value={formData.streetAddress}
                                            onChange={handleChange}
                                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                        />
                                    </div>

                                    <div>
                                        <label
                                            htmlFor="addressLine2"
                                            className="block text-sm font-medium text-gray-700"
                                        >
                                            Address Line 2
                                        </label>
                                        <input
                                            type="text"
                                            id="addressLine2"
                                            name="addressLine2"
                                            value={formData.addressLine2}
                                            onChange={handleChange}
                                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                        />
                                    </div>

                                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                                        <div>
                                            <label
                                                htmlFor="city"
                                                className="block text-sm font-medium text-gray-700"
                                            >
                                                City
                                            </label>
                                            <input
                                                type="text"
                                                id="city"
                                                name="city"
                                                value={formData.city}
                                                onChange={handleChange}
                                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                            />
                                        </div>

                                        <div>
                                            <label
                                                htmlFor="stateProvince"
                                                className="block text-sm font-medium text-gray-700"
                                            >
                                                State/Province/Region
                                            </label>
                                            <input
                                                type="text"
                                                id="stateProvince"
                                                name="stateProvince"
                                                value={formData.stateProvince}
                                                onChange={handleChange}
                                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                            />
                                        </div>
                                    </div>

                                    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                                        <div>
                                            <label
                                                htmlFor="postalCode"
                                                className="block text-sm font-medium text-gray-700"
                                            >
                                                Postal/Zip Code
                                            </label>
                                            <input
                                                type="text"
                                                id="postalCode"
                                                name="postalCode"
                                                value={formData.postalCode}
                                                onChange={handleChange}
                                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                            />
                                        </div>

                                        <div>
                                            <label
                                                htmlFor="country"
                                                className="block text-sm font-medium text-gray-700"
                                            >
                                                Country
                                            </label>
                                            <input
                                                type="text"
                                                id="country"
                                                name="country"
                                                value={formData.country}
                                                onChange={handleChange}
                                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                            />
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div className="sm:col-span-2">
                                <div className="flex items-center">
                                    <input
                                        id="isOnboarded"
                                        name="isOnboarded"
                                        type="checkbox"
                                        checked={formData.isOnboarded}
                                        onChange={handleChange}
                                        className="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300 rounded"
                                    />
                                    <label
                                        htmlFor="isOnboarded"
                                        className="ml-2 block text-sm text-gray-900"
                                    >
                                        User is onboarded
                                    </label>
                                </div>
                            </div>
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
                                disabled={isSubmitting}
                                className="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:bg-gray-400"
                            >
                                {isSubmitting ? 'Saving...' : 'Save Changes'}
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    );
}
