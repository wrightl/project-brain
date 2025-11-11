import { init, BasicLogger } from '@launchdarkly/node-server-sdk';
import { getSession, getUserRoles } from './auth';

export type FeatureFlags = {
    'enable-coach-section': boolean;
    'beta-features': boolean;
};

export async function getFlags() {
    const SDK_KEY = process.env.LAUNCHDARKLY_SDK_KEY;
    const session = await getSession();
    const roles = await getUserRoles();
    const user = session?.user;

    const options = {
        logger: new BasicLogger({
            destination: console.log,
            level: 'debug',
        }),
    };
    if (!SDK_KEY) {
        throw new Error('LAUNCHDARKLY_SDK_KEY is not defined');
    }
    const client = init(SDK_KEY, options);
    await client.waitForInitialization({
        timeout: 5,
    });

    const context = user
        ? {
              kind: 'user',
              key: user.sub,
              name: user.name,
              email: user.email,
              roles: roles || [],
          }
        : {
              kind: 'user',
              key: 'anonymous',
              anonymous: true,
          };

    const flags = await client.allFlagsState(context);

    return flags.allValues() as FeatureFlags;
}
