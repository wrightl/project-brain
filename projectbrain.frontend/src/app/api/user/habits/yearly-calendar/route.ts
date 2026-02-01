import { createApiRoute } from '@/_lib/api-route-handler';
import {
    HabitsService,
    type YearlyHabitsCalendarResponse,
} from '@/_services/habits-service';

export const GET = createApiRoute<YearlyHabitsCalendarResponse>(async () => {
    return HabitsService.getYearlyCalendar();
});

