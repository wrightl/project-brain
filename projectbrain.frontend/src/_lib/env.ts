import { z } from 'zod';

/**
 * Environment variable schema
 * Add all environment variables that should be validated here
 */
const envSchema = z.object({
    // Public env vars (available on client)
    NEXT_PUBLIC_AUTH0_DOMAIN: z.string().optional(),
    NEXT_PUBLIC_AUTH0_CLIENT_ID: z.string().optional(),
    NEXT_PUBLIC_AUTH0_AUDIENCE: z.string().optional(),
    
    // Server-only env vars
    API_SERVER_URL: z.string().url().optional(),
    AUTH0_SECRET: z.string().optional(),
    AUTH0_BASE_URL: z.string().url().optional(),
    AUTH0_ISSUER_BASE_URL: z.string().url().optional(),
    AUTH0_CLIENT_SECRET: z.string().optional(),
    
    // Node environment
    NODE_ENV: z.enum(['development', 'production', 'test']).default('development'),
});

type Env = z.infer<typeof envSchema>;

/**
 * Validated environment variables
 * This will throw an error at startup if required env vars are missing or invalid
 */
function getEnv(): Env {
    try {
        return envSchema.parse({
            NEXT_PUBLIC_AUTH0_DOMAIN: process.env.NEXT_PUBLIC_AUTH0_DOMAIN,
            NEXT_PUBLIC_AUTH0_CLIENT_ID: process.env.NEXT_PUBLIC_AUTH0_CLIENT_ID,
            NEXT_PUBLIC_AUTH0_AUDIENCE: process.env.NEXT_PUBLIC_AUTH0_AUDIENCE,
            API_SERVER_URL: process.env.API_SERVER_URL,
            AUTH0_SECRET: process.env.AUTH0_SECRET,
            AUTH0_BASE_URL: process.env.AUTH0_BASE_URL,
            AUTH0_ISSUER_BASE_URL: process.env.AUTH0_ISSUER_BASE_URL,
            AUTH0_CLIENT_SECRET: process.env.AUTH0_CLIENT_SECRET,
            NODE_ENV: process.env.NODE_ENV,
        });
    } catch (error) {
        if (error instanceof z.ZodError) {
            const missingVars = error.errors.map((e) => e.path.join('.')).join(', ');
            throw new Error(
                `❌ Invalid environment variables: ${missingVars}\n` +
                `Please check your .env file and ensure all required variables are set.`
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

