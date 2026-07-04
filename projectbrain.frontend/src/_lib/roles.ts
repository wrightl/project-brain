export const AppRoles = {
    User: 'user',
    Coach: 'coach',
    Admin: 'admin',
} as const;

export type UserRole = (typeof AppRoles)[keyof typeof AppRoles];

export const ALL_APP_ROLES: UserRole[] = Object.values(AppRoles);

export const AUTH_ROLES_CLAIM = 'https://projectbrain.app/roles' as const;

const ROLE_PRIORITY: UserRole[] = [AppRoles.Admin, AppRoles.Coach, AppRoles.User];

export function getPrimaryRole(roles?: string[] | null): UserRole | undefined {
    if (!roles?.length) {
        return undefined;
    }

    for (const role of ROLE_PRIORITY) {
        if (roles.includes(role)) {
            return role;
        }
    }

    return roles[0] as UserRole;
}
