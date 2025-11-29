import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest, NextResponse } from 'next/server';

export async function GET() {
    try {
        const response = await callBackendApi('/quizes');
        if (!response.ok) {
            return NextResponse.json(
                { error: 'Failed to fetch quizzes' },
                { status: response.status }
            );
        }
        const quizzes = await response.json();
        return NextResponse.json(quizzes);
    } catch (error) {
        console.error('Error fetching quizzes:', error);
        return NextResponse.json(
            { error: 'Internal server error' },
            { status: 500 }
        );
    }
}

export async function POST(request: NextRequest) {
    try {
        const body = await request.json();
        const response = await callBackendApi('/quizes', {
            method: 'POST',
            body,
        });
        if (!response.ok) {
            const errorData = await response.json();
            return NextResponse.json(errorData, { status: response.status });
        }
        const quiz = await response.json();
        return NextResponse.json(quiz, { status: 201 });
    } catch (error) {
        console.error('Error creating quiz:', error);
        return NextResponse.json(
            { error: 'Internal server error' },
            { status: 500 }
        );
    }
}

