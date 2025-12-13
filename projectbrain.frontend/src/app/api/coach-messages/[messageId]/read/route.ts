import { CoachMessageService } from '@/_services/coach-message-service';
import { NextRequest, NextResponse } from 'next/server';

export async function PUT(
    req: NextRequest,
    { params }: { params: Promise<{ messageId: string }> }
) {
    try {
        const { messageId } = await params;
        await CoachMessageService.markAsRead(messageId);
        return NextResponse.json({ success: true });
    } catch (error) {
        console.error('Error marking message as read:', error);
        return NextResponse.json(
            { error: 'Failed to mark message as read' },
            { status: 500 }
        );
    }
}

