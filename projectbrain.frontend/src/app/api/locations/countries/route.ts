import { createApiRoute } from '@/_lib/api-route-handler';
import { NextRequest } from 'next/server';

export type CountryOption = {
    name: string;
    code: string; // ISO 3166-1 alpha-2 (cca2)
};

type RestCountriesResponseItem = {
    name?: { common?: string };
    cca2?: string;
};

const CACHE_TTL_MS = 1000 * 60 * 60 * 24; // 24h
let cache:
    | { expiresAt: number; data: CountryOption[] }
    | null = null;

export const GET = createApiRoute<CountryOption[]>(async (_req: NextRequest) => {
    const now = Date.now();
    if (cache && cache.expiresAt > now) {
        return cache.data;
    }

    const res = await fetch(
        'https://restcountries.com/v3.1/all?fields=name,cca2',
        { cache: 'no-store' }
    );

    if (!res.ok) {
        throw new Error(`Failed to fetch countries: ${res.status}`);
    }

    const json = (await res.json()) as RestCountriesResponseItem[];

    const countries = json
        .map((c) => {
            const name = c.name?.common?.trim();
            const code = c.cca2?.trim();
            if (!name || !code) return null;
            return { name, code } satisfies CountryOption;
        })
        .filter((x): x is CountryOption => Boolean(x))
        .sort((a, b) => a.name.localeCompare(b.name));

    cache = { expiresAt: now + CACHE_TTL_MS, data: countries };
    return countries;
});

