import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export interface ConnectionResponse {
    id: string;
    status: 'pending' | 'connected';
    requestedAt: string;
    coachId: string;
    userId: string;
}

export const POST = createApiRoute<ConnectionResponse>(
    async (req: NextRequest, { params }: { params: Promise<{ id: string }> }) => {
        const { id: coachId } = await params;
        const body = await req.json().catch(() => ({}));
        
        const response = await callBackendApi(`/coaches/${coachId}/connections`, {
            method: 'POST',
            body: body,
        });
        
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(
                errorData.error?.message || 'Failed to send connection request'
            );
        }
        
        return response.json();
    }
);
