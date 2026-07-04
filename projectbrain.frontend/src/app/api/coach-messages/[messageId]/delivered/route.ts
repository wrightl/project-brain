import { createApiRoute } from '@/_lib/api-route-handler';
import { CoachMessageService } from '@/_services/coach-message-service';

export const PUT = createApiRoute(
    async (_req, context?: { params: Promise<{ messageId: string }> }) => {
        const { messageId } = await context!.params;
        await CoachMessageService.markAsDelivered(messageId);
        return { success: true };
    },
);
