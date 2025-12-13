'use client';

import { onCLS, onFID, onFCP, onLCP, onTTFB, onINP, Metric } from 'web-vitals';

type ReportHandler = (metric: Metric) => void;

/**
 * Report Web Vitals metrics
 * Can be extended to send to analytics service
 */
function reportWebVital(metric: Metric) {
    // Log to console in development
    if (process.env.NODE_ENV === 'development') {
        console.log(metric);
    }

    // In production, you can send to analytics service
    // Example: send to Google Analytics, Sentry, or your own analytics endpoint
    if (process.env.NODE_ENV === 'production') {
        // Example: Send to analytics endpoint
        // fetch('/api/analytics', {
        //     method: 'POST',
        //     body: JSON.stringify(metric),
        //     headers: { 'Content-Type': 'application/json' },
        // });
    }
}

/**
 * Initialize Web Vitals monitoring
 * Call this in your app's entry point
 */
export function initWebVitals() {
    onCLS(reportWebVital);
    onFID(reportWebVital);
    onFCP(reportWebVital);
    onLCP(reportWebVital);
    onTTFB(reportWebVital);
    onINP(reportWebVital);
}

