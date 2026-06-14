'use client';

import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { Theme } from '@/_lib/theme-types';
import { THEMES } from '@/_lib/theme-registry';
import { ThemeService } from '@/_services/theme-service';

export function ThemeSelector() {
    const [currentTheme, setCurrentTheme] = useState<Theme>('standard');
    const [isSaving, setIsSaving] = useState(false);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const loadTheme = async () => {
            try {
                const theme = await ThemeService.getTheme();
                setCurrentTheme(theme);
            } catch (error) {
                console.error('Error loading theme:', error);
            } finally {
                setIsLoading(false);
            }
        };

        loadTheme();
    }, []);

    const handleThemeChange = async (newTheme: Theme) => {
        if (isSaving || newTheme === currentTheme) return;

        setIsSaving(true);
        try {
            await ThemeService.setTheme({ theme: newTheme });
            setCurrentTheme(newTheme);
            document.documentElement.setAttribute('data-theme', newTheme);
            toast.success('Theme preference saved successfully');
        } catch (error) {
            console.error('Error saving theme:', error);
            toast.error('Failed to save theme preference');
        } finally {
            setIsSaving(false);
        }
    };

    if (isLoading) {
        return (
            <div className="bg-white shadow rounded-lg p-6">
                <div className="animate-pulse space-y-4">
                    <div className="h-6 bg-gray-200 rounded w-1/4"></div>
                    <div className="h-10 bg-gray-200 rounded"></div>
                </div>
            </div>
        );
    }

    return (
        <div className="bg-white shadow rounded-lg p-6">
            <div className="mb-6">
                <h2 className="text-xl font-semibold text-gray-900">
                    Preferences
                </h2>
                <p className="mt-1 text-sm text-gray-600">
                    Customize your app experience
                </p>
            </div>

            <div className="space-y-4">
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                        Theme
                    </label>
                    <div className="space-y-2">
                        {THEMES.map((theme) => (
                            <ThemeOption
                                key={theme.id}
                                value={theme.id}
                                label={theme.label}
                                description={theme.description}
                                currentTheme={currentTheme}
                                onChange={handleThemeChange}
                                disabled={isSaving}
                            />
                        ))}
                    </div>
                </div>
            </div>
        </div>
    );
}

interface ThemeOptionProps {
    value: Theme;
    label: string;
    description: string;
    currentTheme: Theme;
    onChange: (theme: Theme) => void;
    disabled: boolean;
}

function ThemeOption({
    value,
    label,
    description,
    currentTheme,
    onChange,
    disabled,
}: ThemeOptionProps) {
    const isSelected = currentTheme === value;

    return (
        <button
            type="button"
            onClick={() => onChange(value)}
            disabled={disabled}
            className={`w-full text-left p-4 rounded-lg border-2 transition-all ${
                isSelected
                    ? 'border-indigo-500 bg-indigo-50'
                    : 'border-gray-200 hover:border-gray-300 bg-white'
            } ${disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer'}`}
        >
            <div className="flex items-start justify-between">
                <div className="flex-1">
                    <div className="flex items-center gap-2">
                        <div
                            className={`w-4 h-4 rounded-full border-2 flex items-center justify-center ${
                                isSelected
                                    ? 'border-indigo-500'
                                    : 'border-gray-300'
                            }`}
                        >
                            {isSelected && (
                                <div className="w-2 h-2 rounded-full bg-indigo-500" />
                            )}
                        </div>
                        <span className="font-medium text-gray-900">
                            {label}
                        </span>
                    </div>
                    <p className="mt-1 text-sm text-gray-600 ml-6">
                        {description}
                    </p>
                </div>
            </div>
        </button>
    );
}
