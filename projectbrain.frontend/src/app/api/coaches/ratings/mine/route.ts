import { createApiRoute } from '@/_lib/api-route-handler';
import { PagedResponse } from '@/_lib/types';
import { NextRequest } from 'next/server';
import {
    CoachRating,
    CoachRatingService,
} from '@/_services/coach-rating-service';

export const GET = createApiRoute<PagedResponse<CoachRating>>(
    async (req: NextRequest) => {
        const pathname = req.nextUrl.pathname;

        const { searchParams } = new URL(req.url);
        const page = searchParams.get('page');
        const pageSize = searchParams.get('pageSize');

        return await CoachRatingService.getMyRatings({
            page: page ? parseInt(page, 10) : undefined,
            pageSize: pageSize ? parseInt(pageSize, 10) : undefined,
        });
    }
);
