export interface Auth0Resource {
    path: string;
    label: string;
}

export type UserRole = 'admin' | 'coach' | 'user';

export interface BaseUser {
    id: string; // TODO: Might need to remove this
    email: string;
    fullName: string;
    firstName?: string;
    isOnboarded: boolean;
    roles: UserRole[];
    lastActivityAt?: string;
    // Profile fields
    doB?: string;
    preferredPronoun?: string;
    neurodiverseTraits?: string[];
    preferences?: string;
    // Address fields
    streetAddress?: string;
    addressLine2?: string;
    city?: string;
    stateProvince?: string;
    postalCode?: string;
    country?: string;
}

export interface User extends BaseUser {
    userProfileId: string;
}

export interface Coach extends BaseUser {
    coachProfileId: string;
    qualifications: string[];
    specialisms: string[];
    ageGroups: string[];
    availabilityStatus?: 'Available' | 'Busy' | 'Away' | 'Offline';
    averageRating?: number;
    ratingCount?: number;
}

export interface OnboardingData {
    email: string;
    fullName: string;
    // role: UserRole;
    // Address fields
    streetAddress?: string;
    addressLine2?: string;
    city?: string;
    stateProvince?: string;
    postalCode?: string;
    country?: string;
}

export interface CoachOnboardingData extends OnboardingData {
    qualifications?: string[];
    specialisms?: string[];
    ageGroups?: string[];
}

export interface WelcomeSection {
    preferredName?: string;
    inspiration?: string;
    currentFeeling?: string;
}

export interface AboutYouSection {
    selfDescription?: string[];
    businessType?: string;
    proudMoment?: string;
    challenge?: string[];
}

export interface PreferencesSection {
    learningStyle?: string;
    informationDepth?: string;
    celebrationStyle?: string;
}

export interface ProfileSection {
    strengths?: string[];
    supportAreas?: string[];
    motivationStyle?: string;
    neurodivergentUnderstanding?: string;
    biggestGoal?: string;
}

export interface CoachingBuddySection {
    tasks?: string[];
    communicationStyle?: string;
    toolsIntegration?: string;
    workingStyle?: string;
    additionalInfo?: string;
}

export interface ClosingSection {
    safeSpace?: string;
    tipsOptIn?: boolean;
}

export interface FollowOnQuestions {
    strengths?: {
        howUseStrengths?: string;
        tapIntoStrengths?: string;
        buildOnStrengths?: string;
    };
    challenges?: {
        hardestToManage?: string;
        toolsThatHelp?: string;
        suggestions?: boolean;
        recharge?: string;
    };
    learning?: {
        learningExample?: string;
        preferredFormat?: boolean;
        breakTasks?: string;
    };
    motivation?: {
        whatMotivates?: string;
        goalSetting?: string;
        reminders?: string;
        celebrateProgress?: string;
    };
    coping?: {
        sensoryFriendly?: string;
        timeManagement?: string;
        overwhelmed?: string;
        exploreStrategies?: boolean;
    };
    support?: {
        biggestDifference?: string;
        supportSystem?: string;
        specificSkills?: string;
        selfCareBalance?: string;
    };
    coachingBuddy?: {
        taskToTakeOff?: string;
        helpWith?: string;
        adaptCommunication?: string;
        specificReminders?: string;
    };
    emotional?: {
        feelGrounded?: string;
        processChallenges?: string;
        buildCalm?: string;
        feelSupported?: string;
    };
    celebrating?: {
        recentWin?: string;
        acknowledgeProgress?: string;
        celebrationIdeas?: boolean;
        helpRecognize?: string;
    };
    customization?: {
        specificTools?: string;
        customizeCommunication?: string;
        tailoredNeeds?: string;
    };
}

export interface UserOnboardingData extends OnboardingData {
    doB: string;
    preferredPronoun: string;
    neurodiverseTraits?: string[];
    preferences?: string;
    // New structured onboarding fields
    onboarding?: {
        locale?: string;
        welcome?: WelcomeSection;
        aboutYou?: AboutYouSection;
        preferences?: PreferencesSection;
        profile?: ProfileSection;
        coachingBuddy?: CoachingBuddySection;
        closing?: ClosingSection;
        followOnQuestions?: FollowOnQuestions;
    };
}

export interface Citation {
    id: string;
    index: number;
    sourceFile: string;
    sourcePage?: string;
    storageUrl: string;
    isShared: boolean;
}

export interface ChatMessage {
    role: 'user' | 'assistant';
    content: string;
    createdAt?: string;
    citations?: Citation[];
}

export interface Conversation {
    id: string;
    userId: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    messages?: ChatMessage[];
}

export interface ChatRequest {
    conversationId?: string;
    content: string;
}

export interface ChatStreamChunk {
    type: 'text';
    value: string;
}

export interface UploadResult {
    status: 'uploaded' | 'error';
    filename: string;
    fileSize?: number;
    message?: string;
    location?: string;
}

export interface Resource {
    id: string;
    fileName: string;
    location: string;
    sizeInBytes: number;
    createdAt: string;
    updatedAt: string;
}

export interface ReindexResult {
    status: 'success' | 'error';
    filesReindexed: number;
}

// Subscription types
export interface Subscription {
    id?: string;
    tier: string;
    status: string;
    trialEndsAt?: string;
    currentPeriodStart?: string;
    currentPeriodEnd?: string;
    canceledAt?: string;
    userType: string;
}

export interface Usage {
    aiQueries: {
        daily: number;
        monthly: number;
    };
    coachMessages: {
        monthly: number;
    };
    clientMessages: {
        monthly: number;
    };
    fileStorage: {
        bytes: number;
        megabytes: number;
    };
    researchReports: {
        monthly: number;
    };
}

// Voice note types
export interface VoiceNote {
    id: string;
    fileName: string;
    audioUrl: string;
    duration: number | null;
    fileSize: number | null;
    description: string | null;
    createdAt: string;
    updatedAt: string;
}

export interface PagedResponse<T> {
    items: T[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
    hasPreviousPage: boolean;
    hasNextPage: boolean;
}

// Connection types
export interface Connection {
    id: string;
    userId: string;
    coachId: string;
    status: 'pending' | 'accepted' | 'cancelled' | 'rejected';
    userName?: string;
    coachName?: string;
    coachProfileId?: string;
    requestedAt: string;
    respondedAt?: string;
}

// Coach search types
export interface CoachSearchParams {
    city?: string;
    stateProvince?: string;
    country?: string;
    ageGroups?: string[];
    specialisms?: string[];
}

export interface ClientWithConnectionStatus {
    user: User;
    connectionStatus: 'pending' | 'accepted';
    requestedAt: string;
    requestedBy: 'user' | 'coach';
    message?: string;
}
