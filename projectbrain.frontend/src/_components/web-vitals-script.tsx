'use client';

import { useEffect } from 'react';
import { initWebVitals } from '@/_lib/web-vitals';

/**
 * Client component to initialize Web Vitals monitoring
 */
export function WebVitalsScript() {
    useEffect(() => {
        initWebVitals();
    }, []);

    return null;
}

