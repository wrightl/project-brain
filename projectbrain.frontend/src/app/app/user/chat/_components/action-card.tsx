'use client';

import Link from 'next/link';
import { ActionCard } from '@/_lib/types';

interface ActionCardWidgetProps {
    card: ActionCard;
}

export default function ActionCardWidget({ card }: ActionCardWidgetProps) {
    const title = getCardTitle(card);

    return (
        <div className="mt-2 border border-indigo-200 rounded-lg bg-indigo-50 px-3 py-2">
            <p className="text-sm font-medium text-indigo-900">{title}</p>

            {card.cardType === 'goals_created' && card.goals && (
                <ul className="mt-2 space-y-1 text-xs text-indigo-800">
                    {card.goals.map((goal) => (
                        <li key={goal.index}>
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

            {card.href && card.label && (
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
        case 'strategy_saved':
            return card.title ? `Strategy saved: ${card.title}` : 'Strategy saved';
        case 'coaches_found':
            return 'Coaches found';
        case 'document_uploaded':
            return 'Document uploaded';
        default:
            return card.title ?? 'Action completed';
    }
}
