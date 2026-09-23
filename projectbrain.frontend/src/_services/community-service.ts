import { callBackendApi } from '@/_lib/backend-api';

export interface CommunityChannel {
    id: string;
    slug: string;
    name: string;
    description?: string | null;
    sortOrder: number;
}

export interface CommunityPost {
    id: string;
    channelId: string;
    channelSlug: string;
    authorUserId: string;
    authorDisplayName: string;
    body: string;
    createdAt: string;
    updatedAt: string;
    reactionCount: number;
    reactedByCurrentUser: boolean;
    isOwnPost: boolean;
}

export interface CommunityPostsPage {
    items: CommunityPost[];
    nextCursor?: string | null;
}

export interface CommunityReport {
    id: string;
    postId: string;
    postBodyPreview: string;
    channelSlug: string;
    reporterUserId: string;
    reporterDisplayName: string;
    reason: string;
    status: string;
    createdAt: string;
}

function normalizeChannel(raw: Record<string, unknown>): CommunityChannel {
    return {
        id: String(raw.id ?? raw.Id ?? ''),
        slug: String(raw.slug ?? raw.Slug ?? ''),
        name: String(raw.name ?? raw.Name ?? ''),
        description: (raw.description ?? raw.Description ?? null) as
            | string
            | null,
        sortOrder: Number(raw.sortOrder ?? raw.SortOrder ?? 0),
    };
}

function normalizePost(raw: Record<string, unknown>): CommunityPost {
    return {
        id: String(raw.id ?? raw.Id ?? ''),
        channelId: String(raw.channelId ?? raw.ChannelId ?? ''),
        channelSlug: String(raw.channelSlug ?? raw.ChannelSlug ?? ''),
        authorUserId: String(raw.authorUserId ?? raw.AuthorUserId ?? ''),
        authorDisplayName: String(
            raw.authorDisplayName ?? raw.AuthorDisplayName ?? 'Member',
        ),
        body: String(raw.body ?? raw.Body ?? ''),
        createdAt: String(raw.createdAt ?? raw.CreatedAt ?? ''),
        updatedAt: String(raw.updatedAt ?? raw.UpdatedAt ?? ''),
        reactionCount: Number(raw.reactionCount ?? raw.ReactionCount ?? 0),
        reactedByCurrentUser: !!(
            raw.reactedByCurrentUser ?? raw.ReactedByCurrentUser
        ),
        isOwnPost: !!(raw.isOwnPost ?? raw.IsOwnPost),
    };
}

export class CommunityService {
    static async getChannels(): Promise<CommunityChannel[]> {
        const response = await callBackendApi('/community/channels');
        if (!response.ok) {
            throw new Error('Failed to load community channels');
        }
        const data = (await response.json()) as Record<string, unknown>[];
        return data.map(normalizeChannel);
    }

    static async getPosts(
        slug: string,
        options?: { cursor?: string; limit?: number },
    ): Promise<CommunityPostsPage> {
        const params = new URLSearchParams();
        if (options?.cursor) params.set('cursor', options.cursor);
        if (options?.limit) params.set('limit', String(options.limit));
        const qs = params.toString();
        const response = await callBackendApi(
            `/community/channels/${encodeURIComponent(slug)}/posts${qs ? `?${qs}` : ''}`,
        );
        if (!response.ok) {
            throw new Error('Failed to load community posts');
        }
        const data = (await response.json()) as Record<string, unknown>;
        const items = (data.items ?? data.Items ?? []) as Record<
            string,
            unknown
        >[];
        return {
            items: items.map(normalizePost),
            nextCursor: (data.nextCursor ?? data.NextCursor ?? null) as
                | string
                | null,
        };
    }

    static async getLatestFeed(options?: {
        cursor?: string;
        limit?: number;
    }): Promise<CommunityPostsPage> {
        const params = new URLSearchParams();
        if (options?.cursor) params.set('cursor', options.cursor);
        if (options?.limit) params.set('limit', String(options.limit));
        const qs = params.toString();
        const response = await callBackendApi(
            `/community/feed${qs ? `?${qs}` : ''}`,
        );
        if (!response.ok) {
            throw new Error('Failed to load community feed');
        }
        const data = (await response.json()) as Record<string, unknown>;
        const items = (data.items ?? data.Items ?? []) as Record<
            string,
            unknown
        >[];
        return {
            items: items.map(normalizePost),
            nextCursor: (data.nextCursor ?? data.NextCursor ?? null) as
                | string
                | null,
        };
    }

    static async createPost(slug: string, body: string): Promise<CommunityPost> {
        const response = await callBackendApi(
            `/community/channels/${encodeURIComponent(slug)}/posts`,
            {
                method: 'POST',
                body: { body },
            },
        );
        if (!response.ok) {
            throw new Error('Failed to create post');
        }
        return normalizePost((await response.json()) as Record<string, unknown>);
    }

    static async deletePost(id: string): Promise<void> {
        const response = await callBackendApi(`/community/posts/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) {
            throw new Error('Failed to delete post');
        }
    }

    static async addReaction(id: string): Promise<void> {
        const response = await callBackendApi(
            `/community/posts/${id}/reactions`,
            { method: 'POST' },
        );
        if (!response.ok) {
            throw new Error('Failed to add reaction');
        }
    }

    static async removeReaction(id: string): Promise<void> {
        const response = await callBackendApi(
            `/community/posts/${id}/reactions`,
            { method: 'DELETE' },
        );
        if (!response.ok) {
            throw new Error('Failed to remove reaction');
        }
    }

    static async reportPost(id: string, reason: string): Promise<void> {
        const response = await callBackendApi(`/community/posts/${id}/reports`, {
            method: 'POST',
            body: { reason },
        });
        if (!response.ok) {
            throw new Error('Failed to report post');
        }
    }

    static async getOpenReports(): Promise<CommunityReport[]> {
        const response = await callBackendApi('/community/admin/reports');
        if (!response.ok) {
            throw new Error('Failed to load reports');
        }
        const data = (await response.json()) as Record<string, unknown>[];
        return data.map((raw) => ({
            id: String(raw.id ?? raw.Id ?? ''),
            postId: String(raw.postId ?? raw.PostId ?? ''),
            postBodyPreview: String(
                raw.postBodyPreview ?? raw.PostBodyPreview ?? '',
            ),
            channelSlug: String(raw.channelSlug ?? raw.ChannelSlug ?? ''),
            reporterUserId: String(
                raw.reporterUserId ?? raw.ReporterUserId ?? '',
            ),
            reporterDisplayName: String(
                raw.reporterDisplayName ?? raw.ReporterDisplayName ?? '',
            ),
            reason: String(raw.reason ?? raw.Reason ?? ''),
            status: String(raw.status ?? raw.Status ?? ''),
            createdAt: String(raw.createdAt ?? raw.CreatedAt ?? ''),
        }));
    }

    static async hidePost(id: string): Promise<void> {
        const response = await callBackendApi(`/community/posts/${id}/hide`, {
            method: 'POST',
        });
        if (!response.ok) {
            throw new Error('Failed to hide post');
        }
    }

    static async resolveReport(id: string): Promise<void> {
        const response = await callBackendApi(
            `/community/admin/reports/${id}/resolve`,
            { method: 'POST' },
        );
        if (!response.ok) {
            throw new Error('Failed to resolve report');
        }
    }

    static async updateChannel(
        id: string,
        body: {
            isActive?: boolean;
            name?: string;
            description?: string | null;
            sortOrder?: number;
        },
    ): Promise<CommunityChannel> {
        const response = await callBackendApi(`/community/channels/${id}`, {
            method: 'PATCH',
            body,
        });
        if (!response.ok) {
            throw new Error('Failed to update channel');
        }
        return normalizeChannel(
            (await response.json()) as Record<string, unknown>,
        );
    }
}
