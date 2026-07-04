import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { NextResponse } from 'next/server';

export const GET = createApiRoute(
    async (_req, context?: { params: Promise<{ messageId: string }> }) => {
        const { messageId } = await context!.params;
        const response = await callBackendApi(`/coach-messages/${messageId}/audio`);

        if (!response.ok) {
            return NextResponse.json(
                { error: 'Failed to fetch audio' },
                { status: response.status },
            );
        }

        const audioBlob = await response.blob();
        const contentType = response.headers.get('Content-Type') || 'audio/m4a';

        return new NextResponse(audioBlob, {
            headers: {
                'Content-Type': contentType,
            },
        });
    },
);
