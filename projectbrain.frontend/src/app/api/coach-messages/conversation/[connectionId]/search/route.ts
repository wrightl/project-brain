import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest, NextResponse } from 'next/server';

export async function GET(
    req: NextRequest,
    { params }: { params: Promise<{ connectionId: string }> }
) {
    try {
        const { connectionId } = await params;
        const searchParams = req.nextUrl.searchParams;
        const searchTerm = searchParams.get('searchTerm');

        if (!searchTerm) {
            return NextResponse.json(
                { error: 'Search term is required' },
                { status: 400 }
            );
        }

        const response = await callBackendApi(
            `/coach-messages/conversation/${connectionId}/search?searchTerm=${encodeURIComponent(
                searchTerm
            )}`
        );

        if (!response.ok) {
            throw new Error('Failed to search messages');
        }

        return NextResponse.json(await response.json());
    } catch (error) {
        console.error('Error searching messages:', error);
        return NextResponse.json(
            { error: 'Failed to search messages' },
            { status: 500 }
        );
    }
}
