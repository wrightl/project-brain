import { callBackendApi } from '@/_lib/backend-api';

export interface Tag {
    id: string;
    name: string;
    createdAt: string;
}

export interface CreateTagRequest {
    name: string;
}

export class TagService {
    /**
     * Get all tags for the current user
     */
    static async getAllTags(): Promise<Tag[]> {
        const response = await callBackendApi('/tag', {
            method: 'GET',
        });

        if (!response.ok) {
            throw new Error('Failed to fetch tags');
        }

        return response.json();
    }

    /**
     * Get a tag by ID
     */
    static async getTag(id: string): Promise<Tag> {
        const response = await callBackendApi(`/tag/${id}`, {
            method: 'GET',
        });

        if (!response.ok) {
            throw new Error('Failed to fetch tag');
        }

        return response.json();
    }

    /**
     * Get a tag by name
     */
    static async getTagByName(name: string): Promise<Tag | null> {
        const response = await callBackendApi(
            `/tag/name/${encodeURIComponent(name)}`,
            {
                method: 'GET',
            }
        );

        if (!response.ok) {
            if (response.status === 404) {
                return null;
            }
            throw new Error('Failed to fetch tag');
        }

        return response.json();
    }

    /**
     * Create a new tag
     */
    static async createTag(request: CreateTagRequest): Promise<Tag> {
        const response = await callBackendApi('/tag', {
            method: 'POST',
            body: request,
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to create tag');
        }

        return response.json();
    }

    /**
     * Update a tag
     */
    static async updateTag(
        id: string,
        request: CreateTagRequest
    ): Promise<Tag> {
        const response = await callBackendApi(`/tag/${id}`, {
            method: 'PUT',
            body: request,
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to update tag');
        }

        return response.json();
    }

    /**
     * Delete a tag
     */
    static async deleteTag(tagId: string): Promise<void> {
        const response = await callBackendApi(`/tag/${tagId}`, {
            method: 'DELETE',
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to delete tag');
        }
    }
}
