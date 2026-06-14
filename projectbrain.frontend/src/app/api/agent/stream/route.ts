import { createStreamingApiRoute } from '@/_lib/api-route-handler';
import { getAccessToken } from '@/_lib/auth';
import { NextRequest } from 'next/server';

export const POST = createStreamingApiRoute(async (req: NextRequest) => {
    const { content, conversationId, workflowId } = await req.json();
    const accessToken = await getAccessToken();

    const headers: HeadersInit = {
        Authorization: `Bearer ${accessToken}`,
        'Content-Type': 'application/json',
    };

    const body: Record<string, unknown> = { content };
    if (conversationId) {
        body.conversationId = conversationId;
    }
    if (workflowId) {
        body.workflowId = workflowId;
    }

    const backendResponse = await fetch(
        `${process.env.API_SERVER_URL}/agent/stream`,
        {
            method: 'POST',
            headers,
            body: JSON.stringify(body),
        }
    );

    const stream = backendResponse.body;
    const conversationIdHeader =
        backendResponse.headers.get('X-Conversation-Id');
    const workflowIdHeader = backendResponse.headers.get('X-Workflow-Id');
    const agentStatusHeader = backendResponse.headers.get('X-Agent-Status');

    return new Response(stream, {
        status: backendResponse.status,
        headers: {
            'Content-Type': 'text/event-stream',
            ...(conversationIdHeader
                ? { 'X-Conversation-Id': conversationIdHeader }
                : {}),
            ...(workflowIdHeader ? { 'X-Workflow-Id': workflowIdHeader } : {}),
            ...(agentStatusHeader
                ? { 'X-Agent-Status': agentStatusHeader }
                : {}),
        },
    });
});
