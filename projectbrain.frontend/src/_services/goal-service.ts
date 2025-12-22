import { callBackendApi } from '@/_lib/backend-api';

export interface Goal {
    id: string;
    index: number;
    message: string;
    completed: boolean;
    completedAt?: string | null;
    createdAt: string;
    updatedAt: string;
}

export interface CreateOrUpdateGoalsRequest {
    goals: string[];
}

export interface CompleteGoalRequest {
    completed: boolean;
}

export class GoalService {
    /**
     * Get today's goals
     */
    static async getTodaysGoals(): Promise<Goal[]> {
        const response = await callBackendApi('/eggs');
        if (!response.ok) {
            throw new Error("Failed to fetch today's goals");
        }
        return response.json();
    }

    /**
     * Create or update today's goals
     */
    static async createOrUpdateGoals(goals: string[]): Promise<Goal[]> {
        const response = await callBackendApi('/eggs', {
            method: 'POST',
            body: { goals },
        });
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(
                errorData.error?.message || 'Failed to create/update goals'
            );
        }
        return response.json();
    }

    /**
     * Complete or uncomplete a goal at the specified index
     */
    static async completeGoal(
        index: number,
        completed: boolean
    ): Promise<Goal[]> {
        const response = await callBackendApi(`/eggs/${index}/complete`, {
            method: 'POST',
            body: { completed },
        });
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({}));
            throw new Error(
                errorData.error?.message || 'Failed to complete goal'
            );
        }
        return response.json();
    }

    /**
     * Get completion streak (consecutive days with all goals completed)
     */
    static async getCompletionStreak(): Promise<number> {
        const response = await callBackendApi('/eggs/streak');
        if (!response.ok) {
            throw new Error('Failed to fetch completion streak');
        }
        const data = await response.json();
        return data.streak ?? 0;
    }

    /**
     * Check if user has ever created any goals (queries historical data)
     */
    static async hasEverCreatedGoals(): Promise<boolean> {
        const response = await callBackendApi('/eggs/has-ever-created');
        if (!response.ok) {
            throw new Error('Failed to check goal history');
        }
        const data = await response.json();
        return data.hasEverCreated ?? false;
    }
}
