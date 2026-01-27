'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { User } from '@/_lib/types';
import {
    ArrowLeftIcon,
    CheckCircleIcon,
    XCircleIcon,
} from '@heroicons/react/24/outline';
import toast from 'react-hot-toast';
import ConfirmationDialog from '@/_components/confirmation-dialog';
import { UsagePieCard } from './usage-pie-card';

interface UserDetailsComponentProps {
    userId: string;
}

interface SubscriptionData {
    tier: string;
    status: string;
    trialEndsAt?: string;
    currentPeriodStart?: string;
    currentPeriodEnd?: string;
    canceledAt?: string;
    userType: string;
    isExcluded?: boolean;
}

interface UserUsageResponse {
    userId: string;
    userType: 'user' | 'coach';
    tier: string;
    usage: {
        aiQueries: { daily: number; monthly: number };
        coachMessages: { monthly: number };
        clientMessages: { monthly: number };
        researchReports: { monthly: number };
        fileUploads: { monthly: number };
        fileStorage: { bytes: number; megabytes: number };
        files: { totalCount: number };
        connections: { coachAcceptedCount: number; clientAcceptedCount: number };
    };
    limits: {
        dailyAIQueries?: number | null;
        monthlyAIQueries?: number | null;
        maxCoachConnections?: number | null;
        monthlyCoachMessages?: number | null;
        maxFiles?: number | null;
        maxFileStorageMB?: number | null;
        monthlyResearchReports?: number | null;
        maxClientConnections?: number | null;
        monthlyClientMessages?: number | null;
        fileUploadsMonthly?: number | null;
    };
}

export default function UserDetailsComponent({
    userId,
}: UserDetailsComponentProps) {
    const router = useRouter();
    const [user, setUser] = useState<User | null>(null);
    const [subscription, setSubscription] = useState<SubscriptionData | null>(
        null
    );
    const [usageStats, setUsageStats] = useState<UserUsageResponse | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [exclusionLoading, setExclusionLoading] = useState(false);
    const [exclusionNotes, setExclusionNotes] = useState('');
    const [addExclusionConfirmOpen, setAddExclusionConfirmOpen] =
        useState(false);
    const [removeExclusionConfirmOpen, setRemoveExclusionConfirmOpen] =
        useState(false);

    useEffect(() => {
        loadData();
    }, [userId]);

    const loadData = async () => {
        try {
            setLoading(true);
            const [userResponse, subscriptionResponse, usageResponse] =
                await Promise.all([
                fetchWithAuth(`/api/admin/users/${userId}`),
                fetchWithAuth(`/api/admin/users/${userId}/subscription`),
                fetchWithAuth(`/api/admin/users/${userId}/subscription/usage`),
            ]);

            if (!userResponse.ok) {
                throw new Error('Failed to load user');
            }

            const userData = await userResponse.json();
            setUser(userData);

            if (subscriptionResponse.ok) {
                const subscriptionData = await subscriptionResponse.json();
                setSubscription(subscriptionData);
            }

            if (usageResponse.ok) {
                const usageData = await usageResponse.json();
                setUsageStats(usageData);
            } else {
                setUsageStats(null);
            }
        } catch (err) {
            setError(
                err instanceof Error ? err.message : 'Failed to load user data'
            );
        } finally {
            setLoading(false);
        }
    };

    const handleAddExclusionClick = () => {
        setAddExclusionConfirmOpen(true);
    };

    const handleAddExclusion = async () => {
        try {
            setExclusionLoading(true);
            const userType = subscription?.userType || 'user';
            const response = await fetchWithAuth(
                `/api/admin/users/${userId}/subscription/exclusion`,
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({
                        userType,
                        notes: exclusionNotes,
                    }),
                }
            );

            if (!response.ok) {
                throw new Error('Failed to add exclusion');
            }

            toast.success(
                'User excluded from subscription requirements successfully'
            );
            setExclusionNotes('');
            await loadData();
        } catch (err) {
            toast.error(
                err instanceof Error ? err.message : 'Failed to add exclusion'
            );
        } finally {
            setExclusionLoading(false);
            setAddExclusionConfirmOpen(false);
        }
    };

    const handleRemoveExclusionClick = () => {
        setRemoveExclusionConfirmOpen(true);
    };

    const handleRemoveExclusion = async () => {
        try {
            setExclusionLoading(true);
            const response = await fetchWithAuth(
                `/api/admin/users/${userId}/subscription/exclusion`,
                {
                    method: 'DELETE',
                }
            );

            if (!response.ok) {
                throw new Error('Failed to remove exclusion');
            }

            toast.success('Exclusion removed successfully');
            await loadData();
        } catch (err) {
            toast.error(
                err instanceof Error
                    ? err.message
                    : 'Failed to remove exclusion'
            );
        } finally {
            setExclusionLoading(false);
            setRemoveExclusionConfirmOpen(false);
        }
    };

    const formatDate = (dateString?: string) => {
        if (!dateString) return 'N/A';
        return new Date(dateString).toLocaleDateString();
    };

    if (loading) {
        return (
            <div className="flex items-center justify-center min-h-screen">
                <div className="text-lg">Loading user details...</div>
            </div>
        );
    }

    if (error || !user) {
        return (
            <div className="flex items-center justify-center min-h-screen">
                <div className="text-red-600">
                    Error: {error || 'User not found'}
                </div>
            </div>
        );
    }

    const isActive =
        subscription?.status === 'active' ||
        subscription?.status === 'trialing';
    const isExcluded = subscription?.isExcluded === true;

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <Link
                        href="/app/admin/users"
                        className="inline-flex items-center text-sm text-gray-600 hover:text-gray-900 mb-2"
                    >
                        <ArrowLeftIcon className="h-4 w-4 mr-1" />
                        Back to Users
                    </Link>
                    <h1 className="text-3xl font-bold text-gray-900">
                        {user.fullName}
                    </h1>
                    <p className="mt-1 text-sm text-gray-600">{user.email}</p>
                </div>
            </div>

            {/* User Information */}
            <div className="bg-white shadow rounded-lg p-6">
                <h2 className="text-xl font-semibold mb-4">User Information</h2>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <div>
                        <label className="block text-sm font-medium text-gray-700">
                            Full Name
                        </label>
                        <p className="mt-1 text-sm text-gray-900">
                            {user.fullName}
                        </p>
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700">
                            Email
                        </label>
                        <p className="mt-1 text-sm text-gray-900">
                            {user.email}
                        </p>
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700">
                            Roles
                        </label>
                        <div className="mt-1 flex flex-wrap gap-2">
                            {user.roles.map((role) => (
                                <span
                                    key={role}
                                    className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-100 text-indigo-800"
                                >
                                    {role}
                                </span>
                            ))}
                        </div>
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700">
                            Status
                        </label>
                        <span
                            className={`mt-1 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                                user.isOnboarded
                                    ? 'bg-green-100 text-green-800'
                                    : 'bg-yellow-100 text-yellow-800'
                            }`}
                        >
                            {user.isOnboarded ? 'Onboarded' : 'Pending'}
                        </span>
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-gray-700">
                            Last Activity
                        </label>
                        <p className="mt-1 text-sm text-gray-900">
                            {user.lastActivityAt
                                ? new Date(user.lastActivityAt).toLocaleString()
                                : 'Never'}
                        </p>
                    </div>
                </div>
            </div>

            {/* Subscription Information */}
            <div className="bg-white shadow rounded-lg p-6">
                <h2 className="text-xl font-semibold mb-4">
                    Subscription Information
                </h2>
                <div className="space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                        <div>
                            <label className="block text-sm font-medium text-gray-700">
                                Tier
                            </label>
                            <p className="mt-1 text-sm font-semibold text-gray-900 capitalize">
                                {subscription?.tier || 'Free'}
                            </p>
                        </div>
                        <div>
                            <label className="block text-sm font-medium text-gray-700">
                                Status
                            </label>
                            <span
                                className={`mt-1 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                                    isActive
                                        ? 'bg-green-100 text-green-800'
                                        : subscription?.status === 'canceled'
                                        ? 'bg-yellow-100 text-yellow-800'
                                        : 'bg-gray-100 text-gray-800'
                                }`}
                            >
                                {subscription?.status || 'active'}
                            </span>
                        </div>
                        {isExcluded && (
                            <div className="md:col-span-2">
                                <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                                    <div className="flex items-center">
                                        <CheckCircleIcon className="h-5 w-5 text-blue-600 mr-2" />
                                        <span className="text-sm font-medium text-blue-900">
                                            User has free access (excluded from
                                            subscription requirements)
                                        </span>
                                    </div>
                                </div>
                            </div>
                        )}
                        {isActive && subscription?.currentPeriodEnd && (
                            <div>
                                <label className="block text-sm font-medium text-gray-700">
                                    Subscription Expires
                                </label>
                                <p className="mt-1 text-sm text-gray-900">
                                    {formatDate(subscription.currentPeriodEnd)}
                                </p>
                            </div>
                        )}
                        {subscription?.status === 'trialing' &&
                            subscription?.trialEndsAt && (
                                <div>
                                    <label className="block text-sm font-medium text-gray-700">
                                        Trial Ends
                                    </label>
                                    <p className="mt-1 text-sm text-gray-900">
                                        {formatDate(subscription.trialEndsAt)}
                                    </p>
                                </div>
                            )}
                        {!isActive && subscription?.canceledAt && (
                            <div>
                                <label className="block text-sm font-medium text-gray-700">
                                    Expired On
                                </label>
                                <p className="mt-1 text-sm text-gray-900">
                                    {formatDate(subscription.canceledAt)}
                                </p>
                            </div>
                        )}
                    </div>

                    {usageStats && (
                        <div className="border-t border-gray-200 pt-4 mt-4">
                            <h3 className="text-lg font-medium text-gray-900 mb-4">
                                Usage
                            </h3>

                            {usageStats.userType === 'user' ? (
                                <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                                    <UsagePieCard
                                        title="AI queries (daily)"
                                        used={usageStats.usage.aiQueries.daily}
                                        limit={usageStats.limits.dailyAIQueries}
                                    />
                                    <UsagePieCard
                                        title="AI queries (monthly)"
                                        used={usageStats.usage.aiQueries.monthly}
                                        limit={usageStats.limits.monthlyAIQueries}
                                    />
                                    <UsagePieCard
                                        title="Coach messages (monthly)"
                                        used={
                                            usageStats.usage.coachMessages
                                                .monthly
                                        }
                                        limit={
                                            usageStats.limits.monthlyCoachMessages
                                        }
                                    />
                                    <UsagePieCard
                                        title="Coach connections (accepted)"
                                        used={
                                            usageStats.usage.connections
                                                .coachAcceptedCount
                                        }
                                        limit={usageStats.limits.maxCoachConnections}
                                    />
                                    <UsagePieCard
                                        title="Files (count)"
                                        used={usageStats.usage.files.totalCount}
                                        limit={usageStats.limits.maxFiles}
                                    />
                                    <UsagePieCard
                                        title="File storage"
                                        used={usageStats.usage.fileStorage.megabytes}
                                        limit={usageStats.limits.maxFileStorageMB}
                                        unit="MB"
                                        decimals={1}
                                    />
                                    <UsagePieCard
                                        title="Research reports (monthly)"
                                        used={
                                            usageStats.usage.researchReports
                                                .monthly
                                        }
                                        limit={
                                            usageStats.limits.monthlyResearchReports
                                        }
                                    />
                                    <UsagePieCard
                                        title="File uploads (monthly)"
                                        used={
                                            usageStats.usage.fileUploads.monthly
                                        }
                                        limit={usageStats.limits.fileUploadsMonthly}
                                        helperText="No limit configured yet"
                                    />
                                </div>
                            ) : (
                                <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                                    <UsagePieCard
                                        title="Client messages (monthly)"
                                        used={
                                            usageStats.usage.clientMessages
                                                .monthly
                                        }
                                        limit={
                                            usageStats.limits.monthlyClientMessages
                                        }
                                    />
                                    <UsagePieCard
                                        title="Client connections (accepted)"
                                        used={
                                            usageStats.usage.connections
                                                .clientAcceptedCount
                                        }
                                        limit={usageStats.limits.maxClientConnections}
                                    />
                                    <UsagePieCard
                                        title="File uploads (monthly)"
                                        used={
                                            usageStats.usage.fileUploads.monthly
                                        }
                                        limit={usageStats.limits.fileUploadsMonthly}
                                        helperText="No limit configured yet"
                                    />
                                    <UsagePieCard
                                        title="File storage"
                                        used={usageStats.usage.fileStorage.megabytes}
                                        limit={usageStats.limits.maxFileStorageMB}
                                        unit="MB"
                                        decimals={1}
                                    />
                                </div>
                            )}
                        </div>
                    )}

                    {/* Exclusion Management */}
                    <div className="border-t border-gray-200 pt-4 mt-4">
                        <h3 className="text-lg font-medium text-gray-900 mb-4">
                            Free Access Management
                        </h3>
                        {isExcluded ? (
                            <div className="space-y-4">
                                <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                                    <p className="text-sm text-blue-800 mb-4">
                                        This user currently has free access and
                                        is excluded from subscription
                                        requirements.
                                    </p>
                                    <button
                                        onClick={handleRemoveExclusionClick}
                                        disabled={exclusionLoading}
                                        className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 disabled:opacity-50"
                                    >
                                        {exclusionLoading
                                            ? 'Removing...'
                                            : 'Remove Free Access'}
                                    </button>
                                </div>
                            </div>
                        ) : (
                            <div className="space-y-4">
                                <p className="text-sm text-gray-600">
                                    Give this user free access to bypass
                                    subscription requirements.
                                </p>
                                <div>
                                    <label className="block text-sm font-medium text-gray-700 mb-2">
                                        Notes (optional)
                                    </label>
                                    <textarea
                                        value={exclusionNotes}
                                        onChange={(e) =>
                                            setExclusionNotes(e.target.value)
                                        }
                                        placeholder="Reason for giving free access..."
                                        className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                                        rows={3}
                                    />
                                </div>
                                <button
                                    onClick={handleAddExclusionClick}
                                    disabled={exclusionLoading}
                                    className="px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 disabled:opacity-50"
                                >
                                    {exclusionLoading
                                        ? 'Adding...'
                                        : 'Give Free Access'}
                                </button>
                            </div>
                        )}
                    </div>
                </div>
            </div>

            {/* Add Exclusion Confirmation Dialog */}
            <ConfirmationDialog
                isOpen={addExclusionConfirmOpen}
                onClose={() => setAddExclusionConfirmOpen(false)}
                onConfirm={handleAddExclusion}
                title="Give Free Access"
                message="Are you sure you want to give this user free access? They will not need a paid subscription."
                confirmText="Give Free Access"
                cancelText="Cancel"
                variant="info"
            />

            {/* Remove Exclusion Confirmation Dialog */}
            <ConfirmationDialog
                isOpen={removeExclusionConfirmOpen}
                onClose={() => setRemoveExclusionConfirmOpen(false)}
                onConfirm={handleRemoveExclusion}
                title="Remove Free Access"
                message="Are you sure you want to remove free access? The user will need to select a paid tier."
                confirmText="Remove Free Access"
                cancelText="Cancel"
                variant="warning"
            />
        </div>
    );
}
