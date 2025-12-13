export type AvailabilityStatus = 'Available' | 'Busy' | 'Away' | 'Offline';

/**
 * Converts the backend enum integer to the frontend string type
 * Backend enum: Available = 0, Busy = 1, Away = 2, Offline = 3
 */
export function convertAvailabilityStatus(
    status: number | string | undefined | null
): AvailabilityStatus {
    if (typeof status === 'string') {
        // Already a string, validate and return
        const validStatuses: AvailabilityStatus[] = [
            'Available',
            'Busy',
            'Away',
            'Offline',
        ];
        if (validStatuses.includes(status as AvailabilityStatus)) {
            return status as AvailabilityStatus;
        }
    }

    if (typeof status === 'number') {
        // Convert integer enum to string
        const statusMap: Record<number, AvailabilityStatus> = {
            0: 'Available',
            1: 'Busy',
            2: 'Away',
            3: 'Offline',
        };
        return statusMap[status] || 'Offline';
    }

    // Default to Offline if status is undefined, null, or invalid
    return 'Offline';
}
