export interface UserFactMemory {
    id: string;
    content: string;
    category: string;
    status: string;
    createdAt: string;
    isPinned: boolean;
}

export interface UserEpisodeMemory {
    id: string;
    summary: string;
    topic: string;
    outcome: string;
    status: string;
    createdAt: string;
    isPinned: boolean;
}

export interface UserMemoryList {
    facts: UserFactMemory[];
    episodes: UserEpisodeMemory[];
}
