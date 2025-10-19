import 'server-only';

import { Egg } from '@/models/egg';
import { getAccessToken } from '@auth0/nextjs-auth0';

export const getAllEggs = async (): Promise<Egg[]> => {
    const { accessToken } = await getAccessToken();

    if (!accessToken) {
        throw new Error(`Requires authorization`);
    }

    console.log(`token: ${accessToken}`);
    console.log(`url: ${process.env.API_SERVER_URL}`);
    console.log(`aud: ${process.env.AUTH0_AUDIENCE}`);

    const res = await fetch(`${process.env.API_SERVER_URL}/eggs`, {
        headers: {
            Authorization: `Bearer ${accessToken}`,
        },
    });

    if (!res.ok) {
        console.log(res);
        const json = await res.json();

        return json.message || res.statusText || 'Unable to fetch message.';
    }

    return res.json();
};
