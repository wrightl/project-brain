'use client';

import React, { useMemo } from 'react';

interface UsageMeterProps {
    label: string;
    current: number;
    limit: number;
    unit?: string;
}

function UsageMeter({ label, current, limit, unit = '' }: UsageMeterProps) {
    const isUnlimited = useMemo(() => limit < 0, [limit]);
    const percentage = useMemo(
        () => (isUnlimited ? 0 : Math.min(100, (current / limit) * 100)),
        [current, limit, isUnlimited]
    );
    const isNearLimit = useMemo(() => !isUnlimited && percentage >= 80, [isUnlimited, percentage]);
    const isOverLimit = useMemo(() => !isUnlimited && current >= limit, [isUnlimited, current, limit]);

    return (
        <div className="space-y-1">
            <div className="flex justify-between text-sm">
                <span>{label}</span>
                <span className={isOverLimit ? 'text-red-600' : isNearLimit ? 'text-yellow-600' : ''}>
                    {current} {isUnlimited ? '' : `/ ${limit}`} {unit}
                </span>
            </div>
            {!isUnlimited && (
                <div className="w-full bg-gray-200 rounded-full h-2">
                    <div
                        role="progressbar"
                        aria-valuenow={current}
                        aria-valuemin={0}
                        aria-valuemax={limit}
                        aria-label={`${label}: ${current} of ${limit} ${unit}`.trim()}
                        className={`h-2 rounded-full transition-all ${
                            isOverLimit
                                ? 'bg-red-600'
                                : isNearLimit
                                ? 'bg-yellow-600'
                                : 'bg-blue-600'
                        }`}
                        style={{ width: `${percentage}%` }}
                    />
                </div>
            )}
        </div>
    );
}

export default React.memo(UsageMeter);

