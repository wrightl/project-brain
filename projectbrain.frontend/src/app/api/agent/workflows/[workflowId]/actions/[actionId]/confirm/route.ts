import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';

export const POST = createApiRoute(async (_req, context) => {
    const { workflowId, actionId } = await context.params;
    const response = await callBackendApi(
        `/agent/workflows/${workflowId}/actions/${actionId}/confirm`,
        { method: 'POST' }
    );
    return response.json();
});
