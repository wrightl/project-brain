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
    // Apply theme to all app routes (user, coach, admin, and any /app/*)
    const isAppRoute = typeof pathname === 'string' && pathname.startsWith('/app');

    if (isAppRoute) {
        return (
            <ThemeProvider>
                <ThemeInitializer />
                {children}
            </ThemeProvider>
        );
    }

    return <>{children}</>;
}

