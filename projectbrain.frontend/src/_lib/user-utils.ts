import { User, Coach } from './types';

/**
 * Type guard to check if a user is a Coach
 */
export function isCoach(user: User | Coach): user is Coach {
    return 'coachProfileId' in user;
}

/**
 * Type guard to check if a user is a regular User
 */
export function isUser(user: User | Coach): user is User {
    return 'userProfileId' in user;
}

/**
 * Get the user ID from either a User or Coach object
 */
export function getUserId(user: User | Coach): string {
    if (isCoach(user)) {
        return user.coachProfileId;
    }
    if (isUser(user)) {
        return user.userProfileId;
    }
    // Fallback to id if present
    return (user as { id?: string }).id || '';
}

