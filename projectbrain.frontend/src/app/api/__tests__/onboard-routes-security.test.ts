/** @jest-environment node */

import { getAccessToken } from '@/_lib/auth';
import { POST as userOnboardPost } from '@/app/api/user/onboard/route';
import { POST as coachOnboardPost } from '@/app/api/coach/onboard/route';

jest.mock('@/_lib/auth', () => ({
    getAccessToken: jest.fn(),
}));

const mockedGetAccessToken = getAccessToken as jest.MockedFunction<
    typeof getAccessToken
>;

describe('Onboard route security', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        mockedGetAccessToken.mockResolvedValue('super-secret-token');
    });

    it('does not leak bearer token in user onboarding error responses', async () => {
        global.fetch = jest.fn().mockResolvedValue(
            new Response(JSON.stringify({ error: 'Backend failed' }), {
                status: 502,
                headers: { 'content-type': 'application/json' },
            })
        ) as jest.Mock;

        const request = {
            json: async () => ({ fullName: 'Test User' }),
        };

        const response = await userOnboardPost(request as any);
        const payload = (await response.json()) as { error: string };

        expect(response.status).toBe(502);
        expect(payload.error).toBe('Backend failed');
        expect(JSON.stringify(payload)).not.toContain('super-secret-token');
    });

    it('does not leak bearer token in coach onboarding error responses', async () => {
        global.fetch = jest.fn().mockResolvedValue(
            new Response('Gateway timeout', {
                status: 504,
                headers: { 'content-type': 'text/plain' },
            })
        ) as jest.Mock;

        const request = {
            json: async () => ({ fullName: 'Coach User' }),
        };

        const response = await coachOnboardPost(request as any);
        const payload = (await response.json()) as { error: string };

        expect(response.status).toBe(504);
        expect(payload.error).toBe('Gateway timeout');
        expect(JSON.stringify(payload)).not.toContain('super-secret-token');
    });
});
