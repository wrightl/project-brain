import { createApiRoute } from '@/_lib/api-route-handler';
import { CoachService, CoachSearchParams } from '@/_services/coach-service';
import { Coach } from '@/_lib/types';
import { NextRequest } from 'next/server';

function parseBoolish(value: string | null): boolean | undefined {
    if (value == null) return undefined;
    const v = value.trim().toLowerCase();
    if (v === 'true' || v === '1' || v === 'yes' || v === 'on') return true;
    if (v === 'false' || v === '0' || v === 'no' || v === 'off') return false;
    return undefined;
}

function parseNumber(value: string | null): number | undefined {
    if (value == null) return undefined;
    const n = Number(value);
    return Number.isFinite(n) ? n : undefined;
}

export const GET = createApiRoute<Coach[]>(async (req: NextRequest) => {
    const { searchParams } = new URL(req.url);

    const ageGroups = searchParams.getAll('ageGroups');
    const specialisms = searchParams.getAll('specialisms');

    const useMyLocation = parseBoolish(searchParams.get('useMyLocation'));
    const distanceMiles = parseNumber(searchParams.get('distanceMiles'));
    const latitude = parseNumber(searchParams.get('latitude'));
    const longitude = parseNumber(searchParams.get('longitude'));

    const params: CoachSearchParams = {
        city: searchParams.get('city') || undefined,
        stateProvince: searchParams.get('stateProvince') || undefined,
        country: searchParams.get('country') || undefined,
        ageGroups: ageGroups.length > 0 ? ageGroups : undefined,
        specialisms: specialisms.length > 0 ? specialisms : undefined,
        useMyLocation,
        distanceMiles,
        latitude,
        longitude,
    };

    const coaches = await CoachService.searchCoaches(params);
    return coaches;
});

