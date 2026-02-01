'use client';

import { useMemo } from 'react';
import { useYearlyHabitsCalendar } from '@/_hooks/queries/use-habits';
import type {
    YearlyHabitsCalendarDay,
    YearlyGoalsStatus,
} from '@/_services/habits-service';
import { Skeleton } from '@/_components/ui/skeleton';

export type YearlyHabitsCalendarMode = 'journal' | 'goals' | 'both';

function parseLocalDate(dateString: string): Date {
    // dateString is yyyy-MM-dd and should be interpreted as a local date.
    const [y, m, d] = dateString.split('-').map((n) => parseInt(n, 10));
    return new Date(y, (m ?? 1) - 1, d ?? 1);
}

function getJournalColor(hasEntry: boolean): string {
    return hasEntry ? 'var(--emerald)' : 'var(--orange)';
}

function getGoalsColor(status: YearlyGoalsStatus): string {
    switch (status) {
        case 'AllCompleted':
            return 'var(--emerald)';
        case 'SomeCompleted':
            return 'var(--mandarine)';
        case 'NoneCompleted':
            return 'var(--orange)';
        case 'NoneSet':
        default:
            return 'var(--aluminium)';
    }
}

function getGoalsLabel(status: YearlyGoalsStatus): string {
    switch (status) {
        case 'AllCompleted':
            return 'All goals completed';
        case 'SomeCompleted':
            return 'Some goals completed';
        case 'NoneCompleted':
            return 'No goals completed';
        case 'NoneSet':
        default:
            return 'No goals set';
    }
}

function buildWeeks(days: YearlyHabitsCalendarDay[]) {
    const sorted = [...days].sort((a, b) => a.date.localeCompare(b.date));
    if (sorted.length === 0) {
        return {
            weeks: [] as Array<Array<YearlyHabitsCalendarDay | null>>,
            start: null as string | null,
        };
    }

    const start = sorted[0].date;
    const startDate = parseLocalDate(start);
    const startDow = startDate.getDay(); // 0=Sun..6=Sat
    const startRow = (startDow + 6) % 7; // 0=Mon..6=Sun

    const total = sorted.length;
    const weeksCount = Math.floor((startRow + total - 1) / 7) + 1;
    const weeks: Array<Array<YearlyHabitsCalendarDay | null>> = Array.from(
        { length: weeksCount },
        () => Array.from({ length: 7 }, () => null),
    );

    for (let i = 0; i < total; i++) {
        const pos = startRow + i;
        const weekIndex = Math.floor(pos / 7);
        const rowIndex = pos % 7;
        weeks[weekIndex][rowIndex] = sorted[i];
    }

    return { weeks, start };
}

export default function YearlyHabitsCalendar({
    mode = 'both',
}: {
    mode?: YearlyHabitsCalendarMode;
}) {
    const { data, isLoading, error } = useYearlyHabitsCalendar();

    const days = useMemo(() => data?.days ?? [], [data?.days]);
    const { weeks } = useMemo(() => buildWeeks(days), [days]);

    if (isLoading) {
        return (
            <div className="bg-white rounded-lg shadow-sm p-4">
                <div className="flex items-center justify-between">
                    <div>
                        <div className="text-gray-900 font-semibold">
                            Past year
                        </div>
                        <div className="text-sm text-gray-600">
                            Goals and journaling consistency
                        </div>
                    </div>
                </div>
                <div className="mt-4 overflow-x-auto">
                    <div className="min-w-[760px]">
                        <Skeleton height={120} />
                    </div>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="bg-white rounded-lg shadow-sm p-4">
                <div className="text-gray-900 font-semibold">Past year</div>
                <p className="mt-2 text-sm text-red-600">
                    Failed to load habits calendar.
                </p>
            </div>
        );
    }

    if (!data || days.length === 0) {
        return (
            <div className="bg-white rounded-lg shadow-sm p-4">
                <div className="text-gray-900 font-semibold">Past year</div>
                <p className="mt-2 text-sm text-gray-600">
                    No data available yet.
                </p>
            </div>
        );
    }

    const rangeLabel = `${data.startDate} → ${data.endDate}`;

    return (
        <div className="bg-white rounded-lg shadow-sm p-4">
            <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                <div>
                    <div className="text-gray-900 font-semibold">Past year</div>
                    <div className="text-sm text-gray-600">{rangeLabel}</div>
                </div>
            </div>

            <div className="mt-4 overflow-x-auto">
                <div className="min-w-[760px]">
                    <div className="flex gap-1">
                        {weeks.map((week, weekIdx) => (
                            <div key={weekIdx} className="flex flex-col gap-1">
                                {week.map((day, rowIdx) => {
                                    if (!day) {
                                        return (
                                            <div
                                                key={rowIdx}
                                                className="h-3 w-3 rounded-sm bg-gray-200"
                                                title=""
                                            />
                                        );
                                    }

                                    const goalsLabel = getGoalsLabel(
                                        day.goalsStatus,
                                    );
                                    const journalLabel = day.hasJournalEntry
                                        ? 'Journal entry added'
                                        : 'No journal entry';

                                    const title =
                                        mode === 'journal'
                                            ? `${day.date}\n${journalLabel}`
                                            : mode === 'goals'
                                            ? `${day.date}\n${goalsLabel}`
                                            : `${day.date}\n${journalLabel}\n${goalsLabel}`;

                                    const aria =
                                        mode === 'journal'
                                            ? `${day.date}. ${journalLabel}.`
                                            : mode === 'goals'
                                            ? `${day.date}. ${goalsLabel}.`
                                            : `${day.date}. ${journalLabel}. ${goalsLabel}.`;

                                    return (
                                        <div
                                            key={rowIdx}
                                            className="h-3 w-3 overflow-hidden rounded-sm border border-gray-200"
                                            title={title}
                                            aria-label={aria}
                                        >
                                            {mode === 'journal' ? (
                                                <div
                                                    className="h-full w-full"
                                                    style={{
                                                        backgroundColor:
                                                            getJournalColor(
                                                                day.hasJournalEntry,
                                                            ),
                                                    }}
                                                />
                                            ) : mode === 'goals' ? (
                                                <div
                                                    className="h-full w-full"
                                                    style={{
                                                        backgroundColor:
                                                            getGoalsColor(
                                                                day.goalsStatus,
                                                            ),
                                                    }}
                                                />
                                            ) : (
                                                <div className="flex h-full flex-col">
                                                    <div
                                                        className="h-1/2 w-full"
                                                        style={{
                                                            backgroundColor:
                                                                getJournalColor(
                                                                    day.hasJournalEntry,
                                                                ),
                                                        }}
                                                    />
                                                    <div
                                                        className="h-1/2 w-full"
                                                        style={{
                                                            backgroundColor:
                                                                getGoalsColor(
                                                                    day.goalsStatus,
                                                                ),
                                                        }}
                                                    />
                                                </div>
                                            )}
                                        </div>
                                    );
                                })}
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </div>
    );
}
