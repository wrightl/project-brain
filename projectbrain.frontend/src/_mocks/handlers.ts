import { http, HttpResponse } from 'msw';

const API_URL = process.env.API_SERVER_URL || 'https://localhost:7585';

export const handlers = [
    // Subscription endpoints
    http.get(`${API_URL}/subscriptions/me`, () => {
        return HttpResponse.json({
            id: 'sub_123',
            tier: 'Pro',
            status: 'active',
            userType: 'user',
        });
    }),

    http.get(`${API_URL}/subscriptions/usage`, () => {
        return HttpResponse.json({
            aiQueries: {
                daily: 10,
                monthly: 150,
            },
            coachMessages: {
                monthly: 25,
            },
            fileStorage: {
                bytes: 1024 * 1024 * 50, // 50 MB
                megabytes: 50,
            },
            researchReports: {
                monthly: 0,
            },
        });
    }),

    // Resource endpoints
    http.get(`${API_URL}/resource/user`, () => {
        return HttpResponse.json([
            {
                id: 'res_1',
                fileName: 'test.pdf',
                location: 's3://bucket/test.pdf',
                sizeInBytes: 1024 * 100,
                createdAt: '2024-01-01T00:00:00Z',
                updatedAt: '2024-01-01T00:00:00Z',
            },
        ]);
    }),

    // Voice note endpoints
    http.get(`${API_URL}/voicenotes`, () => {
        return HttpResponse.json([
            {
                id: 'vn_1',
                fileName: 'note.m4a',
                audioUrl: 'https://example.com/note.m4a',
                duration: 120,
                fileSize: 1024 * 500,
                description: 'Test note',
                createdAt: '2024-01-01T00:00:00Z',
                updatedAt: '2024-01-01T00:00:00Z',
            },
        ]);
    }),

    // Connection endpoints
    http.get(`${API_URL}/connections`, () => {
        return HttpResponse.json([
            {
                id: 'conn_1',
                userId: 'user_1',
                coachId: 'coach_1',
                status: 'accepted',
                userName: 'Test User',
                coachName: 'Test Coach',
                requestedAt: '2024-01-01T00:00:00Z',
                respondedAt: '2024-01-01T01:00:00Z',
            },
        ]);
    }),
];

