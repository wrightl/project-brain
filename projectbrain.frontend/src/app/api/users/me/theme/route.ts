import { NextRequest } from 'next/server';
import { createApiRoute } from '@/_lib/api-route-handler';
import { Theme, parseThemeFromPreferences } from '@/_lib/theme-types';
import { UserService } from '@/_services/user-service';
import { BackendApiError } from '@/_lib/backend-api';

export const GET = createApiRoute<{ theme: Theme }>(
    async (req: NextRequest) => {
        try {
            const result = await UserService.getCurrentUserTheme();
            return result;
        } catch (error) {
            console.error('Error getting theme:', error);
            return { theme: 'standard' as Theme };
        }
    }
);

export const PUT = createApiRoute<{ theme: Theme }>(
    async (req: NextRequest) => {
        const body = await req.json();
        const { theme } = body;

        if (!theme) {
            throw new BackendApiError(400, 'Theme and userId are required');
        }

        if (!['standard', 'dark', 'colourful'].includes(theme)) {
            throw new BackendApiError(400, 'Invalid theme value');
        }

        try {
            const result = await UserService.updateCurrentUserTheme(theme);

            return result;
        } catch (error) {
            console.error('Error setting theme:', error);
            throw error;
        }
    }
);
