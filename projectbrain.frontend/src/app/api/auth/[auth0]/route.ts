import { handleAuth, handleLogin } from '@auth0/nextjs-auth0';

export const GET = handleAuth({
    login: handleLogin({
        returnTo: '/app',
        authorizationParams: {
            // Add the `offline_access` scope to also get a Refresh Token
            scope: 'openid profile email read:weather_forecast', // or AUTH0_SCOPE
        },
    }),
    signup: handleLogin({
        authorizationParams: {
            screen_hint: 'signup',
        },
        returnTo: '/app',
    }),
});
