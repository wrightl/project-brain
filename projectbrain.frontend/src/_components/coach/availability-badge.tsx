type AvailabilityStatus = 'Available' | 'Busy' | 'Away' | 'Offline';

interface AvailabilityBadgeProps {
    status?: AvailabilityStatus;
    size?: 'sm' | 'md';
}

const statusConfig = {
    Available: {
        label: 'Available',
        className: 'bg-green-100 text-green-800 border-green-300',
    },
    Busy: {
        label: 'Busy',
        className: 'bg-red-100 text-red-800 border-red-300',
    },
    Away: {
        label: 'Away',
        className: 'bg-yellow-100 text-yellow-800 border-yellow-300',
    },
    Offline: {
        label: 'Offline',
        className: 'bg-gray-100 text-gray-800 border-gray-300',
    },
};

export default function AvailabilityBadge({
    status = 'Offline',
    size = 'md',
}: AvailabilityBadgeProps) {
    const config = statusConfig[status] || statusConfig.Offline;
    const sizeClass = size === 'sm' ? 'text-xs px-2 py-0.5' : 'text-sm px-2.5 py-1';

    return (
        <span
            className={`inline-flex items-center font-medium rounded border ${config.className} ${sizeClass}`}
        >
            {config.label}
        </span>
    );
}

