'use client';

interface UsageMeterProps {
    label: string;
    current: number;
    limit: number;
    unit?: string;
}

export default function UsageMeter({ label, current, limit, unit = '' }: UsageMeterProps) {
    const isUnlimited = limit < 0;
    const percentage = isUnlimited ? 0 : Math.min(100, (current / limit) * 100);
    const isNearLimit = !isUnlimited && percentage >= 80;
    const isOverLimit = !isUnlimited && current >= limit;

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

