'use client';

import { useEffect, useState } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import toast from 'react-hot-toast';

interface AISettings {
    maxSearchResults: number;
    maxContentLengthPerSource: number;
    maxHistoryMessages: number;
    maxTotalTokens: number;
}

export default function AISettingsSection() {
    const [settings, setSettings] = useState<AISettings>({
        maxSearchResults: 5,
        maxContentLengthPerSource: 800,
        maxHistoryMessages: 10,
        maxTotalTokens: 7000,
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
            const response = await fetchWithAuth('/api/admin/settings/ai');
            if (!response.ok) {
                throw new Error('Failed to load AI settings');
            }
            const data = await response.json();
            setSettings(data);
        } catch (err) {
            setError(
                err instanceof Error ? err.message : 'Failed to load AI settings'
            );
            console.error('Error loading AI settings:', err);
            toast.error(
                err instanceof Error
                    ? err.message
                    : 'Failed to load AI settings'
            );
        } finally {
            setLoading(false);
        }
    };

    const handleChange = (
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
            settings.maxSearchResults < 1 ||
            settings.maxContentLengthPerSource < 1 ||
            settings.maxHistoryMessages < 1 ||
            settings.maxTotalTokens < 1
        ) {
            setError('All values must be greater than 0');
            setSaving(false);
            return;
        }

        try {
            const response = await fetchWithAuth('/api/admin/settings/ai', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(settings),
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(
                    errorData.error || 'Failed to update AI settings'
                );
            }

            toast.success('AI settings updated successfully');
        } catch (err) {
            setError(
                err instanceof Error ? err.message : 'Failed to update AI settings'
            );
            toast.error(
                err instanceof Error
                    ? err.message
                    : 'Failed to update AI settings'
            );
        } finally {
            setSaving(false);
        }
    };

    if (loading) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <div className="animate-pulse">
                    <div className="h-6 bg-gray-200 rounded w-1/4 mb-4"></div>
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
                AI Settings
            </h2>
            <p className="text-sm text-gray-600 mb-6">
                Configure AI model parameters and limits
            </p>

            <form onSubmit={handleSubmit} className="space-y-6">
                {error && (
                    <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
                        {error}
                    </div>
                )}

                <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                    <div>
                        <label
                            htmlFor="maxSearchResults"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Max Search Results
                        </label>
                        <input
                            type="number"
                            id="maxSearchResults"
                            name="maxSearchResults"
                            min="1"
                            required
                            value={settings.maxSearchResults}
                            onChange={handleChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                        <p className="mt-1 text-xs text-gray-500">
                            Maximum number of search results to return
                        </p>
                    </div>

                    <div>
                        <label
                            htmlFor="maxContentLengthPerSource"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Max Content Length Per Source
                        </label>
                        <input
                            type="number"
                            id="maxContentLengthPerSource"
                            name="maxContentLengthPerSource"
                            min="1"
                            required
                            value={settings.maxContentLengthPerSource}
                            onChange={handleChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                        <p className="mt-1 text-xs text-gray-500">
                            Maximum content length per source in characters
                        </p>
                    </div>

                    <div>
                        <label
                            htmlFor="maxHistoryMessages"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Max History Messages
                        </label>
                        <input
                            type="number"
                            id="maxHistoryMessages"
                            name="maxHistoryMessages"
                            min="1"
                            required
                            value={settings.maxHistoryMessages}
                            onChange={handleChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                        <p className="mt-1 text-xs text-gray-500">
                            Maximum number of history messages to include
                        </p>
                    </div>

                    <div>
                        <label
                            htmlFor="maxTotalTokens"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Max Total Tokens
                        </label>
                        <input
                            type="number"
                            id="maxTotalTokens"
                            name="maxTotalTokens"
                            min="1"
                            required
                            value={settings.maxTotalTokens}
                            onChange={handleChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                        <p className="mt-1 text-xs text-gray-500">
                            Maximum total tokens allowed
                        </p>
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
