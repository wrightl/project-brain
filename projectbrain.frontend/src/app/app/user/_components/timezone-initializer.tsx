'use client';

import { useEffect, useRef } from 'react';
import { apiClient } from '@/_lib/api-client';

export default function TimezoneInitializer() {
    const ranRef = useRef(false);

    useEffect(() => {
        if (ranRef.current) return;
        ranRef.current = true;

        const run = async () => {
            let tz: string | undefined;
            try {
                tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
            } catch {
                tz = undefined;
            }

            if (!tz) return;

            try {
                const current = await apiClient<{ timezone: string | null }>(
                    '/api/user/timezone'
                );
                if (current?.timezone !== tz) {
                    await apiClient<{ timezone: string | null }>(
                        '/api/user/timezone',
                        { method: 'PUT', body: { timezone: tz } }
                    );
                }
            } catch {
                // Best-effort only; don't block UI
            }
        };

        void run();
    }, []);

    return null;
}

