import { callBackendApi } from '@/_lib/backend-api';
import { getAccessToken } from '@/_lib/auth';

jest.mock('@/_lib/auth', () => ({
    getAccessToken: jest.fn(),
}));

const mockedGetAccessToken = getAccessToken as jest.MockedFunction<
    typeof getAccessToken
>;

describe('callBackendApi security logging', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        mockedGetAccessToken.mockResolvedValue('top-secret-access-token');
    });

    it('does not log access token when backend returns non-2xx', async () => {
        global.fetch = jest.fn().mockResolvedValue(
            new Response('Bad gateway', {
                status: 502,
                statusText: 'Bad Gateway',
            })
        ) as jest.Mock;

        const consoleSpy = jest
            .spyOn(console, 'error')
            .mockImplementation(() => {});

        await callBackendApi('/test-endpoint');

        const allLogs = consoleSpy.mock.calls
            .flatMap((args) => args.map((arg) => String(arg)))
            .join(' ');

        expect(allLogs).not.toContain('top-secret-access-token');
        consoleSpy.mockRestore();
    });
});
