interface AdminKpiCardProps {
    label: string;
    value: string | number;
    subtitle?: string;
    accentColor?: string;
}

export function AdminKpiCard({
    label,
    value,
    subtitle,
    accentColor = 'text-emerald-500',
}: AdminKpiCardProps) {
    return (
        <div
            className="rounded-lg p-6 text-gray-900 border border-gray-300 shadow"
            style={{ background: 'var(--dashboard-card-bg)' }}
        >
            <p className="text-sm font-medium text-gray-500">{label}</p>
            <p className="mt-2 text-2xl font-bold text-white tracking-tight">
                {value}
            </p>
            {subtitle && (
                <p className={`mt-1 text-xs font-medium ${accentColor}`}>
                    {subtitle}
                </p>
            )}
        </div>
    );
}
