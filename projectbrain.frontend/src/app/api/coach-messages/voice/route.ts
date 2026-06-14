import { createApiRoute } from '@/_lib/api-route-handler';
import { CoachMessageService } from '@/_services/coach-message-service';
import { NextRequest } from 'next/server';

export const POST = createApiRoute(async (req: NextRequest) => {
    const formData = await req.formData();
    const file = formData.get('file') as File;
    const connectionId = formData.get('connectionId') as string;

    if (!file || !connectionId) {
        return Response.json(
            { error: 'Missing required fields' },
            { status: 400 }
        );
    }

    const message = await CoachMessageService.sendVoiceMessage(
        connectionId,
        file
    );
    return Response.json(message, { status: 201 });
});
