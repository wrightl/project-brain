import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export interface ConnectionStatusResponse {
    status: 'none' | 'pending' | 'connected';
    connectionId?: string;
    requestedAt?: string;
    respondedAt?: string;
    requestedBy?: 'user' | 'coach';
}

export const GET = createApiRoute<ConnectionStatusResponse>(
    async (req: NextRequest, { params }: { params: { id: string } }) => {
        const { id: coachId } = await params;
        const response = await callBackendApi(`/coaches/${coachId}/connection-status`);
        if (!response.ok) {
            throw new Error('Failed to fetch connection status');
        }
        return response.json();
    }
);

