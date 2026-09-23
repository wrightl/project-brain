interface AdminKpiCardProps {
    label: string;
    value: string | number;
    subtitle?: string;
}

export function AdminKpiCard({ label, value, subtitle }: AdminKpiCardProps) {
    return (
        <div className="rounded-lg border border-gray-300 bg-white p-6 shadow">
            <p className="text-sm font-medium text-gray-500">{label}</p>
            <p className="mt-2 text-2xl font-bold text-gray-900 tracking-tight">
                {value}
            </p>
            {subtitle && (
                <p className="mt-1 text-xs font-medium text-[color:var(--indigo)]">
                    {subtitle}
                </p>
            )}
        </div>
    );
}
