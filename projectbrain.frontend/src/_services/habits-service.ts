import { callBackendApi } from '@/_lib/backend-api';

export type YearlyGoalsStatus =
    | 'NoneSet'
    | 'NoneCompleted'
    | 'SomeCompleted'
    | 'AllCompleted';

export interface YearlyHabitsCalendarDay {
    date: string; // yyyy-MM-dd (user local)
    hasJournalEntry: boolean;
    goalsStatus: YearlyGoalsStatus;
}

export interface YearlyHabitsCalendarResponse {
    startDate: string; // yyyy-MM-dd
    endDate: string; // yyyy-MM-dd
    days: YearlyHabitsCalendarDay[];
}

export class HabitsService {
    static async getYearlyCalendar(): Promise<YearlyHabitsCalendarResponse> {
        const response = await callBackendApi('/habits/yearly-calendar', {
            method: 'GET',
        });

        if (!response.ok) {
            throw new Error('Failed to fetch yearly habits calendar');
        }

        const data = await response.json();

        // Backend currently returns enum as number; map to stable string union.
        const mapGoalsStatus = (value: unknown): YearlyGoalsStatus => {
            if (typeof value === 'string') {
                return value as YearlyGoalsStatus;
            }
            if (typeof value === 'number') {
                switch (value) {
                    case 0:
                        return 'NoneSet';
                    case 1:
                        return 'NoneCompleted';
                    case 2:
                        return 'SomeCompleted';
                    case 3:
                        return 'AllCompleted';
                }
            }
            return 'NoneSet';
        };

        const days: YearlyHabitsCalendarDay[] = Array.isArray(data?.days)
            ? data.days.map((d: any) => ({
                  date: String(d.date),
                  hasJournalEntry: Boolean(d.hasJournalEntry),
                  goalsStatus: mapGoalsStatus(d.goalsStatus),
              }))
            : [];

        return {
            startDate: String(data?.startDate ?? ''),
            endDate: String(data?.endDate ?? ''),
            days,
        };
    }
}

