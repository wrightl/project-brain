import { CoachMessageService } from '@/_services/coach-message-service';
import { NextRequest, NextResponse } from 'next/server';

export async function POST(req: NextRequest) {
    try {
        const formData = await req.formData();
        const file = formData.get('file') as File;
        const connectionId = formData.get('connectionId') as string;

        if (!file || !connectionId) {
            return NextResponse.json(
                { error: 'Missing required fields' },
                { status: 400 }
            );
        }

        const message = await CoachMessageService.sendVoiceMessage(
            connectionId,
            file
        );
        return NextResponse.json(message, { status: 201 });
    } catch (error) {
        console.error('Error sending voice message:', error);
        return NextResponse.json(
            { error: 'Failed to send voice message' },
            { status: 500 }
        );
    }
}
