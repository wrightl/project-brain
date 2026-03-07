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
                // Always set data-theme so theme overrides (higher specificity) win over Tailwind
                document.documentElement.setAttribute('data-theme', theme);
            } catch (error) {
                console.error('Error initializing theme:', error);
                document.documentElement.setAttribute('data-theme', 'standard');
            }
        };

        initializeTheme();
    }, [isAuthenticatedRoute]);

    return null;
}
