import { callBackendApi } from '@/_lib/backend-api';
import { ReindexResult, Resource } from '@/_lib/types';

export class ResourceService {
    static async getResource(id: string): Promise<Resource> {
        const response = await callBackendApi(`/resource/${id}/user`);
        return response.json();
    }

    static async getUserResourceFile(
        id: string
    ): Promise<{ blob: Blob; headers: Headers }> {
        return this._getFile(id, 'user');
    }

    static async _getFile(
        id: string,
        type: 'user' | 'shared'
    ): Promise<{ blob: Blob; headers: Headers }> {
        var guid = id.split('_')[0];
        const response = await callBackendApi(`/resource/${guid}/${type}/file`);

        const blob = await response.blob();

        const contentDisposition = response.headers.get('Content-Disposition');
        let filename = 'download';

        if (contentDisposition) {
            // Parse filename from Content-Disposition header
            // Format: attachment; filename="filename.ext" or attachment; filename*=UTF-8''filename.ext
            // Try filename* first (RFC 5987), then fall back to filename
            const filenameStarMatch =
                contentDisposition.match(/filename\*=([^;]+)/i);
            if (filenameStarMatch) {
                const value = filenameStarMatch[1].trim();
                // Handle UTF-8 encoded filenames (filename*=UTF-8''filename.ext)
                if (
                    value.startsWith("UTF-8''") ||
                    value.startsWith("utf-8''")
                ) {
                    filename = decodeURIComponent(value.substring(7));
                } else {
                    filename = decodeURIComponent(value);
                }
            } else {
                // Fall back to regular filename parameter
                const filenameMatch =
                    contentDisposition.match(/filename=([^;]+)/i);
                if (filenameMatch) {
                    filename = filenameMatch[1]
                        .trim()
                        .replace(/^["']|["']$/g, '');
                }
            }
        }

        // Get the Content-Disposition header from the backend response
        // This contains the filename for the download
        const contentType =
            response.headers.get('Content-Type') || 'application/octet-stream';

        // Prepare headers for the Next.js response
        const headers = new Headers();
        headers.set('Content-Type', contentType);

        if (contentDisposition) {
            headers.set('Content-Disposition', contentDisposition);
        } else {
            // Fallback: set a default Content-Disposition if not provided
            headers.set(
                'Content-Disposition',
                `attachment; filename="resource-${id}"`
            );
        }

        return { blob, headers };
    }

    static async getSharedResource(id: string): Promise<Resource> {
        const response = await callBackendApi(`/resource/${id}/shared`);
        return response.json();
    }

    static async getSharedResourceFile(
        id: string
    ): Promise<{ blob: Blob; headers: Headers }> {
        return this._getFile(id, 'shared');
    }

    /**
     * Get all resources for current user
     */
    static async getResources(): Promise<Resource[]> {
        const response = await callBackendApi('/resource/user');
        return response.json();
    }

    /**
     * Get shared resources for current user
     */
    static async getSharedResources(): Promise<Resource[]> {
        const response = await callBackendApi('/resource/shared');
        return response.json();
    }

    /**
     * Delete a resource by ID
     */
    static async deleteResource(resourceId: string): Promise<Response> {
        const response = await callBackendApi(`/resource/${resourceId}/user`, {
            method: 'DELETE',
        });
        return response;
    }

    /**
     * Delete a shared resource by ID
     */
    static async deleteSharedResource(resourceId: string): Promise<Response> {
        const response = await callBackendApi(
            `/resource/${resourceId}/shared`,
            {
                method: 'DELETE',
            }
        );
        return response;
    }

    /**
     * Reindex resources for current user
     */
    static async reindexResources(): Promise<ReindexResult> {
        const response = await callBackendApi('/resource/reindex/user', {
            method: 'POST',
        });
        return response.json();
    }

    /**
     * Reindex shared resources for current user
     */
    static async reindexSharedResources(): Promise<ReindexResult> {
        const response = await callBackendApi('/resource/reindex/shared', {
            method: 'POST',
        });
        return response.json();
    }
}
