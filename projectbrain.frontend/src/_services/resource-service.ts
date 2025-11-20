import { Resource } from '@/_lib/types';

export class ResourceService {
    /**
     * Get all resources for current user
     */
    static async getResources(): Promise<Resource[]> {
        const response = await fetch('/api/resources', {
            method: 'GET',
            cache: 'no-store',
        });

        if (!response.ok) {
            throw new Error('Failed to fetch resources');
        }

        return response.json();
    }

    /**
     * Delete a resource by ID
     */
    static async deleteResource(resourceId: string): Promise<void> {
        const response = await fetch(`/api/resources/${resourceId}`, {
            method: 'DELETE',
        });

        if (!response.ok) {
            throw new Error('Failed to delete resource');
        }
    }
}
