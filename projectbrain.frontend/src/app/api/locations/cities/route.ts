import { createApiRoute } from '@/_lib/api-route-handler';
import { callBackendApi } from '@/_lib/backend-api';
import { NextRequest } from 'next/server';

export type CityOption = {
    city: string;
    stateProvince?: string;
    country?: string;
    latitude: number;
    longitude: number;
    placeId: string;
    formattedAddress: string;
};

const CACHE_TTL_MS = 1000 * 60 * 30; // 30m

let cache = new Map<string, { expiresAt: number; data: CityOption[] }>();

function normalizeKey(key: string) {
    return key.trim().toLowerCase();
}

export const GET = createApiRoute<CityOption[]>(async (req: NextRequest) => {
    const { searchParams } = new URL(req.url);
    const q = (searchParams.get('q') || '').trim();
    const countryCode = (searchParams.get('countryCode') || '').trim();

    // Only populate cities when country is selected (UI should enforce this too)
    if (!countryCode) return [];

    // Avoid noisy lookups
    if (q.length < 2) return [];

    const cacheKey = normalizeKey(`${countryCode}|${q}`);
    const now = Date.now();
    const cached = cache.get(cacheKey);
    if (cached && cached.expiresAt > now) {
        return cached.data;
    }

    const response = await callBackendApi(
        `/locations/cities?q=${encodeURIComponent(
            q
        )}&countryCode=${encodeURIComponent(countryCode)}`
    );

    if (!response.ok) {
        // callBackendApi already logs; keep UI resilient
        return [];
    }

    const options = (await response.json()) as CityOption[];

    // Basic cache pruning (keep map from growing unbounded)
    if (cache.size > 500) {
        cache = new Map(
            Array.from(cache.entries())
                .filter(([, v]) => v.expiresAt > now)
                .slice(-250)
        );
    }

    cache.set(cacheKey, { expiresAt: now + CACHE_TTL_MS, data: options });
    return options;
});

