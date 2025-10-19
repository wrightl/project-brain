import 'server-only';

import { WeatherForecast } from '@/models/weather-forecast';
import { getAccessToken } from '@auth0/nextjs-auth0';

export const getWeatherForecast = async (): Promise<WeatherForecast[]> => {
    const { accessToken } = await getAccessToken();

    if (!accessToken) {
        throw new Error(`Requires authorization`);
    }

    console.log(`token: ${accessToken}`);
    console.log(`url: ${process.env.API_SERVER_URL}`);
    console.log(`aud: ${process.env.AUTH0_AUDIENCE}`);

    const res = await fetch(
        `${process.env.API_SERVER_URL}/weatherforecast?count=20`,
        {
            headers: {
                Authorization: `Bearer ${accessToken}`,
            },
        }
    );

    if (!res.ok) {
        console.log(res);
        const json = await res.json();

        return json.message || res.statusText || 'Unable to fetch message.';
    }

    return res.json();
};
