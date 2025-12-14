'use client';

import { usePathname } from 'next/navigation';
import { ReactNode } from 'react';
import { ThemeProvider } from './theme-provider';
import { ThemeInitializer } from './theme-initializer';

interface ConditionalThemeProviderProps {
    children: ReactNode;
}

export function ConditionalThemeProvider({
    children,
}: ConditionalThemeProviderProps) {
    const pathname = usePathname();
    const isAuthenticatedRoute = pathname?.startsWith('/app') ?? false;

    // Only apply themes for authenticated routes
    if (isAuthenticatedRoute) {
        return (
            <ThemeProvider>
                <ThemeInitializer />
                {children}
            </ThemeProvider>
        );
    }

    // For public routes, just render children without theme logic
    return <>{children}</>;
}

