import React, { PropsWithChildren } from 'react';

export interface CardProps {
    title?: string;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    // onDragStart: (e: any, cardId: string) => void;
}

const CardComponent: React.FC<PropsWithChildren<CardProps>> = ({
    children,
    title,
    // onDragStart,
}) => {
    return (
        <div
            // draggable
            // onDragStart={(e) => onDragStart(e, id)}
            className="mb-4 bg-white rounded-lg border border-gray-200 p-4"
        >
            <div className="flex items-center justify-between mb-3">
                <h3 className="font-semibold text-gray-800">{title}</h3>
                {/* <div className="h-6 w-6 rounded-full bg-blue-100 flex items-center justify-center">
                    <span className="text-xs text-blue-600 font-medium">
                        {id}
                    </span>
                </div> */}
            </div>
            <p className="text-gray-600 text-sm">{children}</p>
            <div className="mt-3 pt-3 border-t border-gray-100 flex items-center justify-between">
                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                    Task
                </span>
                <div className="w-8 h-8 rounded-full bg-gray-100 flex items-center justify-center">
                    <span className="text-xs text-gray-600">↕️</span>
                </div>
            </div>
        </div>
    );
};

export const Card = React.memo(CardComponent);
