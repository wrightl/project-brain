'use client';

import { useEffect, useState } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import toast from 'react-hot-toast';
import {
    UserEpisodeMemory,
    UserFactMemory,
    UserMemoryList,
} from '@/_services/user-memory-service';

export default function LearnedMemorySection() {
    const [memories, setMemories] = useState<UserMemoryList>({
        facts: [],
        episodes: [],
    });
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [deletingId, setDeletingId] = useState<string | null>(null);

    useEffect(() => {
        loadMemories();
    }, []);

    const loadMemories = async () => {
        try {
            setLoading(true);
            setError(null);
            const response = await fetchWithAuth('/api/user/memory');
            if (!response.ok) {
                throw new Error('Failed to load learned memories');
            }
            const data = await response.json();
            setMemories({
                facts: data.facts ?? [],
                episodes: data.episodes ?? [],
            });
        } catch (err) {
            const message =
                err instanceof Error
                    ? err.message
                    : 'Failed to load learned memories';
            setError(message);
            toast.error(message);
        } finally {
            setLoading(false);
        }
    };

    const handleDeleteFact = async (fact: UserFactMemory) => {
        if (
            !window.confirm(
                'Remove this learned fact? The assistant will no longer use it in future chats.'
            )
        ) {
            return;
        }

        try {
            setDeletingId(fact.id);
            const response = await fetchWithAuth(
                `/api/user/memory/facts/${fact.id}`,
                { method: 'DELETE' }
            );
            if (!response.ok) {
                throw new Error('Failed to delete memory');
            }
            setMemories((prev) => ({
                ...prev,
                facts: prev.facts.filter((f) => f.id !== fact.id),
            }));
            toast.success('Memory removed');
        } catch (err) {
            toast.error(
                err instanceof Error ? err.message : 'Failed to delete memory'
            );
        } finally {
            setDeletingId(null);
        }
    };

    const handleDeleteEpisode = async (episode: UserEpisodeMemory) => {
        if (
            !window.confirm(
                'Remove this past experience? The assistant will no longer use it in future chats.'
            )
        ) {
            return;
        }

        try {
            setDeletingId(episode.id);
            const response = await fetchWithAuth(
                `/api/user/memory/episodes/${episode.id}`,
                { method: 'DELETE' }
            );
            if (!response.ok) {
                throw new Error('Failed to delete memory');
            }
            setMemories((prev) => ({
                ...prev,
                episodes: prev.episodes.filter((e) => e.id !== episode.id),
            }));
            toast.success('Memory removed');
        } catch (err) {
            toast.error(
                err instanceof Error ? err.message : 'Failed to delete memory'
            );
        } finally {
            setDeletingId(null);
        }
    };

    if (loading) {
        return (
            <div className="bg-white shadow rounded-lg p-6 border border-gray-300">
                <p className="text-gray-600">Loading learned memories...</p>
            </div>
        );
    }

    const isEmpty =
        memories.facts.length === 0 && memories.episodes.length === 0;

    return (
        <div className="bg-white shadow rounded-lg p-6 border border-gray-300">
            <h2 className="text-xl font-semibold text-gray-900">
                Learned memories
            </h2>
            <p className="mt-1 text-sm text-gray-600">
                Facts and experiences the assistant has learned from your
                conversations. Only active memories are shown here.
            </p>

            {error && (
                <p className="mt-4 text-sm text-red-600" role="alert">
                    {error}
                </p>
            )}

            {isEmpty ? (
                <p className="mt-4 text-sm text-gray-600">
                    No learned memories yet. As you chat, the assistant may
                    remember helpful facts and past experiences across
                    conversations.
                </p>
            ) : (
                <div className="mt-6 space-y-8">
                    {memories.facts.length > 0 && (
                        <div>
                            <h3 className="text-sm font-medium text-gray-900">
                                Facts
                            </h3>
                            <ul className="mt-3 divide-y divide-gray-200 border border-gray-300 rounded-md">
                                {memories.facts.map((fact) => (
                                    <li
                                        key={fact.id}
                                        className="flex items-start justify-between gap-4 p-4"
                                    >
                                        <div>
                                            <p className="text-gray-900">
                                                {fact.content}
                                            </p>
                                            <p className="mt-1 text-xs text-gray-600">
                                                {fact.category}
                                            </p>
                                        </div>
                                        <button
                                            type="button"
                                            onClick={() =>
                                                handleDeleteFact(fact)
                                            }
                                            disabled={deletingId === fact.id}
                                            className="shrink-0 text-sm text-red-600 hover:text-red-800 disabled:opacity-50"
                                        >
                                            Remove
                                        </button>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}

                    {memories.episodes.length > 0 && (
                        <div>
                            <h3 className="text-sm font-medium text-gray-900">
                                Past experiences
                            </h3>
                            <ul className="mt-3 divide-y divide-gray-200 border border-gray-300 rounded-md">
                                {memories.episodes.map((episode) => (
                                    <li
                                        key={episode.id}
                                        className="flex items-start justify-between gap-4 p-4"
                                    >
                                        <div>
                                            <p className="text-gray-900">
                                                {episode.summary}
                                            </p>
                                            <p className="mt-1 text-xs text-gray-600">
                                                Topic: {episode.topic} · Outcome:{' '}
                                                {episode.outcome}
                                            </p>
                                        </div>
                                        <button
                                            type="button"
                                            onClick={() =>
                                                handleDeleteEpisode(episode)
                                            }
                                            disabled={
                                                deletingId === episode.id
                                            }
                                            className="shrink-0 text-sm text-red-600 hover:text-red-800 disabled:opacity-50"
                                        >
                                            Remove
                                        </button>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}
