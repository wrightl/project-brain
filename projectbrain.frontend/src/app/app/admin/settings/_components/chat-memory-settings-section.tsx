'use client';

import { useEffect, useState } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import toast from 'react-hot-toast';

interface ChatMemorySettings {
    recentMessageWindow: number;
    conversationSummaryInterval: number;
    maxConversationSummaryLength: number;
    enableConversationSummary: boolean;
}

export default function ChatMemorySettingsSection() {
    const [settings, setSettings] = useState<ChatMemorySettings>({
        recentMessageWindow: 4,
        conversationSummaryInterval: 6,
        maxConversationSummaryLength: 1500,
        enableConversationSummary: true,
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
            const response = await fetchWithAuth('/api/admin/settings/chat-memory');
            if (!response.ok) {
                throw new Error('Failed to load chat memory settings');
            }
            const data = await response.json();
            setSettings({
                recentMessageWindow: Number(data.recentMessageWindow ?? 4),
                conversationSummaryInterval: Number(
                    data.conversationSummaryInterval ?? 6
                ),
                maxConversationSummaryLength: Number(
                    data.maxConversationSummaryLength ?? 1500
                ),
                enableConversationSummary: !!data.enableConversationSummary,
            });
        } catch (err) {
            const message =
                err instanceof Error
                    ? err.message
                    : 'Failed to load chat memory settings';
            setError(message);
            console.error('Error loading chat memory settings:', err);
            toast.error(message);
        } finally {
            setLoading(false);
        }
    };

    const handleNumberChange = (e: React.ChangeEvent<HTMLInputElement>) => {
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

        if (
            settings.recentMessageWindow < 1 ||
            settings.conversationSummaryInterval < 1 ||
            settings.maxConversationSummaryLength < 1
        ) {
            setError('All numeric values must be greater than 0');
            setSaving(false);
            return;
        }

        try {
            const response = await fetchWithAuth('/api/admin/settings/chat-memory', {
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
                        'Failed to update chat memory settings'
                );
            }

            const data = await response.json();
            setSettings({
                recentMessageWindow: Number(data.recentMessageWindow ?? 4),
                conversationSummaryInterval: Number(
                    data.conversationSummaryInterval ?? 6
                ),
                maxConversationSummaryLength: Number(
                    data.maxConversationSummaryLength ?? 1500
                ),
                enableConversationSummary: !!data.enableConversationSummary,
            });
            toast.success('Chat memory settings updated successfully');
        } catch (err) {
            const message =
                err instanceof Error
                    ? err.message
                    : 'Failed to update chat memory settings';
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
                Chat Memory Settings
            </h2>
            <p className="text-sm text-gray-600 mb-6">
                Configure rolling conversation summaries and how much recent
                chat history is included alongside them.
            </p>

            <form onSubmit={handleSubmit} className="space-y-6">
                {error && (
                    <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
                        {error}
                    </div>
                )}

                <label className="flex items-center justify-between gap-4">
                    <div>
                        <div className="text-sm font-medium text-gray-900">
                            Enable conversation summaries
                        </div>
                        <div className="text-xs text-gray-500">
                            When off, chat uses full history window only (no
                            rolling summary injection).
                        </div>
                    </div>
                    <input
                        type="checkbox"
                        checked={settings.enableConversationSummary}
                        onChange={() =>
                            setSettings((prev) => ({
                                ...prev,
                                enableConversationSummary:
                                    !prev.enableConversationSummary,
                            }))
                        }
                        className="h-5 w-5 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                    />
                </label>

                <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                    <div>
                        <label
                            htmlFor="recentMessageWindow"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Recent message window
                        </label>
                        <input
                            type="number"
                            id="recentMessageWindow"
                            name="recentMessageWindow"
                            min="1"
                            required
                            value={settings.recentMessageWindow}
                            onChange={handleNumberChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                        <p className="mt-1 text-xs text-gray-500">
                            Raw messages kept when a summary is present
                        </p>
                    </div>

                    <div>
                        <label
                            htmlFor="conversationSummaryInterval"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Summary interval (messages)
                        </label>
                        <input
                            type="number"
                            id="conversationSummaryInterval"
                            name="conversationSummaryInterval"
                            min="1"
                            required
                            value={settings.conversationSummaryInterval}
                            onChange={handleNumberChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                        <p className="mt-1 text-xs text-gray-500">
                            Regenerate summary every N persisted messages
                        </p>
                    </div>

                    <div>
                        <label
                            htmlFor="maxConversationSummaryLength"
                            className="block text-sm font-medium text-gray-700"
                        >
                            Max summary length
                        </label>
                        <input
                            type="number"
                            id="maxConversationSummaryLength"
                            name="maxConversationSummaryLength"
                            min="1"
                            required
                            value={settings.maxConversationSummaryLength}
                            onChange={handleNumberChange}
                            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                        />
                        <p className="mt-1 text-xs text-gray-500">
                            Maximum stored summary length in characters
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
