'use client';

import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { FormEvent, useCallback, useEffect, useState } from 'react';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';
import { useCommunityFeatureFlag } from '@/_hooks/use-feature-flag';
import type { CommunityPost } from '@/_services/community-service';

export default function CommunityChannelPage() {
    const params = useParams<{ slug: string }>();
    const slug = params.slug;
    const { enabled: communityEnabled, loading: flagLoading } =
        useCommunityFeatureFlag();
    const router = useRouter();

    const [posts, setPosts] = useState<CommunityPost[]>([]);
    const [nextCursor, setNextCursor] = useState<string | null>(null);
    const [body, setBody] = useState('');
    const [loading, setLoading] = useState(true);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const loadPosts = useCallback(
        async (cursor?: string | null, append = false) => {
            const qs = new URLSearchParams();
            if (cursor) qs.set('cursor', cursor);
            const response = await fetchWithAuth(
                `/api/community/channels/${encodeURIComponent(slug)}/posts?${qs}`,
            );
            if (!response.ok) {
                throw new Error('Failed to load posts');
            }
            const data = (await response.json()) as {
                items: CommunityPost[];
                nextCursor?: string | null;
            };
            setPosts((prev) => (append ? [...prev, ...data.items] : data.items));
            setNextCursor(data.nextCursor ?? null);
        },
        [slug],
    );

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
                await loadPosts();
                if (!cancelled) setError(null);
            } catch (err) {
                if (!cancelled) {
                    setError(
                        err instanceof Error
                            ? err.message
                            : 'Failed to load posts',
                    );
                }
            } finally {
                if (!cancelled) setLoading(false);
            }
        })();

        return () => {
            cancelled = true;
        };
    }, [flagLoading, communityEnabled, router, loadPosts]);

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        if (!body.trim()) return;
        setSubmitting(true);
        try {
            const response = await fetchWithAuth(
                `/api/community/channels/${encodeURIComponent(slug)}/posts`,
                {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ body }),
                },
            );
            if (!response.ok) {
                throw new Error('Failed to post');
            }
            setBody('');
            await loadPosts();
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to post');
        } finally {
            setSubmitting(false);
        }
    };

    const toggleReaction = async (post: CommunityPost) => {
        const method = post.reactedByCurrentUser ? 'DELETE' : 'POST';
        const response = await fetchWithAuth(
            `/api/community/posts/${post.id}/reactions`,
            { method },
        );
        if (!response.ok) return;
        setPosts((prev) =>
            prev.map((p) =>
                p.id === post.id
                    ? {
                          ...p,
                          reactedByCurrentUser: !p.reactedByCurrentUser,
                          reactionCount: p.reactedByCurrentUser
                              ? Math.max(0, p.reactionCount - 1)
                              : p.reactionCount + 1,
                      }
                    : p,
            ),
        );
    };

    const deletePost = async (postId: string) => {
        const response = await fetchWithAuth(`/api/community/posts/${postId}`, {
            method: 'DELETE',
        });
        if (!response.ok) return;
        setPosts((prev) => prev.filter((p) => p.id !== postId));
    };

    const reportPost = async (postId: string) => {
        const reason = window.prompt('Why are you reporting this post?');
        if (!reason?.trim()) return;
        await fetchWithAuth(`/api/community/posts/${postId}/reports`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ reason }),
        });
    };

    if (flagLoading) {
        return (
            <div className="max-w-3xl mx-auto">
                <p className="text-gray-600">Loading…</p>
            </div>
        );
    }

    if (!communityEnabled) return null;

    return (
        <div className="max-w-3xl mx-auto space-y-6">
            <div className="flex items-center justify-between gap-4">
                <div>
                    <Link
                        href="/app/user/community"
                        className="text-sm text-gray-600 hover:text-gray-900"
                    >
                        ← All channels
                    </Link>
                    <h1 className="text-2xl font-semibold text-gray-900 mt-1 capitalize">
                        {slug.replace(/-/g, ' ')}
                    </h1>
                </div>
            </div>

            <form onSubmit={handleSubmit} className="space-y-3">
                <textarea
                    value={body}
                    onChange={(e) => setBody(e.target.value)}
                    maxLength={2000}
                    rows={3}
                    placeholder="Share with the community…"
                    className="w-full rounded-lg border border-gray-300 bg-white p-3 text-gray-900"
                />
                <button
                    type="submit"
                    disabled={submitting || !body.trim()}
                    className="rounded-md bg-gray-900 px-4 py-2 text-sm font-medium text-white disabled:opacity-50"
                >
                    {submitting ? 'Posting…' : 'Post'}
                </button>
            </form>

            {loading && <p className="text-gray-600">Loading posts…</p>}
            {error && <p className="text-red-600">{error}</p>}

            <ul className="space-y-4">
                {posts.map((post) => (
                    <li
                        key={post.id}
                        className="rounded-lg border border-gray-300 bg-white p-4"
                    >
                        <div className="flex items-start justify-between gap-3">
                            <div>
                                <p className="text-sm font-medium text-gray-900">
                                    {post.authorDisplayName}
                                </p>
                                <p className="text-xs text-gray-500">
                                    {new Date(post.createdAt).toLocaleString()}
                                </p>
                            </div>
                            <div className="flex gap-2 text-sm">
                                <button
                                    type="button"
                                    onClick={() => toggleReaction(post)}
                                    className="text-gray-600 hover:text-gray-900"
                                >
                                    {post.reactedByCurrentUser ? 'Unlike' : 'Like'}{' '}
                                    ({post.reactionCount})
                                </button>
                                <button
                                    type="button"
                                    onClick={() => reportPost(post.id)}
                                    className="text-gray-600 hover:text-gray-900"
                                >
                                    Report
                                </button>
                                {post.isOwnPost && (
                                    <button
                                        type="button"
                                        onClick={() => deletePost(post.id)}
                                        className="text-red-600 hover:text-red-700"
                                    >
                                        Delete
                                    </button>
                                )}
                            </div>
                        </div>
                        <p className="mt-3 whitespace-pre-wrap text-gray-900">
                            {post.body}
                        </p>
                    </li>
                ))}
            </ul>

            {nextCursor && (
                <button
                    type="button"
                    onClick={() => loadPosts(nextCursor, true)}
                    className="text-sm text-gray-700 hover:text-gray-900"
                >
                    Load more
                </button>
            )}
        </div>
    );
}
