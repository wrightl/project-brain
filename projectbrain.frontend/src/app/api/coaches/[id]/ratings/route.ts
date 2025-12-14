import { createApiRoute } from '@/_lib/api-route-handler';
import { CoachRatingService, CoachRating } from '@/_services/coach-rating-service';
import { PagedResponse } from '@/_lib/types';
import { NextRequest } from 'next/server';

export const POST = createApiRoute<CoachRating>(
    async (req: NextRequest) => {
        const pathname = req.nextUrl.pathname;
        const coachId = pathname.split('/')[3]; // /api/coaches/[id]/ratings

        if (!coachId) {
            throw new Error('Coach ID is required');
        }

        const body = await req.json();
        return await CoachRatingService.createOrUpdateRating(coachId, body);
    }
);

export const GET = createApiRoute<PagedResponse<CoachRating>>(
    async (req: NextRequest) => {
        const pathname = req.nextUrl.pathname;
        const coachId = pathname.split('/')[3]; // /api/coaches/[id]/ratings

        if (!coachId) {
            throw new Error('Coach ID is required');
        }

        const { searchParams } = new URL(req.url);
        const page = searchParams.get('page');
        const pageSize = searchParams.get('pageSize');

        return await CoachRatingService.getRatings(coachId, {
            page: page ? parseInt(page, 10) : undefined,
            pageSize: pageSize ? parseInt(pageSize, 10) : undefined,
        });
    }
);

