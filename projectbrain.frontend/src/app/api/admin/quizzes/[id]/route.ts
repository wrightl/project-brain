import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest, NextResponse } from 'next/server';

export async function GET(
    request: NextRequest,
    { params }: { params: Promise<{ id: string }> }
) {
    try {
        const { id } = await params;
        const response = await callBackendApi(`/quizes/${id}`);
        if (!response.ok) {
            return NextResponse.json(
                {
                    error: 'Failed to fetch quiz',
                },
                { status: response.status }
            );
        }
        const quiz = await response.json();
        return NextResponse.json(quiz);
    } catch (error) {
        console.error('Error fetching quiz:', error);
        return NextResponse.json(
            { error: 'Internal server error' },
            { status: 500 }
        );
    }
}

export async function PUT(
    request: NextRequest,
    { params }: { params: Promise<{ id: string }> }
) {
    try {
        const { id } = await params;
        const body = await request.json();
        const response = await callBackendApi(`/quizes/${id}`, {
            method: 'PUT',
            body,
        });
        if (!response.ok) {
            const errorData = await response.json();
            return NextResponse.json(errorData, { status: response.status });
        }
        const quiz = await response.json();
        return NextResponse.json(quiz, { status: 200 });
    } catch (error) {
        console.error('Error updating quiz:', error);
        return NextResponse.json(
            { error: 'Internal server error' },
            { status: 500 }
        );
    }
}

export async function DELETE(
    request: NextRequest,
    { params }: { params: Promise<{ id: string }> }
) {
    try {
        const { id } = await params;
        const response = await callBackendApi(`/quizes/${id}`, {
            method: 'DELETE',
        });
        if (!response.ok) {
            const errorData = await response.json();
            return NextResponse.json(errorData, { status: response.status });
        }
        return NextResponse.json({ success: true }, { status: 200 });
    } catch (error) {
        console.error('Error deleting quiz:', error);
        return NextResponse.json(
            { error: 'Internal server error' },
            { status: 500 }
        );
    }
}
