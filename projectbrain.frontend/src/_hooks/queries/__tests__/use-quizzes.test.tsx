import React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook, waitFor } from '@testing-library/react';
import { useSubmitQuizResponse } from '../use-quizzes';
import { fetchWithAuth } from '@/_lib/fetch-with-auth';

jest.mock('@/_lib/fetch-with-auth', () => ({
    fetchWithAuth: jest.fn(),
}));

const mockedFetchWithAuth = fetchWithAuth as jest.MockedFunction<
    typeof fetchWithAuth
>;

function wrapper({ children }: { children: React.ReactNode }) {
    const client = new QueryClient({
        defaultOptions: {
            queries: { retry: false },
            mutations: { retry: false },
        },
    });
    return (
        <QueryClientProvider client={client}>{children}</QueryClientProvider>
    );
}

describe('useSubmitQuizResponse', () => {
    beforeEach(() => {
        mockedFetchWithAuth.mockReset();
    });

    it('posts to the quiz id path instead of a literal template string', async () => {
        const quizId = '6f1c3d2a-4b8e-4a11-9c22-0d4e5f6a7b8c';
        mockedFetchWithAuth.mockResolvedValue(
            new Response(
                JSON.stringify({
                    id: 'response-1',
                    quizId,
                    userId: 'user-1',
                    answers: { q1: 'a' },
                    completedAt: '2026-09-11T00:00:00.000Z',
                    createdAt: '2026-09-11T00:00:00.000Z',
                }),
                { status: 200, headers: { 'Content-Type': 'application/json' } },
            ),
        );

        const { result } = renderHook(() => useSubmitQuizResponse(), {
            wrapper,
        });

        await act(async () => {
            await result.current.mutateAsync({
                quizId,
                answers: { q1: 'a' },
            });
        });

        await waitFor(() => {
            expect(mockedFetchWithAuth).toHaveBeenCalledTimes(1);
        });

        const [url, init] = mockedFetchWithAuth.mock.calls[0];
        expect(url).toBe(`/api/user/quizzes/${quizId}/responses`);
        expect(url).not.toContain('${quizId}');
        expect(init).toEqual(
            expect.objectContaining({
                method: 'POST',
            }),
        );
    });
});
