/**
 * Question configuration for the onboarding wizard
 * Defines which selections trigger which follow-on questions
 */

export interface FollowOnTrigger {
    section: string;
    field: string;
    value: string | string[];
    followOnCategory: string;
}

export const FOLLOW_ON_TRIGGERS: FollowOnTrigger[] = [
    // Strengths trigger
    { section: 'profile', field: 'strengths', value: [], followOnCategory: 'strengths' },
    
    // Challenges trigger
    { section: 'aboutYou', field: 'challenge', value: [], followOnCategory: 'challenges' },
    { section: 'profile', field: 'supportAreas', value: [], followOnCategory: 'challenges' },
    
    // Learning preferences trigger
    { section: 'preferences', field: 'learningStyle', value: '', followOnCategory: 'learning' },
    
    // Motivation trigger
    { section: 'profile', field: 'motivationStyle', value: '', followOnCategory: 'motivation' },
    
    // Coping strategies trigger (if challenge includes organization, focus, or energy)
    { section: 'aboutYou', field: 'challenge', value: ['organization', 'focus', 'energy-management'], followOnCategory: 'coping' },
    
    // Support needs trigger
    { section: 'profile', field: 'supportAreas', value: [], followOnCategory: 'support' },
    
    // Coaching buddy trigger
    { section: 'coachingBuddy', field: 'tasks', value: [], followOnCategory: 'coachingBuddy' },
    
    // Emotional well-being trigger (if current feeling is overwhelmed, stuck, or uncertain)
    { section: 'welcome', field: 'currentFeeling', value: ['overwhelmed', 'stuck', 'uncertain'], followOnCategory: 'emotional' },
    
    // Celebrating wins trigger
    { section: 'preferences', field: 'celebrationStyle', value: '', followOnCategory: 'celebrating' },
    
    // Customization trigger
    { section: 'coachingBuddy', field: 'toolsIntegration', value: '', followOnCategory: 'customization' },
];

/**
 * Check if a follow-on category should be shown based on form data
 */
export function shouldShowFollowOn(
    category: string,
    formData: Record<string, any>
): boolean {
    const triggers = FOLLOW_ON_TRIGGERS.filter(
        trigger => trigger.followOnCategory === category
    );
    
    for (const trigger of triggers) {
        const sectionData = formData[trigger.section];
        if (!sectionData) continue;
        
        const fieldValue = sectionData[trigger.field];
        if (!fieldValue) continue;
        
        // Handle different combinations of trigger value and field value
        if (Array.isArray(trigger.value)) {
            if (trigger.value.length === 0) {
                // Empty array means "if field has any value"
                if (Array.isArray(fieldValue) && fieldValue.length > 0) return true;
                if (!Array.isArray(fieldValue) && fieldValue) return true;
            } else {
                // Non-empty array means "if any of these values are in field"
                if (Array.isArray(fieldValue)) {
                    // Check if any trigger value is in field array
                    if (trigger.value.some(val => fieldValue.includes(val))) return true;
                } else {
                    // Field is single value, check if it's in trigger array
                    if (trigger.value.includes(fieldValue)) return true;
                }
            }
        } else {
            // Trigger is single value (string)
            if (trigger.value === '') {
                // Empty string means "if field has any value"
                if (fieldValue) return true;
            } else {
                // Check exact match
                if (Array.isArray(fieldValue)) {
                    if (fieldValue.includes(trigger.value)) return true;
                } else {
                    if (fieldValue === trigger.value) return true;
                }
            }
        }
    }
    
    return false;
}

/**
 * Get all follow-on categories that should be shown
 */
export function getFollowOnCategories(formData: Record<string, any>): string[] {
    const categories = new Set<string>();
    
    for (const trigger of FOLLOW_ON_TRIGGERS) {
        if (shouldShowFollowOn(trigger.followOnCategory, formData)) {
            categories.add(trigger.followOnCategory);
        }
    }
    
    return Array.from(categories);
}

