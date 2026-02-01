'use client';

import { useEffect, useState } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import toast from 'react-hot-toast';

interface ReferralSettings {
    enabled: boolean;
    maxRewardsPerInviter: number;
    inviterFreeMonths: number;
    inviteeFreeMonths: number;
    inviteTokenExpiryDays: number;
    maxInvitesPerRequest: number;
    requireInviterActiveSubscriberToEarn: boolean;
}

export default function ReferralSettingsSection() {
    const [settings, setSettings] = useState<ReferralSettings>({
        enabled: false,
        maxRewardsPerInviter: 12,
        inviterFreeMonths: 1,
        inviteeFreeMonths: 1,
        inviteTokenExpiryDays: 30,
        maxInvitesPerRequest: 10,
        requireInviterActiveSubscriberToEarn: false,
    });
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        loadSettings();
    }, []);

    const loadSettings = async () => {
        try {
            setLoading(true);
            setError(null);
            const response = await fetchWithAuth('/api/admin/settings/referrals');
            if (!response.ok) {
                throw new Error('Failed to load referral settings');
            }
            const data = await response.json();
            setSettings({
                enabled: !!data.enabled,
                maxRewardsPerInviter: Number(data.maxRewardsPerInviter ?? 12),
                inviterFreeMonths: Number(data.inviterFreeMonths ?? 1),
                inviteeFreeMonths: Number(data.inviteeFreeMonths ?? 1),
                inviteTokenExpiryDays: Number(data.inviteTokenExpiryDays ?? 30),
                maxInvitesPerRequest: Number(data.maxInvitesPerRequest ?? 10),
                requireInviterActiveSubscriberToEarn:
                    !!data.requireInviterActiveSubscriberToEarn,
            });
        } catch (err) {
            const message =
                err instanceof Error
                    ? err.message
                    : 'Failed to load referral settings';
            setError(message);
            console.error('Error loading referral settings:', err);
            toast.error(message);
        } finally {
            setLoading(false);
        }
    };

    const handleToggle = (key: keyof ReferralSettings) => {
        setSettings((prev) => ({
            ...prev,
            [key]: !prev[key],
        }));
    };

    const handleNumberChange = (
        e: React.ChangeEvent<HTMLInputElement>
    ) => {
        const { name, value } = e.target;
        setSettings((prev) => ({
            ...prev,
            [name]: parseInt(value, 10) || 0,
        }));
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setSaving(true);

        // Validate
        if (
            settings.maxRewardsPerInviter < 0 ||
            settings.inviterFreeMonths < 0 ||
            settings.inviteeFreeMonths < 0 ||
            settings.inviteTokenExpiryDays < 1 ||
            settings.maxInvitesPerRequest < 1 ||
            settings.maxInvitesPerRequest > 10
        ) {
            setError(
                'Please enter valid values (max invites per request must be 1–10)'
            );
            setSaving(false);
            return;
        }

        try {
            const response = await fetchWithAuth('/api/admin/settings/referrals', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(settings),
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => null);
                throw new Error(
                    (errorData && (errorData.error || errorData.message)) ||
                        'Failed to update referral settings'
                );
            }

            toast.success('Referral settings updated successfully');
        } catch (err) {
            const message =
                err instanceof Error
                    ? err.message
                    : 'Failed to update referral settings';
            setError(message);
            toast.error(message);
        } finally {
            setSaving(false);
        }
    };

    if (loading) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <div className="animate-pulse">
                    <div className="h-6 bg-gray-200 rounded w-1/3 mb-4"></div>
                    <div className="space-y-4">
                        <div className="h-4 bg-gray-200 rounded"></div>
                        <div className="h-4 bg-gray-200 rounded"></div>
                        <div className="h-4 bg-gray-200 rounded"></div>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="bg-white shadow rounded-lg p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">
                Referral Settings
            </h2>
            <p className="text-sm text-gray-600 mb-6">
                Configure the referral program (invite by email and award free
                months after the invitee becomes a paying subscriber beyond the
                free trial).
            </p>

            <form onSubmit={handleSubmit} className="space-y-6">
                {error && (
                    <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
                        {error}
                    </div>
                )}

                <div className="space-y-4">
                    <label className="flex items-center justify-between gap-4">
                        <div>
                            <div className="text-sm font-medium text-gray-900">
                                Enable referrals
                            </div>
                            <div className="text-xs text-gray-500">
                                When off, users won’t be able to send invites.
                            </div>
                        </div>
                        <input
                            type="checkbox"
                            checked={settings.enabled}
                            onChange={() => handleToggle('enabled')}
                            className="h-5 w-5 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                        />
                    </label>

                    <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                        <div>
                            <label
                                htmlFor="maxRewardsPerInviter"
                                className="block text-sm font-medium text-gray-700"
                            >
                                Max rewards per inviter
                            </label>
                            <input
                                type="number"
                                id="maxRewardsPerInviter"
                                name="maxRewardsPerInviter"
                                min="0"
                                required
                                value={settings.maxRewardsPerInviter}
                                onChange={handleNumberChange}
                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            />
                            <p className="mt-1 text-xs text-gray-500">
                                Limits how many successful referrals an inviter
                                can be rewarded for.
                            </p>
                        </div>

                        <div>
                            <label
                                htmlFor="maxInvitesPerRequest"
                                className="block text-sm font-medium text-gray-700"
                            >
                                Max invites per send
                            </label>
                            <input
                                type="number"
                                id="maxInvitesPerRequest"
                                name="maxInvitesPerRequest"
                                min="1"
                                max="10"
                                required
                                value={settings.maxInvitesPerRequest}
                                onChange={handleNumberChange}
                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            />
                            <p className="mt-1 text-xs text-gray-500">
                                Hard limit per request (1–10).
                            </p>
                        </div>

                        <div>
                            <label
                                htmlFor="inviterFreeMonths"
                                className="block text-sm font-medium text-gray-700"
                            >
                                Inviter free months (per successful referral)
                            </label>
                            <input
                                type="number"
                                id="inviterFreeMonths"
                                name="inviterFreeMonths"
                                min="0"
                                required
                                value={settings.inviterFreeMonths}
                                onChange={handleNumberChange}
                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            />
                        </div>

                        <div>
                            <label
                                htmlFor="inviteeFreeMonths"
                                className="block text-sm font-medium text-gray-700"
                            >
                                Invitee free months
                            </label>
                            <input
                                type="number"
                                id="inviteeFreeMonths"
                                name="inviteeFreeMonths"
                                min="0"
                                required
                                value={settings.inviteeFreeMonths}
                                onChange={handleNumberChange}
                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            />
                        </div>

                        <div>
                            <label
                                htmlFor="inviteTokenExpiryDays"
                                className="block text-sm font-medium text-gray-700"
                            >
                                Invite link expiry (days)
                            </label>
                            <input
                                type="number"
                                id="inviteTokenExpiryDays"
                                name="inviteTokenExpiryDays"
                                min="1"
                                required
                                value={settings.inviteTokenExpiryDays}
                                onChange={handleNumberChange}
                                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            />
                            <p className="mt-1 text-xs text-gray-500">
                                Unaccepted invites expire after this many days.
                            </p>
                        </div>

                        <div className="sm:col-span-2">
                            <label className="flex items-center justify-between gap-4">
                                <div>
                                    <div className="text-sm font-medium text-gray-900">
                                        Require inviter to be paid to earn
                                    </div>
                                    <div className="text-xs text-gray-500">
                                        If enabled, inviter rewards are only
                                        applied when the inviter is an active
                                        paid subscriber.
                                    </div>
                                </div>
                                <input
                                    type="checkbox"
                                    checked={
                                        settings.requireInviterActiveSubscriberToEarn
                                    }
                                    onChange={() =>
                                        handleToggle(
                                            'requireInviterActiveSubscriberToEarn'
                                        )
                                    }
                                    className="h-5 w-5 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                                />
                            </label>
                        </div>
                    </div>
                </div>

                <div className="flex justify-end space-x-3 pt-4 border-t border-gray-200">
                    <button
                        type="submit"
                        disabled={saving}
                        className="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:bg-gray-400"
                    >
                        {saving ? 'Saving...' : 'Save Changes'}
                    </button>
                </div>
            </form>
        </div>
    );
}

