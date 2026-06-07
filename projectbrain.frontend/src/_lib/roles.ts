export const AppRoles = {
    User: 'user',
    Coach: 'coach',
    Admin: 'admin',
} as const;

export type UserRole = (typeof AppRoles)[keyof typeof AppRoles];

export const ALL_APP_ROLES: UserRole[] = Object.values(AppRoles);

export const AUTH_ROLES_CLAIM = 'https://projectbrain.app/roles' as const;
