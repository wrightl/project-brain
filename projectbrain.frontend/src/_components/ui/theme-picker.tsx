'use client';

import { useEffect, useState } from 'react';
import { usePathname } from 'next/navigation';
import toast from 'react-hot-toast';
import { Theme } from '@/_lib/theme-types';
import { DEFAULT_THEME, THEMES } from '@/_lib/theme-registry';
import { ThemeService } from '@/_services/theme-service';

const LOCAL_THEME_KEY = 'theme';

function applyTheme(theme: Theme) {
    document.documentElement.setAttribute('data-theme', theme);
}

function readLocalTheme(): Theme {
    if (typeof window === 'undefined') {
        return DEFAULT_THEME;
    }

    const stored = localStorage.getItem(LOCAL_THEME_KEY);
    if (stored && THEMES.some((theme) => theme.id === stored)) {
        return stored as Theme;
    }

    return DEFAULT_THEME;
}

export function ThemePicker() {
    const pathname = usePathname();
    const isAuthenticatedRoute = pathname?.startsWith('/app') ?? false;
    const [currentTheme, setCurrentTheme] = useState<Theme>(DEFAULT_THEME);
    const [isSaving, setIsSaving] = useState(false);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const loadTheme = async () => {
            try {
                if (isAuthenticatedRoute) {
                    const theme = await ThemeService.getTheme();
                    setCurrentTheme(theme);
                    applyTheme(theme);
                } else {
                    const theme = readLocalTheme();
                    setCurrentTheme(theme);
                    applyTheme(theme);
                }
            } catch (error) {
                console.error('Error loading theme:', error);
                const fallback = readLocalTheme();
                setCurrentTheme(fallback);
                applyTheme(fallback);
            } finally {
                setIsLoading(false);
            }
        };

        loadTheme();
    }, [isAuthenticatedRoute]);

    const handleThemeChange = async (newTheme: Theme) => {
        if (isSaving || newTheme === currentTheme) return;

        setIsSaving(true);
        setCurrentTheme(newTheme);
        applyTheme(newTheme);
        localStorage.setItem(LOCAL_THEME_KEY, newTheme);

        if (isAuthenticatedRoute) {
            try {
                await ThemeService.setTheme({ theme: newTheme });
                toast.success('Theme preference saved');
            } catch (error) {
                console.error('Error saving theme:', error);
                toast.error('Failed to save theme preference');
            }
        }

        setIsSaving(false);
    };

    if (isLoading) {
        return (
            <div className="h-9 w-full max-w-xs animate-pulse rounded-md bg-gray-200" />
        );
    }

    return (
        <label className="block">
            <span className="sr-only">Theme</span>
            <select
                value={currentTheme}
                onChange={(event) =>
                    handleThemeChange(event.target.value as Theme)
                }
                disabled={isSaving}
                className="block w-full max-w-xs rounded-md border border-gray-300 bg-white px-3 py-2 text-sm text-gray-900 shadow-sm focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 disabled:opacity-50"
            >
                {THEMES.map((theme) => (
                    <option key={theme.id} value={theme.id}>
                        {theme.label}
                    </option>
                ))}
            </select>
        </label>
    );
}
