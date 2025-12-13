import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export interface ConnectionDetails {
    id: string;
    userProfileId: string;
    coachProfileId: string;
    status: string;
    requestedAt: string;
    respondedAt?: string;
    requestedBy: string;
    message?: string;
}

export const GET = createApiRoute<ConnectionDetails>(
    async (_, { params }: { params: { connectionId: string } }) => {
        const { connectionId } = await params;
        const response = await callBackendApi(`/connections/${connectionId}`);

        if (!response.ok) {
            throw new Error('Failed to fetch connection details');
        }

        return response.json();
    }
);

export const DELETE = createApiRoute(
    async (_, { params }: { params: { connectionId: string } }) => {
        const { connectionId } = await params;
        const response = await callBackendApi(`/connections/${connectionId}`, {
            method: 'DELETE',
        });

        if (!response.ok) {
            throw new Error('Failed to delete connection');
        }

        return { success: true };
    }
);
