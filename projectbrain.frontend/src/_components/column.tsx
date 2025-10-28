// export const Column = ({ title, cards, onDragStart, onDragOver, onDrop }) => {

import { PropsWithChildren } from 'react';
import { Card, CardProps } from './card';

// onDragOver={onDragOver} onDrop={onDrop}

export interface ColumnProps {
    title: string;
    cards: CardProps[];
}

export const Column: React.FC<PropsWithChildren<ColumnProps>> = ({
    title,
    cards,
    // onDragStart,
}) => {
    return (
        <div className="p-4 w-96 max-h-full">
            <div className="flex items-center justify-between mb-4">
                <h2 className="text-xl font-bold text-gray-800">{title}</h2>
                <span className="px-3 py-1 bg-gray-100 rounded-full text-sm font-medium text-gray-600">
                    {cards.length} tasks
                </span>
            </div>
            <div className="bg-gray-50 p-4 rounded-lg border border-gray-200">
                {cards.map((card) => (
                    // eslint-disable-next-line react/jsx-key
                    <Card
                        {...card}
                        // onDragStart={onDragStart}
                    />
                ))}
            </div>
        </div>
    );
};
