import { useQuery } from '@tanstack/react-query';
import { apiClient } from '@/_lib/api-client';
import type { YearlyHabitsCalendarResponse } from '@/_services/habits-service';

export const habitsKeys = {
    all: ['habits'] as const,
    yearlyCalendar: () => [...habitsKeys.all, 'yearlyCalendar'] as const,
};

export function useYearlyHabitsCalendar() {
    return useQuery<YearlyHabitsCalendarResponse>({
        queryKey: habitsKeys.yearlyCalendar(),
        queryFn: () =>
            apiClient<YearlyHabitsCalendarResponse>(
                '/api/user/habits/yearly-calendar',
            ),
        staleTime: 10 * 60 * 1000, // 10 minutes
    });
}

