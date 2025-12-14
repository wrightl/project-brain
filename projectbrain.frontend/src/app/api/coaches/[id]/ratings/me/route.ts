import { createApiRoute } from '@/_lib/api-route-handler';
import { CoachRatingService, CoachRating } from '@/_services/coach-rating-service';
import { NextRequest } from 'next/server';

export const GET = createApiRoute<CoachRating | null>(
    async (req: NextRequest) => {
        const pathname = req.nextUrl.pathname;
        const coachId = pathname.split('/')[3]; // /api/coaches/[id]/ratings/me

        if (!coachId) {
            throw new Error('Coach ID is required');
        }

        return await CoachRatingService.getMyRating(coachId);
    }
);

