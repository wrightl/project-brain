import { AppRouterInstance } from 'next/dist/shared/lib/app-router-context.shared-runtime';
import toast from 'react-hot-toast';
import { UserChoiceAction, UserChoiceOption } from '@/_lib/types';

export function parseCoachActionFromOptionId(
    optionId: string,
): UserChoiceAction | null {
    const viewMatch = optionId.match(/^view_profile:(.+)$/);
    if (viewMatch) {
        return {
            type: 'view_coach_profile',
            coachProfileId: viewMatch[1],
        };
    }

    const messageMatch = optionId.match(/^message_coach:(.+)$/);
    if (messageMatch) {
        return {
            type: 'message_coach',
            coachProfileId: messageMatch[1],
        };
    }

    return null;
}

export function resolveCoachAgentAction(
    option: UserChoiceOption,
): UserChoiceAction | null {
    if (option.action) {
        return option.action;
    }

    return parseCoachActionFromOptionId(option.id);
}

export function executeCoachAgentAction(
    router: AppRouterInstance,
    action: UserChoiceAction,
): boolean {
    if (action.type === 'view_coach_profile') {
        if (!action.coachProfileId) {
            return false;
        }

        router.push(`/app/user/coaches/${action.coachProfileId}`);
        return true;
    }

    if (action.type === 'message_coach') {
        if (action.connectionId) {
            router.push(`/app/user/messages/${action.connectionId}`);
            return true;
        }

        toast.error(
            'You need to be connected with this coach before you can message them.',
        );
        return true;
    }

    return false;
}
