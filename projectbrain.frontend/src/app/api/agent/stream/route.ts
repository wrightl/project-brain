import { getAccessToken } from '@/_lib/auth';
import { NextRequest } from 'next/server';

export async function POST(req: NextRequest) {
    const { content, conversationId, workflowId } = await req.json();

    const accessToken = await getAccessToken();

    // Prepare headers
    const headers: HeadersInit = {
        Authorization: `Bearer ${accessToken}`,
        'Content-Type': 'application/json',
    };

    // Prepare request body
    const body: Record<string, unknown> = { content };
    if (conversationId) {
        body.conversationId = conversationId;
    }
    if (workflowId) {
        body.workflowId = workflowId;
    }

    // Use native fetch for streaming
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
}

