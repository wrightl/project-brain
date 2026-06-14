import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export type CountryOption = {
    name: string;
    code: string; // ISO 3166-1 alpha-2 (cca2)
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

    const response = await callBackendApi('/locations/countries');

    if (!response.ok) {
        return [];
    }

    const countries = (await response.json()) as CountryOption[];

    cache = { expiresAt: now + CACHE_TTL_MS, data: countries };
    return countries;
});
