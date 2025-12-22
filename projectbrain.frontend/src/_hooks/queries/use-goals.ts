import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/_lib/api-client';
import { Goal } from '@/_services/goal-service';

export const goalKeys = {
    all: ['goals'] as const,
    todays: () => [...goalKeys.all, 'todays'] as const,
    streak: () => [...goalKeys.all, 'streak'] as const,
    hasEverCreated: () => [...goalKeys.all, 'hasEverCreated'] as const,
};

export function useTodaysGoals() {
    return useQuery({
        queryKey: goalKeys.todays(),
        queryFn: async () => {
            const response = await apiClient<Goal[]>('/api/goals/eggs');
            return response;
        },
    });
}

export function useCreateOrUpdateGoals() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async (goals: string[]) => {
            const response = await apiClient<Goal[]>('/api/goals/eggs', {
                method: 'POST',
                body: { goals },
            });
            return response;
        },
        onSuccess: () => {
            // Invalidate and refetch today's goals
            queryClient.invalidateQueries({ queryKey: goalKeys.todays() });
            // Also invalidate hasEverCreated since creating goals changes this
            queryClient.invalidateQueries({
                queryKey: goalKeys.hasEverCreated(),
            });
            // Invalidate streak as well since new goals might affect it
            queryClient.invalidateQueries({ queryKey: goalKeys.streak() });
        },
    });
}

export function useCompleteGoal() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async ({
            index,
            completed,
        }: {
            index: number;
            completed: boolean;
        }) => {
            const response = await apiClient<Goal[]>(
                `/api/goals/eggs/${index}/complete`,
                { method: 'POST', body: { completed } }
            );
            return response;
        },
        onSuccess: () => {
            // Invalidate and refetch today's goals and streak
            queryClient.invalidateQueries({ queryKey: goalKeys.todays() });
            queryClient.invalidateQueries({ queryKey: goalKeys.streak() });
        },
    });
}

export function useCompletionStreak() {
    return useQuery({
        queryKey: goalKeys.streak(),
        queryFn: async () => {
            const response = await apiClient<{ streak: number }>(
                '/api/goals/eggs/streak'
            );
            return response.streak;
        },
        staleTime: 5 * 60 * 1000, // 5 minutes - streak doesn't change frequently
    });
}

export function useHasEverCreatedGoals() {
    return useQuery({
        queryKey: goalKeys.hasEverCreated(),
        queryFn: async () => {
            const response = await apiClient<{ hasEverCreated: boolean }>(
                '/api/goals/eggs/has-ever-created'
            );
            return response.hasEverCreated;
        },
        staleTime: 10 * 60 * 1000, // 10 minutes - this rarely changes
    });
}
