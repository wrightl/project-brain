'use client';

import { QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { getQueryClient } from '@/_lib/query-client';
import { useState } from 'react';
import { ConditionalThemeProvider } from './conditional-theme-provider';

export function Providers({ children }: { children: React.ReactNode }) {
    // Use useState to ensure we use the same query client instance
    const [queryClient] = useState(() => getQueryClient());

    return (
        <QueryClientProvider client={queryClient}>
            <ConditionalThemeProvider>
                {children}
                {process.env.NODE_ENV === 'development' && (
                    <ReactQueryDevtools initialIsOpen={false} />
                )}
            </ConditionalThemeProvider>
        </QueryClientProvider>
    );
}
