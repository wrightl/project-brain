import { createApiRoute } from '@/_lib/api-route-handler';
import { CoachService, CoachSearchParams } from '@/_services/coach-service';
import { Coach } from '@/_lib/types';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<Coach[]>(async (req: NextRequest) => {
    const { searchParams } = new URL(req.url);

    const ageGroups = searchParams.getAll('ageGroups');
    const specialisms = searchParams.getAll('specialisms');

    const params: CoachSearchParams = {
        city: searchParams.get('city') || undefined,
        stateProvince: searchParams.get('stateProvince') || undefined,
        country: searchParams.get('country') || undefined,
        ageGroups: ageGroups.length > 0 ? ageGroups : undefined,
        specialisms: specialisms.length > 0 ? specialisms : undefined,
    };

    const coaches = await CoachService.searchCoaches(params);
    return coaches;
});

