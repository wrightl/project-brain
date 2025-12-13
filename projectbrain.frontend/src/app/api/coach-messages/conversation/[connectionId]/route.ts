import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest, NextResponse } from 'next/server';

export async function GET(
    req: NextRequest,
    { params }: { params: Promise<{ connectionId: string }> }
) {
    try {
        const { connectionId } = await params;
        const searchParams = req.nextUrl.searchParams;
        const pageSize = searchParams.get('pageSize') || '20';
        const beforeDate = searchParams.get('beforeDate');

        const queryParams = new URLSearchParams();
        queryParams.append('pageSize', pageSize);
        if (beforeDate) {
            queryParams.append('beforeDate', beforeDate);
        }

        const response = await callBackendApi(
            `/coach-messages/conversation/${connectionId}?${queryParams.toString()}`
        );

        if (!response.ok) {
            throw new Error('Failed to fetch conversation messages');
        }

        return NextResponse.json(await response.json());
    } catch (error) {
        console.error('Error fetching conversation messages:', error);
        return NextResponse.json(
            { error: 'Failed to fetch conversation messages' },
            { status: 500 }
        );
    }
}

export async function PUT(
    req: NextRequest,
    { params }: { params: Promise<{ connectionId: string }> }
) {
    try {
        const { connectionId } = await params;

        // Mark conversation as read using userId and coachId
        const response = await callBackendApi(
            `/coach-messages/conversation/${connectionId}/read`,
            { method: 'PUT' }
        );

        if (!response.ok) {
            throw new Error('Failed to mark conversation as read');
        }

        return NextResponse.json({ success: true });
    } catch (error) {
        console.error('Error marking conversation as read:', error);
        return NextResponse.json(
            { error: 'Failed to mark conversation as read' },
            { status: 500 }
        );
    }
}
