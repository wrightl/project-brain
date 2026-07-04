import { createApiRoute } from '@/_lib/api-route-handler';
import { CoachMessageService } from '@/_services/coach-message-service';

export const DELETE = createApiRoute(
    async (_req, context?: { params: Promise<{ messageId: string }> }) => {
        const { messageId } = await context!.params;
        await CoachMessageService.deleteMessage(messageId);
        return { success: true };
    },
);
