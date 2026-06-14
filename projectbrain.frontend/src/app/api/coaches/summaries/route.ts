import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export type CoachSummaryResponse = {
    coachProfileId: string;
    fullName: string;
    bio?: string | null;
    imageUrl?: string | null;
};

export const POST = createApiRoute(async (req: NextRequest) => {
    const body = (await req.json()) as { coachProfileIds: string[] };
    const response = await callBackendApi('/coaches/summaries', {
        method: 'POST',
        body,
    });

    if (!response.ok) {
        return Response.json(
            { error: 'Failed to fetch coach summaries' },
            { status: response.status }
        );
    }

    return (await response.json()) as Record<string, CoachSummaryResponse>;
});
