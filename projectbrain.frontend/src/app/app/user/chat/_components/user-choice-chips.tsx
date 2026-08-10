'use client';

import { UserChoiceOption, UserChoicePrompt } from '@/_lib/types';

interface UserChoiceChipsProps {
    choices: UserChoicePrompt;
    disabled?: boolean;
    onSelect: (option: UserChoiceOption) => void;
}

export default function UserChoiceChips({
    choices,
    disabled = false,
    onSelect,
}: UserChoiceChipsProps) {
    if (!choices.options.length) {
        return null;
    }

    if (choices.allowMultiple && process.env.NODE_ENV === 'development') {
        console.warn(
            'user_choices allowMultiple is not supported in the UI yet; showing single-select chips.',
        );
    }

    return (
        <div className="mt-2 space-y-2">
            {choices.prompt && (
                <p className="text-xs font-medium text-gray-600">
                    {choices.prompt}
                </p>
            )}
            <div className="flex flex-wrap gap-2">
                {choices.options.map((option) => (
                    <button
                        key={option.id}
                        type="button"
                        disabled={disabled}
                        onClick={() => onSelect(option)}
                        className="inline-flex max-w-full items-center rounded-full border border-gray-300 bg-white px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-50"
                    >
                        {option.label}
                    </button>
                ))}
            </div>
        </div>
    );
}
