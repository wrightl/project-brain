// import { init, BasicLogger } from '@launchdarkly/node-server-sdk';
// import { getSession, getUserRoles } from './auth';
import { callBackendApi } from './backend-api';

export type FeatureFlags = {
    CoachFeatureEnabled: boolean;
    EmailFeatureEnabled: boolean;
    // [key: string]: boolean | undefined;
};

type CacheEntry = {
    data: FeatureFlags;
    timestamp: number;
};

const CACHE_TTL = 5 * 60 * 1000; // 5 minutes in milliseconds
let cache: CacheEntry | null = null;

export async function getFlags(): Promise<FeatureFlags> {
    const now = Date.now();

    // Check if cached data exists and is still valid
    if (cache && now - cache.timestamp < CACHE_TTL) {
        return cache.data;
    }

    try {
        const response = await callBackendApi('/feature-flags', {
            method: 'GET',
        });

        if (!response.ok) {
            throw new Error(
                `Failed to fetch feature flags: ${response.status} ${response.statusText}`
            );
        }

        const flags = (await response.json()) as FeatureFlags;

        // Update cache with new data and timestamp
        cache = {
            data: flags,
            timestamp: now,
        };

        return flags;
    } catch (error) {
        console.error('Error fetching feature flags:', error);

        // On API errors, return cached data if available (graceful degradation)
        if (cache) {
            return cache.data;
        }

        // Otherwise return default values
        return {
            CoachFeatureEnabled: false,
            EmailFeatureEnabled: false,
        };
    }
    // const SDK_KEY = process.env.LAUNCHDARKLY_SDK_KEY;
    // const session = await getSession();
    // const roles = await getUserRoles();
    // const user = session?.user;

    // const options = {
    //     logger: new BasicLogger({
    //         destination: console.log,
    //         level: 'debug',
    //     }),
    // };
    // if (!SDK_KEY) {
    //     throw new Error('LAUNCHDARKLY_SDK_KEY is not defined');
    // }
    // const client = init(SDK_KEY, options);
    // await client.waitForInitialization({
    //     timeout: 5,
    // });

    // const context = user
    //     ? {
    //           kind: 'user',
    //           key: user.sub,
    //           name: user.name,
    //           email: user.email,
    //           roles: roles || [],
    //       }
    //     : {
    //           kind: 'user',
    //           key: 'anonymous',
    //           anonymous: true,
    //       };

    // const flags = await client.allFlagsState(context);

    // return flags.allValues() as FeatureFlags;
}
