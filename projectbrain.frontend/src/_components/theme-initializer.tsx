'use client';

import { useEffect } from 'react';
import { usePathname } from 'next/navigation';
import { ThemeService } from '@/_services/theme-service';

export function ThemeInitializer() {
    const pathname = usePathname();
    const isAuthenticatedRoute = pathname?.startsWith('/app') ?? false;

    useEffect(() => {
        // Skip theme initialization on public routes
        if (!isAuthenticatedRoute) {
            return;
        }

        // Initialize theme on mount
        const initializeTheme = async () => {
            try {
                const theme = await ThemeService.getTheme();
                const html = document.documentElement;

                // Remove all theme attributes
                html.removeAttribute('data-theme');

                // Apply theme if not standard
                if (theme !== 'standard') {
                    html.setAttribute('data-theme', theme);
                }
            } catch (error) {
                console.error('Error initializing theme:', error);
                // Default to standard theme
                document.documentElement.removeAttribute('data-theme');
            }
        };

        initializeTheme();
    }, [isAuthenticatedRoute]);

    return null;
}
