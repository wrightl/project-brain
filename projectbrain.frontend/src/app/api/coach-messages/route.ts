import { createApiRoute } from '@/_lib/api-route-handler';
import { CoachMessageService } from '@/_services/coach-message-service';
import { NextRequest } from 'next/server';

export const POST = createApiRoute(async (req: NextRequest) => {
    const body = await req.json();
    const message = await CoachMessageService.sendMessage(body);
    return message;
});
