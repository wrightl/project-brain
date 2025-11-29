'use client';

import { User } from '@/_lib/types';
import { XMarkIcon, MapPinIcon, EnvelopeIcon, CalendarIcon, UserIcon } from '@heroicons/react/24/outline';

interface ClientDetailModalProps {
    client: User | null;
    isOpen: boolean;
    onClose: () => void;
}

function formatDate(dateString?: string): string {
    if (!dateString) return 'Not provided';
    try {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
        });
    } catch {
        return dateString;
    }
}

function formatLastActivity(lastActivityAt?: string): string {
    if (!lastActivityAt) {
        return 'Never';
    }

    const date = new Date(lastActivityAt);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) {
        return 'Just now';
    } else if (diffMins < 60) {
        return `${diffMins} minute${diffMins === 1 ? '' : 's'} ago`;
    } else if (diffHours < 24) {
        return `${diffHours} hour${diffHours === 1 ? '' : 's'} ago`;
    } else if (diffDays < 7) {
        return `${diffDays} day${diffDays === 1 ? '' : 's'} ago`;
    } else {
        return date.toLocaleDateString();
    }
}

function isUserOnline(lastActivityAt?: string): boolean {
    if (!lastActivityAt) {
        return false;
    }
    const date = new Date(lastActivityAt);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    return diffMins <= 30;
}

export default function ClientDetailModal({
    client,
    isOpen,
    onClose,
}: ClientDetailModalProps) {
    if (!isOpen || !client) return null;

    const isOnline = isUserOnline(client.lastActivityAt);
    const fullAddress = [
        client.streetAddress,
        client.addressLine2,
        client.city,
        client.stateProvince,
        client.postalCode,
        client.country,
    ]
        .filter(Boolean)
        .join(', ');

    return (
        <div className="fixed inset-0 z-50 overflow-y-auto">
            <div className="flex min-h-screen items-center justify-center p-4">
                <div
                    className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
                    onClick={onClose}
                ></div>

                <div className="relative bg-white rounded-lg shadow-xl max-w-3xl w-full max-h-[90vh] overflow-y-auto">
                    <div className="sticky top-0 bg-white border-b border-gray-200 px-6 py-4 flex justify-between items-center z-10">
                        <h2 className="text-xl font-semibold text-gray-900">
                            Client Details
                        </h2>
                        <button
                            onClick={onClose}
                            className="text-gray-400 hover:text-gray-500"
                        >
                            <XMarkIcon className="h-6 w-6" />
                        </button>
                    </div>

                    <div className="p-6 space-y-6">
                        {/* Header Section */}
                        <div className="border-b border-gray-200 pb-6">
                            <div className="flex items-start justify-between">
                                <div className="flex-1">
                                    <div className="flex items-center space-x-3">
                                        <h1 className="text-2xl font-bold text-gray-900">
                                            {client.fullName}
                                        </h1>
                                        {isOnline && (
                                            <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-green-100 text-green-800">
                                                Online
                                            </span>
                                        )}
                                    </div>
                                    {client.email && (
                                        <p className="mt-2 text-gray-600 flex items-center">
                                            <EnvelopeIcon className="h-5 w-5 mr-2 text-gray-400" />
                                            {client.email}
                                        </p>
                                    )}
                                    {client.city && (
                                        <p className="mt-2 text-gray-600 flex items-center">
                                            <MapPinIcon className="h-5 w-5 mr-2 text-gray-400" />
                                            {[
                                                client.city,
                                                client.stateProvince,
                                                client.country,
                                            ]
                                                .filter(Boolean)
                                                .join(', ')}
                                        </p>
                                    )}
                                </div>
                            </div>
                        </div>

                        {/* Personal Information */}
                        <div>
                            <h3 className="text-lg font-medium text-gray-900 mb-4">
                                Personal Information
                            </h3>
                            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                                <div>
                                    <label className="block text-sm font-medium text-gray-500">
                                        Full Name
                                    </label>
                                    <p className="mt-1 text-sm text-gray-900">
                                        {client.fullName}
                                    </p>
                                </div>
                                {client.firstName && (
                                    <div>
                                        <label className="block text-sm font-medium text-gray-500">
                                            First Name
                                        </label>
                                        <p className="mt-1 text-sm text-gray-900">
                                            {client.firstName}
                                        </p>
                                    </div>
                                )}
                                {client.doB && (
                                    <div>
                                        <label className="block text-sm font-medium text-gray-500">
                                            Date of Birth
                                        </label>
                                        <p className="mt-1 text-sm text-gray-900 flex items-center">
                                            <CalendarIcon className="h-4 w-4 mr-1 text-gray-400" />
                                            {formatDate(client.doB)}
                                        </p>
                                    </div>
                                )}
                                {client.preferredPronoun && (
                                    <div>
                                        <label className="block text-sm font-medium text-gray-500">
                                            Preferred Pronoun
                                        </label>
                                        <p className="mt-1 text-sm text-gray-900">
                                            {client.preferredPronoun}
                                        </p>
                                    </div>
                                )}
                            </div>
                        </div>

                        {/* Neurodiversity Information */}
                        {(client.neurodiverseTraits && client.neurodiverseTraits.length > 0) ||
                        client.preferences ? (
                            <div>
                                <h3 className="text-lg font-medium text-gray-900 mb-4">
                                    Neurodiversity Information
                                </h3>
                                <div className="space-y-4">
                                    {client.neurodiverseTraits &&
                                        client.neurodiverseTraits.length > 0 && (
                                            <div>
                                                <label className="block text-sm font-medium text-gray-500">
                                                    Neurodiverse Traits
                                                </label>
                                                <div className="mt-2 flex flex-wrap gap-2">
                                                    {client.neurodiverseTraits.map(
                                                        (trait, index) => (
                                                            <span
                                                                key={index}
                                                                className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-indigo-100 text-indigo-800"
                                                            >
                                                                {trait}
                                                            </span>
                                                        )
                                                    )}
                                                </div>
                                            </div>
                                        )}
                                    {client.preferences && (
                                        <div>
                                            <label className="block text-sm font-medium text-gray-500">
                                                Preferences
                                            </label>
                                            <p className="mt-1 text-sm text-gray-900">
                                                {client.preferences}
                                            </p>
                                        </div>
                                    )}
                                </div>
                            </div>
                        ) : null}

                        {/* Address Information */}
                        {fullAddress && (
                            <div>
                                <h3 className="text-lg font-medium text-gray-900 mb-4">
                                    Address
                                </h3>
                                <div className="space-y-2">
                                    {client.streetAddress && (
                                        <p className="text-sm text-gray-900">
                                            {client.streetAddress}
                                        </p>
                                    )}
                                    {client.addressLine2 && (
                                        <p className="text-sm text-gray-900">
                                            {client.addressLine2}
                                        </p>
                                    )}
                                    <p className="text-sm text-gray-900">
                                        {[
                                            client.city,
                                            client.stateProvince,
                                            client.postalCode,
                                        ]
                                            .filter(Boolean)
                                            .join(', ')}
                                    </p>
                                    {client.country && (
                                        <p className="text-sm text-gray-900">
                                            {client.country}
                                        </p>
                                    )}
                                </div>
                            </div>
                        )}

                        {/* Activity Information */}
                        <div>
                            <h3 className="text-lg font-medium text-gray-900 mb-4">
                                Activity
                            </h3>
                            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                                <div>
                                    <label className="block text-sm font-medium text-gray-500">
                                        Last Activity
                                    </label>
                                    <p className="mt-1 text-sm text-gray-900">
                                        {formatLastActivity(client.lastActivityAt)}
                                    </p>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-gray-500">
                                        Onboarding Status
                                    </label>
                                    <p className="mt-1">
                                        <span
                                            className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                                                client.isOnboarded
                                                    ? 'bg-green-100 text-green-800'
                                                    : 'bg-yellow-100 text-yellow-800'
                                            }`}
                                        >
                                            {client.isOnboarded
                                                ? 'Onboarded'
                                                : 'Pending'}
                                        </span>
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

