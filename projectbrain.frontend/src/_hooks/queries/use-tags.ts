import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '@/_lib/api-client';
import { Tag, CreateTagRequest } from '@/_services/tag-service';

export const tagKeys = {
    all: ['tags'] as const,
    lists: () => [...tagKeys.all, 'list'] as const,
    details: () => [...tagKeys.all, 'detail'] as const,
    detail: (id: string) => [...tagKeys.details(), id] as const,
    byName: (name: string) => [...tagKeys.all, 'name', name] as const,
};

export function useTags() {
    return useQuery<Tag[]>({
        queryKey: tagKeys.lists(),
        queryFn: () => apiClient<Tag[]>('/api/user/tag'),
        staleTime: 5 * 60 * 1000, // 5 minutes
    });
}

export function useTag(id: string) {
    return useQuery<Tag>({
        queryKey: tagKeys.detail(id),
        queryFn: () => apiClient<Tag>(`/api/user/tag/${id}`),
        enabled: !!id,
        staleTime: 5 * 60 * 1000, // 5 minutes
    });
}

export function useCreateTag() {
    const queryClient = useQueryClient();

    return useMutation<Tag, Error, CreateTagRequest>({
        mutationFn: (request: CreateTagRequest) => {
            const { fetchWithAuth } = require('@/_lib/fetch-with-auth');
            return fetchWithAuth('/api/user/tag', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(request),
            }).then(async (response: Response) => {
                if (!response.ok) {
                    const errorText = await response.text();
                    throw new Error(errorText || 'Failed to create tag');
                }
                return response.json() as Promise<Tag>;
            });
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: tagKeys.all });
        },
    });
}

export function useGetOrCreateTag() {
    const queryClient = useQueryClient();
    const createTag = useCreateTag();

    return useMutation({
        mutationFn: async (name: string): Promise<Tag> => {
            // First try to get existing tag
            try {
                const { fetchWithAuth } = await import(
                    '@/_lib/fetch-with-auth'
                );
                const response = await fetchWithAuth(
                    `/api/user/tag/name/${encodeURIComponent(name)}`
                );
                if (response.ok) {
                    const tag = await response.json();
                    if (tag) {
                        return tag;
                    }
                }
            } catch (error) {
                // Tag doesn't exist, continue to create it
            }

            // Create new tag
            const newTag = await createTag.mutateAsync({ name });
            if (!newTag) {
                throw new Error('Failed to create tag');
            }
            return newTag;
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: tagKeys.all });
        },
    });
}
