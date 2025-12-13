import { createApiRoute } from '@/_lib/api-route-handler';
import { CoachMessageService, ConversationSummary } from '@/_services/coach-message-service';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<ConversationSummary[]>(
    async (req: NextRequest) => {
        const conversations = await CoachMessageService.getConversations();
        return conversations;
    }
);

