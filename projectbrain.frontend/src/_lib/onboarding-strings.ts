/**
 * Localized strings for the onboarding wizard
 * Supports UK English (en-GB) and US English (en-US)
 */

import type { SupportedLocale } from './locale';

interface OnboardingStrings {
    welcome: {
        title: string;
        description: string;
        preferredNameLabel: string;
        preferredNamePlaceholder: string;
        preferredNameHint: string;
        inspirationLabel: string;
        inspirationPlaceholder: string;
        currentFeelingLabel: string;
        currentFeelingOptions: { value: string; label: string }[];
    };
    aboutYou: {
        title: string;
        description: string;
        selfDescriptionLabel: string;
        selfDescriptionOptions: { value: string; label: string }[];
        businessTypeLabel: string;
        businessTypePlaceholder: string;
        proudMomentLabel: string;
        proudMomentPlaceholder: string;
        challengeLabel: string;
        challengeOptions: { value: string; label: string }[];
    };
    preferences: {
        title: string;
        description: string;
        learningStyleLabel: string;
        learningStyleOptions: { value: string; label: string }[];
        informationDepthLabel: string;
        informationDepthOptions: { value: string; label: string }[];
        celebrationStyleLabel: string;
        celebrationStyleOptions: { value: string; label: string }[];
    };
    profile: {
        title: string;
        description: string;
        strengthsLabel: string;
        strengthsOptions: { value: string; label: string }[];
        supportAreasLabel: string;
        supportAreasOptions: { value: string; label: string }[];
        motivationStyleLabel: string;
        motivationStyleOptions: { value: string; label: string }[];
        neurodivergentUnderstandingLabel: string;
        neurodivergentUnderstandingPlaceholder: string;
        biggestGoalLabel: string;
        biggestGoalPlaceholder: string;
    };
    coachingBuddy: {
        title: string;
        description: string;
        tasksLabel: string;
        tasksOptions: { value: string; label: string }[];
        communicationStyleLabel: string;
        communicationStyleOptions: { value: string; label: string }[];
        toolsIntegrationLabel: string;
        toolsIntegrationPlaceholder: string;
        workingStyleLabel: string;
        workingStylePlaceholder: string;
        additionalInfoLabel: string;
        additionalInfoPlaceholder: string;
    };
    closing: {
        title: string;
        description: string;
        safeSpaceLabel: string;
        safeSpacePlaceholder: string;
        tipsOptInLabel: string;
    };
    followOn: {
        strengths: {
            title: string;
            questions: {
                howUseStrengthsLabel: string;
                howUseStrengthsPlaceholder: string;
                tapIntoStrengthsLabel: string;
                tapIntoStrengthsPlaceholder: string;
                buildOnStrengthsLabel: string;
                buildOnStrengthsPlaceholder: string;
            };
        };
        challenges: {
            title: string;
            questions: {
                hardestToManageLabel: string;
                hardestToManagePlaceholder: string;
                toolsThatHelpLabel: string;
                toolsThatHelpPlaceholder: string;
                suggestionsLabel: string;
                rechargeLabel: string;
                rechargePlaceholder: string;
            };
        };
        learning: {
            title: string;
            questions: {
                learningExampleLabel: string;
                learningExamplePlaceholder: string;
                preferredFormatLabel: string;
                breakTasksLabel: string;
                breakTasksOptions: { value: string; label: string }[];
            };
        };
        motivation: {
            title: string;
            questions: {
                whatMotivatesLabel: string;
                whatMotivatesPlaceholder: string;
                goalSettingLabel: string;
                goalSettingOptions: { value: string; label: string }[];
                remindersLabel: string;
                remindersPlaceholder: string;
                celebrateProgressLabel: string;
                celebrateProgressPlaceholder: string;
            };
        };
        coping: {
            title: string;
            questions: {
                sensoryFriendlyLabel: string;
                sensoryFriendlyPlaceholder: string;
                timeManagementLabel: string;
                timeManagementPlaceholder: string;
                overwhelmedLabel: string;
                overwhelmedPlaceholder: string;
                exploreStrategiesLabel: string;
            };
        };
        support: {
            title: string;
            questions: {
                biggestDifferenceLabel: string;
                biggestDifferencePlaceholder: string;
                supportSystemLabel: string;
                supportSystemPlaceholder: string;
                specificSkillsLabel: string;
                specificSkillsPlaceholder: string;
                selfCareBalanceLabel: string;
                selfCareBalancePlaceholder: string;
            };
        };
        coachingBuddy: {
            title: string;
            questions: {
                taskToTakeOffLabel: string;
                taskToTakeOffPlaceholder: string;
                helpWithLabel: string;
                helpWithPlaceholder: string;
                adaptCommunicationLabel: string;
                adaptCommunicationPlaceholder: string;
                specificRemindersLabel: string;
                specificRemindersPlaceholder: string;
            };
        };
        emotional: {
            title: string;
            questions: {
                feelGroundedLabel: string;
                feelGroundedPlaceholder: string;
                processChallengesLabel: string;
                processChallengesOptions: { value: string; label: string }[];
                buildCalmLabel: string;
                buildCalmPlaceholder: string;
                feelSupportedLabel: string;
                feelSupportedPlaceholder: string;
            };
        };
        celebrating: {
            title: string;
            questions: {
                recentWinLabel: string;
                recentWinPlaceholder: string;
                acknowledgeProgressLabel: string;
                acknowledgeProgressPlaceholder: string;
                celebrationIdeasLabel: string;
                helpRecognizeLabel: string;
                helpRecognizePlaceholder: string;
            };
        };
        customization: {
            title: string;
            questions: {
                specificToolsLabel: string;
                specificToolsPlaceholder: string;
                customizeCommunicationLabel: string;
                customizeCommunicationPlaceholder: string;
                tailoredNeedsLabel: string;
                tailoredNeedsPlaceholder: string;
            };
        };
    };
    common: {
        optional: string;
        required: string;
        next: string;
        previous: string;
        complete: string;
        skip: string;
        selectPlaceholder: string;
    };
}

const enUSStrings: OnboardingStrings = {
    welcome: {
        title: 'Welcome',
        description: "Let's start by getting to know you",
        preferredNameLabel:
            "What's your name or what would you like us to call you?",
        preferredNamePlaceholder: 'Enter your preferred name',
        preferredNameHint:
            '(Optional: You can skip this if you prefer to stay anonymous.)',
        inspirationLabel: 'What inspired you to download this app?',
        inspirationPlaceholder:
            'E.g., looking for support, curious about the app, wanting to grow your business, etc.',
        currentFeelingLabel:
            'How are you feeling about your entrepreneurial journey right now?',
        currentFeelingOptions: [
            { value: 'excited', label: 'Excited' },
            { value: 'overwhelmed', label: 'Overwhelmed' },
            { value: 'curious', label: 'Curious' },
            { value: 'stuck', label: 'Stuck' },
            { value: 'motivated', label: 'Motivated' },
            { value: 'uncertain', label: 'Uncertain' },
            { value: 'other', label: 'Other' },
        ],
    },
    aboutYou: {
        title: 'About You',
        description: 'Tell us more about yourself as an entrepreneur',
        selfDescriptionLabel:
            'How do you describe yourself as an entrepreneur?',
        selfDescriptionOptions: [
            { value: 'creative', label: 'Creative' },
            { value: 'problem-solver', label: 'Problem-solver' },
            { value: 'big-picture-thinker', label: 'Big-picture thinker' },
            { value: 'detail-oriented', label: 'Detail-oriented' },
            { value: 'innovative', label: 'Innovative' },
            { value: 'analytical', label: 'Analytical' },
            { value: 'strategic', label: 'Strategic' },
        ],
        businessTypeLabel:
            'What kind of business or project are you working on (or dreaming of starting)?',
        businessTypePlaceholder:
            'Feel free to share as much or as little as you like.',
        proudMomentLabel:
            "What's one thing you're really proud of in your entrepreneurial journey so far?",
        proudMomentPlaceholder: 'Big or small, it all matters!',
        challengeLabel:
            "What's one thing you find challenging about being an entrepreneur?",
        challengeOptions: [
            { value: 'organization', label: 'Staying organized' },
            { value: 'energy-management', label: 'Managing energy' },
            { value: 'finding-support', label: 'Finding support' },
            { value: 'time-management', label: 'Time management' },
            { value: 'focus', label: 'Staying focused' },
            { value: 'marketing', label: 'Marketing' },
            { value: 'networking', label: 'Networking' },
            { value: 'self-care', label: 'Self-care' },
            { value: 'other', label: 'Other' },
        ],
    },
    preferences: {
        title: 'Your Preferences',
        description: 'Help us understand how you like to work and learn',
        learningStyleLabel:
            "What's the best way for you to learn or process information?",
        learningStyleOptions: [
            { value: 'step-by-step', label: 'Step-by-step instructions' },
            { value: 'visuals', label: 'Visuals' },
            { value: 'videos', label: 'Videos' },
            { value: 'hands-on', label: 'Hands-on practice' },
            { value: 'written', label: 'Written materials' },
            { value: 'audio', label: 'Audio/podcasts' },
            { value: 'combination', label: 'Combination' },
        ],
        informationDepthLabel:
            'Do you prefer short bursts of information or more detailed explanations?',
        informationDepthOptions: [
            { value: 'short', label: 'Keep it short and simple' },
            { value: 'detailed', label: 'I like all the details' },
            { value: 'flexible', label: 'Flexible, depends on context' },
        ],
        celebrationStyleLabel: 'How do you like to celebrate your wins?',
        celebrationStyleOptions: [
            { value: 'break', label: 'Taking a break' },
            { value: 'sharing', label: 'Sharing with friends' },
            { value: 'treat', label: 'Treating yourself' },
            { value: 'quiet', label: 'Quiet reflection' },
            { value: 'other', label: 'Other' },
        ],
    },
    profile: {
        title: 'Building Your Profile',
        description: 'Tell us about your strengths and goals',
        strengthsLabel: 'What are your top strengths as an entrepreneur?',
        strengthsOptions: [
            { value: 'creativity', label: 'Creativity' },
            { value: 'resilience', label: 'Resilience' },
            { value: 'problem-solving', label: 'Problem-solving' },
            { value: 'connecting-people', label: 'Connecting with people' },
            { value: 'innovation', label: 'Innovation' },
            { value: 'adaptability', label: 'Adaptability' },
            { value: 'persistence', label: 'Persistence' },
        ],
        supportAreasLabel:
            "What are some areas where you'd like more support or tools?",
        supportAreasOptions: [
            { value: 'time-management', label: 'Time management' },
            { value: 'marketing', label: 'Marketing' },
            { value: 'networking', label: 'Networking' },
            { value: 'self-care', label: 'Self-care' },
            { value: 'organization', label: 'Organization' },
            { value: 'planning', label: 'Planning' },
            { value: 'communication', label: 'Communication' },
        ],
        motivationStyleLabel: "What's your preferred way to stay motivated?",
        motivationStyleOptions: [
            { value: 'small-goals', label: 'Setting small goals' },
            { value: 'reminders', label: 'Reminders' },
            { value: 'accountability', label: 'Accountability' },
            { value: 'rewards', label: 'Rewards' },
            { value: 'visual-progress', label: 'Visual progress tracking' },
        ],
        neurodivergentUnderstandingLabel:
            "What's one thing you wish people understood about your experience as a neurodivergent entrepreneur?",
        neurodivergentUnderstandingPlaceholder:
            'Share anything that feels important to you',
        biggestGoalLabel:
            "What's your biggest goal or dream for your business right now?",
        biggestGoalPlaceholder: "Tell us what you're working towards",
    },
    coachingBuddy: {
        title: 'Training Your Personal Coaching Buddy',
        description: 'Help us customize your AI coaching experience',
        tasksLabel:
            'What kind of tasks or challenges would you like your personal Coaching Buddy to help with?',
        tasksOptions: [
            { value: 'writing-emails', label: 'Writing emails' },
            { value: 'brainstorming', label: 'Brainstorming ideas' },
            { value: 'organizing-tasks', label: 'Organizing tasks' },
            { value: 'planning', label: 'Planning' },
            { value: 'problem-solving', label: 'Problem-solving' },
            { value: 'communication', label: 'Communication' },
            { value: 'research', label: 'Research' },
        ],
        communicationStyleLabel:
            "What's your preferred tone or style for communication?",
        communicationStyleOptions: [
            { value: 'friendly', label: 'Friendly' },
            { value: 'professional', label: 'Professional' },
            { value: 'casual', label: 'Casual' },
            { value: 'detailed', label: 'Detailed' },
            { value: 'concise', label: 'Concise' },
        ],
        toolsIntegrationLabel:
            "Are there any specific tools, apps, or systems you already use that you'd like your Coaching Buddy to integrate with?",
        toolsIntegrationPlaceholder: 'List any tools or apps',
        workingStyleLabel:
            "What's one thing you'd like your Coaching Buddy to understand about how you work best?",
        workingStylePlaceholder: 'Share what helps you work most effectively',
        additionalInfoLabel:
            "Is there anything else you'd like to share about yourself or your business?",
        additionalInfoPlaceholder:
            '(Optional: This is your space to share anything that feels important.)',
    },
    closing: {
        title: 'Almost There!',
        description: 'Just a few final questions',
        safeSpaceLabel:
            'How can we make this app feel like a safe and supportive space for you?',
        safeSpacePlaceholder: 'Share your thoughts',
        tipsOptInLabel:
            'Would you like to receive tips, encouragement, or resources tailored to your journey? (You can opt in or out anytime.)',
    },
    followOn: {
        strengths: {
            title: 'More About Your Strengths',
            questions: {
                howUseStrengthsLabel:
                    'How do you currently use your creativity/problem-solving skills in your business?',
                howUseStrengthsPlaceholder: 'Share examples',
                tapIntoStrengthsLabel:
                    "What helps you tap into your strengths when you're feeling stuck or overwhelmed?",
                tapIntoStrengthsPlaceholder: 'Tell us what works for you',
                buildOnStrengthsLabel:
                    'Are there ways we can help you build on these strengths to make your work feel easier or more enjoyable?',
                buildOnStrengthsPlaceholder: 'Share your ideas',
            },
        },
        challenges: {
            title: 'More About Your Challenges',
            questions: {
                hardestToManageLabel:
                    "What's one thing that feels hardest to manage in your day-to-day work?",
                hardestToManagePlaceholder: 'Tell us about it',
                toolsThatHelpLabel:
                    'Have you found any tools, routines, or strategies that help with this challenge?',
                toolsThatHelpPlaceholder: 'Share what works',
                suggestionsLabel:
                    'Would you like suggestions for coping strategies or tools that might work for you?',
                rechargeLabel:
                    "How do you usually recharge when you're feeling drained or overwhelmed?",
                rechargePlaceholder: 'Share your strategies',
            },
        },
        learning: {
            title: 'More About Your Learning Style',
            questions: {
                learningExampleLabel:
                    "What's an example of a time when you learned something in a way that really worked for you?",
                learningExamplePlaceholder: 'Share your experience',
                preferredFormatLabel:
                    'Would you like us to provide resources in your preferred format (e.g., visual guides, checklists, etc.)?',
                breakTasksLabel:
                    'Do you find it helpful to break tasks into smaller steps, or do you prefer to see the big picture first?',
                breakTasksOptions: [
                    { value: 'small-steps', label: 'Small steps' },
                    { value: 'big-picture', label: 'Big picture first' },
                    { value: 'both', label: 'Both, depends on the task' },
                ],
            },
        },
        motivation: {
            title: 'More About Your Motivation',
            questions: {
                whatMotivatesLabel:
                    "What's one thing that helps you feel motivated when you're working on a project?",
                whatMotivatesPlaceholder: 'Share what works',
                goalSettingLabel:
                    'Do you find it helpful to set small, achievable goals, or do you prefer to focus on the bigger picture?',
                goalSettingOptions: [
                    { value: 'small-goals', label: 'Small, achievable goals' },
                    { value: 'big-picture', label: 'Bigger picture' },
                    { value: 'both', label: 'Both' },
                ],
                remindersLabel:
                    'Would you like reminders or encouragement to help you stay on track? If so, what kind of reminders work best for you?',
                remindersPlaceholder: 'Share your preferences',
                celebrateProgressLabel:
                    "How do you celebrate progress, even if it's just a small step forward?",
                celebrateProgressPlaceholder:
                    'Tell us how you acknowledge wins',
            },
        },
        coping: {
            title: 'Coping Strategies',
            questions: {
                sensoryFriendlyLabel:
                    'Are there any sensory-friendly environments or tools that help you stay focused?',
                sensoryFriendlyPlaceholder: 'Share what helps',
                timeManagementLabel:
                    'Do you use any time management techniques, like timers, schedules, or task lists? If so, what works best for you?',
                timeManagementPlaceholder: 'Share your techniques',
                overwhelmedLabel:
                    "When you're feeling overwhelmed, what's one thing that helps you feel calmer or more in control?",
                overwhelmedPlaceholder: 'Share your strategies',
                exploreStrategiesLabel:
                    'Would you like to explore strategies like time-blocking, mindfulness, or creating a "low-energy" task list?',
            },
        },
        support: {
            title: 'Support Needs',
            questions: {
                biggestDifferenceLabel:
                    'What kind of support do you feel would make the biggest difference for you right now?',
                biggestDifferencePlaceholder: 'Share your thoughts',
                supportSystemLabel:
                    'Do you have a support system (e.g., friends, mentors, or a community) that you can lean on? Would you like help finding one?',
                supportSystemPlaceholder: 'Tell us about your support network',
                specificSkillsLabel:
                    "Are there specific skills or areas (e.g., marketing, networking) where you'd like more guidance or resources?",
                specificSkillsPlaceholder: "List areas you'd like help with",
                selfCareBalanceLabel:
                    'How do you balance self-care with running your business? Would you like tips for integrating self-care into your routine?',
                selfCareBalancePlaceholder: 'Share your approach',
            },
        },
        coachingBuddy: {
            title: 'More About Your Coaching Buddy',
            questions: {
                taskToTakeOffLabel:
                    "What's one task you'd love your Coaching Buddy to take off your plate?",
                taskToTakeOffPlaceholder: 'Share what would help most',
                helpWithLabel:
                    'Would you like your Coaching Buddy to help you brainstorm ideas, organize your thoughts, or create plans?',
                helpWithPlaceholder: "Tell us how you'd like help",
                adaptCommunicationLabel:
                    'How can your Coaching Buddy adapt to your communication style to make things easier for you?',
                adaptCommunicationPlaceholder: 'Share your preferences',
                specificRemindersLabel:
                    "Are there specific reminders or check-ins you'd like your Coaching Buddy to provide?",
                specificRemindersPlaceholder:
                    "List any reminders you'd find helpful",
            },
        },
        emotional: {
            title: 'Emotional Well-Being',
            questions: {
                feelGroundedLabel:
                    "What's one thing that helps you feel grounded when things feel overwhelming?",
                feelGroundedPlaceholder: 'Share what helps',
                processChallengesLabel:
                    'Do you find it helpful to talk through challenges, or do you prefer to process things on your own first?',
                processChallengesOptions: [
                    { value: 'talk-through', label: 'Talk through' },
                    { value: 'process-alone', label: 'Process alone first' },
                    { value: 'both', label: 'Both, depends on situation' },
                ],
                buildCalmLabel:
                    'Would you like to explore ways to build more calm or joy into your workday?',
                buildCalmPlaceholder: 'Share your thoughts',
                feelSupportedLabel:
                    'How can we help you feel supported and understood as you navigate your entrepreneurial journey?',
                feelSupportedPlaceholder: 'Tell us what would help',
            },
        },
        celebrating: {
            title: 'Celebrating Your Wins',
            questions: {
                recentWinLabel:
                    "What's a recent win (big or small) that you're proud of?",
                recentWinPlaceholder: 'Share your achievement',
                acknowledgeProgressLabel:
                    'How do you like to acknowledge your progress or achievements?',
                acknowledgeProgressPlaceholder: 'Tell us your approach',
                celebrationIdeasLabel:
                    'Would you like ideas for simple ways to celebrate your wins, even on busy days?',
                helpRecognizeLabel:
                    "How can we help you recognize and celebrate the progress you're making?",
                helpRecognizePlaceholder: 'Share your ideas',
            },
        },
        customization: {
            title: 'Customization',
            questions: {
                specificToolsLabel:
                    "Are there any specific tools or apps you'd like your Coaching Buddy to work with?",
                specificToolsPlaceholder: 'List tools or apps',
                customizeCommunicationLabel:
                    'Would you like to customize how your Coaching Buddy communicates with you (e.g., tone, frequency of check-ins)?',
                customizeCommunicationPlaceholder: 'Share your preferences',
                tailoredNeedsLabel:
                    "What's one thing we can do to make this app feel more tailored to your needs?",
                tailoredNeedsPlaceholder: 'Share your ideas',
            },
        },
    },
    common: {
        optional: '(Optional)',
        required: '*',
        next: 'Next',
        previous: 'Previous',
        complete: 'Complete Onboarding',
        skip: 'Skip',
        selectPlaceholder: 'Select an option...',
    },
};

const enGBStrings: OnboardingStrings = {
    ...enUSStrings,
    welcome: {
        ...enUSStrings.welcome,
    },
    aboutYou: {
        ...enUSStrings.aboutYou,
        challengeOptions: enUSStrings.aboutYou.challengeOptions.map((opt) => ({
            ...opt,
            label: opt.label
                .replace(/organized/gi, 'organised')
                .replace(/organization/gi, 'organisation'),
        })),
    },
    preferences: {
        ...enUSStrings.preferences,
    },
    profile: {
        ...enUSStrings.profile,
        supportAreasOptions: enUSStrings.profile.supportAreasOptions.map(
            (opt) => ({
                ...opt,
                label: opt.label.replace(/Organization/gi, 'Organisation'),
            })
        ),
    },
    coachingBuddy: {
        ...enUSStrings.coachingBuddy,
        tasksOptions: enUSStrings.coachingBuddy.tasksOptions.map((opt) => ({
            ...opt,
            label: opt.label
                .replace(/Organizing/gi, 'Organising')
                .replace(/organizing/gi, 'organising'),
        })),
    },
};

/**
 * Get localized strings for the onboarding wizard
 */
export function getOnboardingStrings(
    locale: SupportedLocale = 'en-US'
): OnboardingStrings {
    return locale === 'en-GB' ? enGBStrings : enUSStrings;
}
