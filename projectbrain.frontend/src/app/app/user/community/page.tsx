'use client';

import Link from 'next/link';
import { useCallback, useEffect, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import { ArrowPathIcon } from '@heroicons/react/24/outline';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { useCommunityFeatureFlag } from '@/_hooks/use-feature-flag';
import type {
    CommunityChannel,
    CommunityPost,
} from '@/_services/community-service';

const FEED_LIMIT = 20;

function previewBody(body: string, max = 180): string {
    const trimmed = body.trim();
    if (trimmed.length <= max) return trimmed;
    return `${trimmed.slice(0, max).trimEnd()}…`;
}

export default function CommunityHubPage() {
    const { enabled: communityEnabled, loading: flagLoading } =
        useCommunityFeatureFlag();
    const router = useRouter();
    const [channels, setChannels] = useState<CommunityChannel[]>([]);
    const [posts, setPosts] = useState<CommunityPost[]>([]);
    const [nextCursor, setNextCursor] = useState<string | null>(null);
    const [loading, setLoading] = useState(true);
    const [refreshing, setRefreshing] = useState(false);
    const [loadingMore, setLoadingMore] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const sentinelRef = useRef<HTMLDivElement | null>(null);
    const loadingMoreRef = useRef(false);
    const nextCursorRef = useRef<string | null>(null);

    useEffect(() => {
        nextCursorRef.current = nextCursor;
    }, [nextCursor]);

    const loadChannels = useCallback(async () => {
        const response = await fetchWithAuth('/api/community/channels');
        if (!response.ok) {
            throw new Error('Failed to load channels');
        }
        return (await response.json()) as CommunityChannel[];
    }, []);

    const loadFeedPage = useCallback(async (cursor?: string | null) => {
        const params = new URLSearchParams({ limit: String(FEED_LIMIT) });
        if (cursor) params.set('cursor', cursor);
        const response = await fetchWithAuth(
            `/api/community/feed?${params.toString()}`,
        );
        if (!response.ok) {
            throw new Error('Failed to load feed');
        }
        return (await response.json()) as {
            items: CommunityPost[];
            nextCursor?: string | null;
        };
    }, []);

    const loadInitial = useCallback(async () => {
        const [channelData, feedData] = await Promise.all([
            loadChannels(),
            loadFeedPage(),
        ]);
        setChannels(channelData);
        setPosts(feedData.items);
        setNextCursor(feedData.nextCursor ?? null);
    }, [loadChannels, loadFeedPage]);

    useEffect(() => {
        if (flagLoading) return;
        if (!communityEnabled) {
            router.replace('/app/user/chat');
            return;
        }

        let cancelled = false;
        (async () => {
            try {
                setLoading(true);
                await loadInitial();
                if (!cancelled) setError(null);
            } catch (err) {
                if (!cancelled) {
                    setError(
                        err instanceof Error
                            ? err.message
                            : 'Failed to load community',
                    );
                }
            } finally {
                if (!cancelled) setLoading(false);
            }
        })();

        return () => {
            cancelled = true;
        };
    }, [flagLoading, communityEnabled, router, loadInitial]);

    const handleRefresh = async () => {
        try {
            setRefreshing(true);
            await loadInitial();
            setError(null);
        } catch (err) {
            setError(
                err instanceof Error ? err.message : 'Failed to refresh',
            );
        } finally {
            setRefreshing(false);
        }
    };

    const loadMore = useCallback(async () => {
        const cursor = nextCursorRef.current;
        if (!cursor || loadingMoreRef.current) return;
        loadingMoreRef.current = true;
        setLoadingMore(true);
        try {
            const feedData = await loadFeedPage(cursor);
            setPosts((prev) => {
                const seen = new Set(prev.map((p) => p.id));
                const appended = feedData.items.filter((p) => !seen.has(p.id));
                return [...prev, ...appended];
            });
            setNextCursor(feedData.nextCursor ?? null);
        } catch (err) {
            setError(
                err instanceof Error ? err.message : 'Failed to load more',
            );
        } finally {
            loadingMoreRef.current = false;
            setLoadingMore(false);
        }
    }, [loadFeedPage]);

    useEffect(() => {
        const node = sentinelRef.current;
        if (!node || !nextCursor) return;

        const observer = new IntersectionObserver(
            (entries) => {
                if (entries.some((e) => e.isIntersecting)) {
                    void loadMore();
                }
            },
            { root: null, rootMargin: '200px', threshold: 0 },
        );
        observer.observe(node);
        return () => observer.disconnect();
    }, [nextCursor, loadMore, posts.length]);

    if (flagLoading) {
        return (
            <div className="max-w-3xl mx-auto">
                <p className="text-gray-600">Loading…</p>
            </div>
        );
    }

    if (!communityEnabled) {
        return null;
    }

    return (
        <div className="max-w-3xl mx-auto space-y-6">
            <div>
                <h1 className="text-2xl font-semibold text-gray-900">
                    Community
                </h1>
                <p className="mt-2 text-gray-600">
                    Join the conversation across channels — share wins, tips,
                    and support.
                </p>
            </div>

            <section className="space-y-3">
                <div className="flex items-center justify-between gap-3">
                    <h2 className="text-lg font-semibold text-gray-900">
                        Latest
                    </h2>
                    <button
                        type="button"
                        onClick={() => void handleRefresh()}
                        disabled={refreshing || loading}
                        aria-label="Refresh feed"
                        title="Refresh"
                        className="inline-flex items-center justify-center rounded-md border border-gray-300 bg-white p-2 text-gray-700 hover:bg-gray-50 disabled:opacity-50"
                    >
                        <ArrowPathIcon
                            className={`h-5 w-5 ${refreshing ? 'animate-spin' : ''}`}
                            aria-hidden="true"
                        />
                    </button>
                </div>

                {loading && <p className="text-gray-600">Loading feed…</p>}
                {error && <p className="text-red-600">{error}</p>}
                {!loading && !error && posts.length === 0 && (
                    <p className="text-gray-600">
                        No posts yet. Be the first to share in a channel.
                    </p>
                )}

                <ul className="space-y-3">
                    {posts.map((post) => (
                        <li key={post.id}>
                            <Link
                                href={`/app/user/community/${post.channelSlug}`}
                                className="block rounded-lg border border-gray-300 bg-white p-4 hover:bg-gray-50"
                            >
                                <div className="flex items-start justify-between gap-3">
                                    <div>
                                        <p className="text-sm font-medium text-gray-900">
                                            {post.authorDisplayName}
                                        </p>
                                        <p className="text-xs text-gray-500">
                                            #{post.channelSlug} ·{' '}
                                            {new Date(
                                                post.createdAt,
                                            ).toLocaleString()}
                                        </p>
                                    </div>
                                    <span className="text-xs text-gray-500">
                                        {post.reactionCount} likes
                                    </span>
                                </div>
                                <p className="mt-2 whitespace-pre-wrap text-gray-900">
                                    {previewBody(post.body)}
                                </p>
                            </Link>
                        </li>
                    ))}
                </ul>

                {nextCursor && <div ref={sentinelRef} className="h-4" />}
                {loadingMore && (
                    <p className="text-sm text-gray-600">Loading more…</p>
                )}
            </section>

            <section className="space-y-3">
                <h2 className="text-lg font-semibold text-gray-900">
                    Channels
                </h2>
                <ul className="space-y-3">
                    {channels.map((channel) => (
                        <li key={channel.id}>
                            <Link
                                href={`/app/user/community/${channel.slug}`}
                                className="block rounded-lg border border-gray-300 bg-white p-4 hover:bg-gray-50"
                            >
                                <p className="font-medium text-gray-900">
                                    {channel.name}
                                </p>
                                {channel.description && (
                                    <p className="text-sm text-gray-600 mt-1">
                                        {channel.description}
                                    </p>
                                )}
                            </Link>
                        </li>
                    ))}
                </ul>
            </section>
        </div>
    );
}
