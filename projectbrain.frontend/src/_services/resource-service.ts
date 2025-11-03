import { callBackendApi } from '@/_lib/backend-api';
import { Resource } from '@/_lib/types';

export class ResourceService {
    /**
     * Get current user
     */
    static async getResources(): Promise<Resource[]> {
        const response = callBackendApi('/resource');
        return (await response).json();
    }
}
