'use client';

import React from 'react';

type Limit = number | null | undefined;

function clamp01(value: number) {
    return Math.max(0, Math.min(1, value));
}

function formatNumber(value: number, decimals?: number) {
    if (Number.isNaN(value)) return 'N/A';
    if (decimals === undefined) return value.toLocaleString();
    return value.toFixed(decimals);
}

function formatLimit(limit: Limit) {
    if (limit === null || limit === undefined) return 'N/A';
    if (limit < 0) return 'Unlimited';
    return limit.toLocaleString();
}

function getPercent(used: number, limit: Limit) {
    if (limit === null || limit === undefined) return null;
    if (limit < 0) return 1;
    if (limit === 0) return used > 0 ? 1 : 0;
    return clamp01(used / limit);
}

function getColorClass(percent: number | null, limit: Limit) {
    if (limit === null || limit === undefined) return 'text-gray-300';
    if (limit < 0) return 'text-indigo-600';
    if (percent === null) return 'text-gray-300';
    if (percent >= 1) return 'text-red-600';
    if (percent >= 0.8) return 'text-amber-500';
    return 'text-emerald-600';
}

function Donut({
    used,
    limit,
    decimals,
}: {
    used: number;
    limit: Limit;
    decimals?: number;
}) {
    const percent = getPercent(used, limit);
    const colorClass = getColorClass(percent, limit);
    const radius = 18;
    const cx = 24;
    const cy = 24;
    const circumference = 2 * Math.PI * radius;
    const dashOffset =
        percent === null ? circumference : circumference * (1 - percent);

    const centerLabel =
        limit === null || limit === undefined
            ? 'N/A'
            : limit < 0
              ? '∞'
              : `${Math.round((percent ?? 0) * 100)}%`;

    return (
        <div className="relative h-14 w-14">
            <svg viewBox="0 0 48 48" className="h-14 w-14" aria-hidden="true">
                <circle
                    cx={cx}
                    cy={cy}
                    r={radius}
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="6"
                    className="text-gray-200"
                />
                <circle
                    cx={cx}
                    cy={cy}
                    r={radius}
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="6"
                    strokeLinecap="round"
                    strokeDasharray={circumference}
                    strokeDashoffset={dashOffset}
                    transform="rotate(-90 24 24)"
                    className={colorClass}
                />
            </svg>

            <div className="absolute inset-0 flex flex-col items-center justify-center">
                <div className="text-xs font-semibold text-gray-900">
                    {centerLabel}
                </div>
            </div>
        </div>
    );
}

export function UsagePieCard({
    title,
    used,
    limit,
    unit,
    decimals,
    helperText,
}: {
    title: string;
    used: number;
    limit: Limit;
    unit?: string;
    decimals?: number;
    helperText?: string;
}) {
    return (
        <div className="flex items-center justify-between gap-4 rounded-lg border border-gray-200 p-4">
            <div className="min-w-0">
                <div className="text-sm font-medium text-gray-900">{title}</div>
                <div className="mt-1 text-xs text-gray-600">
                    {formatNumber(used, decimals)}
                    {unit ? ` ${unit}` : ''} / {formatLimit(limit)}
                    {unit && limit !== null && limit !== undefined && limit >= 0
                        ? ` ${unit}`
                        : ''}
                </div>
                {helperText && (
                    <div className="mt-1 text-xs text-gray-500">
                        {helperText}
                    </div>
                )}
            </div>

            <Donut used={used} limit={limit} decimals={decimals} />
        </div>
    );
}
