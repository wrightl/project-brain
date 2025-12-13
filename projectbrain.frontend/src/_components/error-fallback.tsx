'use client';

import { ErrorBoundary } from './error-boundary';

interface ErrorFallbackWrapperProps {
    children: React.ReactNode;
}

/**
 * Wrapper component that provides error boundary functionality
 * Use this to wrap sections of your app that should have isolated error handling
 */
export function ErrorFallbackWrapper({ children }: ErrorFallbackWrapperProps) {
    return (
        <ErrorBoundary>
            {children}
        </ErrorBoundary>
    );
}

