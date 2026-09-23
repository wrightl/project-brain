import { getFlags, clearFlagsCache } from '@/_lib/flags';

describe('feature flags defaults', () => {
    beforeEach(() => {
        clearFlagsCache();
        global.fetch = jest.fn();
    });

    it('includes CommunityFeatureEnabled defaulting to false on fetch failure', async () => {
        (global.fetch as jest.Mock).mockRejectedValue(new Error('network'));

        const flags = await getFlags();

        expect(flags.CommunityFeatureEnabled).toBe(false);
        expect(flags).toEqual(
            expect.objectContaining({
                CoachFeatureEnabled: expect.any(Boolean),
                EmailFeatureEnabled: expect.any(Boolean),
                AgentFeatureEnabled: expect.any(Boolean),
                CommunityFeatureEnabled: false,
            }),
        );
    });
});
