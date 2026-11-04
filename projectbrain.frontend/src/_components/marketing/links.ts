import { twMerge } from 'tailwind-merge';

/** Auth0 v4 has no /auth/signup route; signup is the login route with a screen hint. */
export const LOGIN_HREF = '/auth/login?returnTo=/app';
export const SIGNUP_HREF = '/auth/login?screen_hint=signup&returnTo=/app';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'outline-light';

const variants: Record<ButtonVariant, string> = {
    primary: 'bg-brand text-navy hover:bg-brand-hover',
    secondary: 'bg-navy text-white hover:bg-navy/90',
    ghost: 'bg-transparent text-navy hover:bg-wash',
    'outline-light': 'border border-white/30 text-white hover:bg-white/10',
};

const base =
    'inline-flex items-center justify-center gap-2 rounded-full px-5 py-2 text-sm tracking-wide transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand';

/** Shared classes so links can look like buttons without a polymorphic wrapper. */
export function buttonClasses(
    variant: ButtonVariant = 'primary',
    className?: string
): string {
    return twMerge(base, variants[variant], className);
}
