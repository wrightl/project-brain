'use client';

import Link from 'next/link';
import { useState } from 'react';
import { ActionCard } from '@/_lib/types';

interface ActionCardWidgetProps {
    card: ActionCard;
    onConfirmPendingAction?: (card: ActionCard) => Promise<void>;
    onCancelPendingAction?: (card: ActionCard) => Promise<void>;
}

export default function ActionCardWidget({
    card,
    onConfirmPendingAction,
    onCancelPendingAction,
}: ActionCardWidgetProps) {
    const [isSubmitting, setIsSubmitting] = useState(false);
    const title = getCardTitle(card);

    const handleConfirm = async () => {
        if (!onConfirmPendingAction || isSubmitting) return;
        setIsSubmitting(true);
        try {
            await onConfirmPendingAction(card);
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleCancel = async () => {
        if (!onCancelPendingAction || isSubmitting) return;
        setIsSubmitting(true);
        try {
            await onCancelPendingAction(card);
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="mt-2 border border-indigo-200 rounded-lg bg-indigo-50 px-3 py-2">
            <p className="text-sm font-medium text-indigo-900">{title}</p>

            {card.cardType === 'pending_confirmation' && card.preview && (
                <p className="mt-1 text-xs text-indigo-700">{card.preview}</p>
            )}

            {card.cardType === 'goals_created' && card.goals && (
                <ul className="mt-2 space-y-1 text-xs text-indigo-800">
                    {card.goals.map((goal, index) => (
                        <li key={goal.index ?? index}>
                            {goal.completed ? '✓ ' : '○ '}
                            {goal.message}
                        </li>
                    ))}
                </ul>
            )}

            {card.cardType === 'goals_created' && card.days && !card.goals && (
                <ul className="mt-2 space-y-1 text-xs text-indigo-800">
                    {card.days.map((day) => (
                        <li key={day.date}>
                            {day.date}: {day.goalCount ?? 0} goal{(day.goalCount ?? 0) === 1 ? '' : 's'}
                        </li>
                    ))}
                </ul>
            )}

            {card.cardType === 'goals_suggested' && card.goals && (
                <ul className="mt-2 space-y-1 text-xs text-indigo-800">
                    {card.goals.map((goal, index) => (
                        <li key={index}>○ {goal.message}</li>
                    ))}
                </ul>
            )}

            {card.cardType === 'goal_streak' && (
                <p className="mt-1 text-xs text-indigo-700">
                    Current streak: {card.currentStreak ?? 0} day{(card.currentStreak ?? 0) === 1 ? '' : 's'}
                    {card.longestStreak !== undefined
                        ? ` · Longest: ${card.longestStreak} day${card.longestStreak === 1 ? '' : 's'}`
                        : ''}
                </p>
            )}

            {card.cardType === 'strategy_saved' && card.description && (
                <p className="mt-1 text-xs text-indigo-700">{card.description}</p>
            )}

            {card.cardType === 'coaches_found' && card.coaches && (
                <ul className="mt-2 space-y-2">
                    {card.coaches.slice(0, 3).map((coach) => (
                        <li key={coach.coachProfileId ?? coach.name} className="text-xs text-indigo-800">
                            <span className="font-medium">{coach.name}</span>
                            {coach.bio ? ` — ${coach.bio}` : ''}
                        </li>
                    ))}
                </ul>
            )}

            {card.cardType === 'document_uploaded' && card.filename && (
                <p className="mt-1 text-xs text-indigo-700">{card.filename}</p>
            )}

            {card.cardType === 'journal_entry_created' && card.summary && (
                <p className="mt-1 text-xs text-indigo-700">{card.summary}</p>
            )}

            {(card.cardType === 'memory_saved' || card.cardType === 'memory_deleted') &&
                (card.description || card.message) && (
                    <p className="mt-1 text-xs text-indigo-700">
                        {card.description ?? card.message}
                    </p>
                )}

            {card.cardType === 'document_deleted' && card.message && (
                <p className="mt-1 text-xs text-indigo-700">{card.message}</p>
            )}

            {card.cardType === 'pending_confirmation' && (
                <div className="mt-3 flex gap-2">
                    <button
                        type="button"
                        onClick={handleConfirm}
                        disabled={isSubmitting}
                        className="rounded-md bg-indigo-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
                    >
                        {isSubmitting ? 'Confirming...' : 'Confirm'}
                    </button>
                    <button
                        type="button"
                        onClick={handleCancel}
                        disabled={isSubmitting}
                        className="rounded-md border border-gray-300 bg-white px-3 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-100 disabled:opacity-50"
                    >
                        Cancel
                    </button>
                </div>
            )}

            {card.href && card.label && card.cardType !== 'pending_confirmation' && (
                <Link
                    href={card.href}
                    className="mt-2 inline-block text-xs text-indigo-600 hover:text-indigo-800 underline font-medium"
                >
                    {card.label} →
                </Link>
            )}
        </div>
    );
}

function getCardTitle(card: ActionCard): string {
    switch (card.cardType) {
        case 'goals_created':
            return card.days?.length
                ? `Goals planned for ${card.days.length} day${card.days.length === 1 ? '' : 's'}`
                : 'Daily goals created';
        case 'goals_suggested':
            return 'Suggested daily goals';
        case 'goal_streak':
            return 'Goal streak';
        case 'strategy_saved':
            return card.title ? `Strategy saved: ${card.title}` : 'Strategy saved';
        case 'coaches_found':
            return 'Coaches found';
        case 'document_uploaded':
            return 'Document uploaded';
        case 'document_deleted':
            return 'Document deleted';
        case 'journal_entry_created':
            return 'Journal entry created';
        case 'memory_saved':
            return card.title ? `Remembered: ${card.title}` : 'Memory saved';
        case 'memory_deleted':
            return 'Memory forgotten';
        case 'pending_confirmation':
            return 'Confirm this action';
        default:
            return card.title ?? 'Action completed';
    }
}
