import { z } from 'zod';

/**
 * Environment variable schema
 * Add all environment variables that should be validated here
 */
const envSchema = z.object({
    // Public env vars (available on client)
    NEXT_PUBLIC_GOOGLE_MAPS_API_KEY: z.string().optional(),

    // Server-only env vars
    API_SERVER_URL: z.string().url().optional(),
    AUTH0_SECRET: z.string().optional(),
    AUTH0_CLIENT_SECRET: z.string().optional(),
    GOOGLE_MAPS_GEOCODING_API_KEY: z.string().optional(),
    GOOGLE_MAPS_PLACES_API_KEY: z.string().optional(),

    // Node environment
    NODE_ENV: z
        .enum(['development', 'production', 'test'])
        .default('development'),
});

type Env = z.infer<typeof envSchema>;

/**
 * Validated environment variables
 * This will throw an error at startup if required env vars are missing or invalid
 */
function getEnv(): Env {
    try {
        return envSchema.parse({
            NEXT_PUBLIC_GOOGLE_MAPS_API_KEY:
                process.env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY,
            API_SERVER_URL: process.env.API_SERVER_URL,
            AUTH0_SECRET: process.env.AUTH0_SECRET,
            AUTH0_CLIENT_SECRET: process.env.AUTH0_CLIENT_SECRET,
            GOOGLE_MAPS_GEOCODING_API_KEY:
                process.env.GOOGLE_MAPS_GEOCODING_API_KEY,
            GOOGLE_MAPS_PLACES_API_KEY: process.env.GOOGLE_MAPS_PLACES_API_KEY,
            NODE_ENV: process.env.NODE_ENV,
        });
    } catch (error) {
        if (error instanceof z.ZodError) {
            const missingVars = error.issues
                .map((e) => e.path.join('.'))
                .join(', ');
            throw new Error(
                `❌ Invalid environment variables: ${missingVars}\n` +
                    `Please check your .env file and ensure all required variables are set.`,
            );
        }
        throw error;
    }
}

export const env = getEnv();

/**
 * Type-safe access to environment variables
 * Use this instead of process.env directly
 */
export default env;
