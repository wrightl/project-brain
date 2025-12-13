import { CoachMessageService } from '@/_services/coach-message-service';
import { NextRequest, NextResponse } from 'next/server';

export async function DELETE(
    req: NextRequest,
    { params }: { params: { messageId: string } }
) {
    try {
        const { messageId } = params;
        await CoachMessageService.deleteMessage(messageId);
        return NextResponse.json({ success: true });
    } catch (error) {
        console.error('Error deleting message:', error);
        return NextResponse.json(
            { error: 'Failed to delete message' },
            { status: 500 }
        );
    }
}

