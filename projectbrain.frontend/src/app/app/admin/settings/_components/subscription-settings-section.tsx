'use client';

import { useEffect, useState } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import toast from 'react-hot-toast';

interface SubscriptionSettings {
    enableUserSubscriptions: boolean;
    enableCoachSubscriptions: boolean;
}

export default function SubscriptionSettingsSection() {
    const [settings, setSettings] = useState<SubscriptionSettings>({
        enableUserSubscriptions: true,
        enableCoachSubscriptions: true,
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
            const response = await fetchWithAuth(
                '/api/admin/settings/subscription',
            );
            if (!response.ok) {
                throw new Error('Failed to load subscription settings');
            }
            const data = await response.json();
            setSettings({
                enableUserSubscriptions: !!data.enableUserSubscriptions,
                enableCoachSubscriptions: !!data.enableCoachSubscriptions,
            });
        } catch (err) {
            const message =
                err instanceof Error
                    ? err.message
                    : 'Failed to load subscription settings';
            setError(message);
            console.error('Error loading subscription settings:', err);
            toast.error(message);
        } finally {
            setLoading(false);
        }
    };

    const handleToggle = (key: keyof SubscriptionSettings) => {
        setSettings((prev) => ({
            ...prev,
            [key]: !prev[key],
        }));
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setSaving(true);

        try {
            const response = await fetchWithAuth(
                '/api/admin/settings/subscription',
                {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(settings),
                },
            );

            if (!response.ok) {
                const errorData = await response.json().catch(() => null);
                throw new Error(
                    (errorData && (errorData.error || errorData.message)) ||
                        'Failed to update subscription settings',
                );
            }

            toast.success('Subscription settings updated successfully');
        } catch (err) {
            const message =
                err instanceof Error
                    ? err.message
                    : 'Failed to update subscription settings';
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
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="bg-white shadow rounded-lg p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">
                Subscription Settings
            </h2>
            <p className="text-sm text-gray-600 mb-6">
                Enable or disable subscriptions globally. Disabling a user type
                forces that audience onto the Free tier.
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
                                Enable user subscriptions
                            </div>
                            <div className="text-xs text-gray-500">
                                When off, all regular users are treated as Free
                                tier.
                            </div>
                        </div>
                        <input
                            type="checkbox"
                            checked={settings.enableUserSubscriptions}
                            onChange={() =>
                                handleToggle('enableUserSubscriptions')
                            }
                            className="h-5 w-5 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                        />
                    </label>

                    <label className="flex items-center justify-between gap-4">
                        <div>
                            <div className="text-sm font-medium text-gray-900">
                                Enable coach subscriptions
                            </div>
                            <div className="text-xs text-gray-500">
                                When off, all coaches are treated as Free tier.
                            </div>
                        </div>
                        <input
                            type="checkbox"
                            checked={settings.enableCoachSubscriptions}
                            onChange={() =>
                                handleToggle('enableCoachSubscriptions')
                            }
                            className="h-5 w-5 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                        />
                    </label>
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
