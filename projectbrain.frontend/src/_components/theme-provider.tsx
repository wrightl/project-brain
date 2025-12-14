'use client';

import {
    createContext,
    useContext,
    useEffect,
    useState,
    ReactNode,
} from 'react';
import { Theme } from '@/_lib/theme-types';
import { ThemeService } from '@/_services/theme-service';

interface ThemeContextType {
    theme: Theme;
    setTheme: (theme: Theme) => void;
    isLoading: boolean;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export function useTheme() {
    const context = useContext(ThemeContext);
    if (!context) {
        throw new Error('useTheme must be used within ThemeProvider');
    }
    return context;
}

interface ThemeProviderProps {
    children: ReactNode;
    initialTheme?: Theme;
}

export function ThemeProvider({
    children,
    initialTheme = 'standard',
}: ThemeProviderProps) {
    const [theme, setThemeState] = useState<Theme>(initialTheme);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        // Load theme from user preferences
        const loadTheme = async () => {
            try {
                const userTheme = await ThemeService.getTheme();
                setThemeState(userTheme);
                applyTheme(userTheme);
            } catch (error) {
                console.error('Error loading theme:', error);
                applyTheme('standard');
            } finally {
                setIsLoading(false);
            }
        };

        loadTheme();
    }, []);

    const applyTheme = (newTheme: Theme) => {
        // Remove all theme classes
        document.documentElement.removeAttribute('data-theme');

        if (newTheme !== 'standard') {
            document.documentElement.setAttribute('data-theme', newTheme);
        }
    };

    const setTheme = (newTheme: Theme) => {
        setThemeState(newTheme);
        applyTheme(newTheme);
    };

    return (
        <ThemeContext.Provider value={{ theme, setTheme, isLoading }}>
            {children}
        </ThemeContext.Provider>
    );
}
