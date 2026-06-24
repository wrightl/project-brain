'use client';

import { useState } from 'react';
import {
    CheckCircleIcon,
    XCircleIcon,
    ChevronDownIcon,
    ChevronUpIcon,
} from '@heroicons/react/24/outline';
import { ToolExecution } from '@/_lib/types';
import Link from 'next/link';

interface ToolExecutionBadgeProps {
    tool: ToolExecution;
}

export default function ToolExecutionBadge({ tool }: ToolExecutionBadgeProps) {
    const [isExpanded, setIsExpanded] = useState(false);

    const getToolDisplayName = (toolName: string): string => {
        const displayNames: Record<string, string> = {
            create_daily_goals: 'Created daily goals',
            create_goals_for_days: 'Planned goals for multiple days',
            get_todays_goals: "Retrieved today's goals",
            complete_goal: 'Updated goal',
            suggest_coping_strategies: 'Suggested coping strategies',
            save_coping_strategy: 'Saved coping strategy',
            get_coping_strategies: 'Retrieved coping strategies',
            rate_coping_strategy: 'Rated coping strategy',
            upload_knowledge_document: 'Uploaded knowledge document',
            list_knowledge_resources: 'Listed knowledge resources',
            delete_knowledge_resource: 'Deleted knowledge resource',
            search_coaches: 'Searched coaches',
            get_connected_coaches: 'Retrieved connected coaches',
        };
        return displayNames[toolName] || toolName;
    };

    const getResultSummary = (): string => {
        if (!tool.success) {
            return tool.errorMessage || 'Action failed';
        }

        // Try to extract meaningful summary from result
        if (typeof tool.result === 'object' && tool.result !== null) {
            const result = tool.result as Record<string, unknown>;
            if (result.message) {
                return String(result.message);
            }
            if (result.success !== undefined) {
                return result.success
                    ? 'Action completed successfully'
                    : 'Action failed';
            }
        }

        return tool.success ? 'Action completed' : 'Action failed';
    };

    const hasGoalsResult = (): boolean => {
        if (
            !tool.success ||
            typeof tool.result !== 'object' ||
            tool.result === null
        ) {
            return false;
        }
        const result = tool.result as Record<string, unknown>;
        return (
            tool.toolName === 'create_daily_goals' &&
            Array.isArray(result.goals)
        );
    };

    const getDeepLink = (): { href: string; label: string } | null => {
        if (!tool.success) {
            return null;
        }

        switch (tool.toolName) {
            case 'create_daily_goals':
            case 'create_goals_for_days':
                return { href: '/app/user/eggs', label: 'View your goals' };
            case 'save_coping_strategy':
            case 'get_coping_strategies':
            case 'rate_coping_strategy':
            case 'suggest_coping_strategies':
                return { href: '/app/user/strategies', label: 'View your strategies' };
            case 'upload_knowledge_document':
            case 'list_knowledge_resources':
                return { href: '/app/user/manage-resources', label: 'Manage resources' };
            case 'search_coaches':
            case 'get_connected_coaches':
                return { href: '/app/user/find-coaches', label: 'Find coaches' };
            default:
                return null;
        }
    };

    const formatDate = (dateString: string): string => {
        try {
            const date = new Date(dateString);
            return date.toLocaleTimeString('en-US', {
                hour: '2-digit',
                minute: '2-digit',
            });
        } catch {
            return '';
        }
    };

    return (
        <div className="mt-2 border border-purple-200 rounded-lg bg-purple-50">
            <button
                onClick={() => setIsExpanded(!isExpanded)}
                className="w-full flex items-center justify-between px-3 py-2 text-left hover:bg-purple-100 transition-colors rounded-lg"
            >
                <div className="flex items-center space-x-2 flex-1 min-w-0">
                    {tool.success ? (
                        <CheckCircleIcon className="h-5 w-5 text-green-600 flex-shrink-0" />
                    ) : (
                        <XCircleIcon className="h-5 w-5 text-red-600 flex-shrink-0" />
                    )}
                    <span className="text-sm font-medium text-purple-900 truncate">
                        {getToolDisplayName(tool.toolName)}
                    </span>
                    <span className="text-xs text-purple-600 truncate">
                        {getResultSummary()}
                    </span>
                </div>
                {isExpanded ? (
                    <ChevronUpIcon className="h-4 w-4 text-purple-600 flex-shrink-0 ml-2" />
                ) : (
                    <ChevronDownIcon className="h-4 w-4 text-purple-600 flex-shrink-0 ml-2" />
                )}
            </button>

            {isExpanded && (
                <div className="px-3 pb-3 space-y-2 border-t border-purple-200 pt-2">
                    <div className="text-xs text-purple-700">
                        <span className="font-medium">Status:</span>{' '}
                        {tool.success ? (
                            <span className="text-green-600">Success</span>
                        ) : (
                            <span className="text-red-600">Failed</span>
                        )}
                    </div>

                    {tool.errorMessage && (
                        <div className="text-xs text-red-600 bg-red-50 p-2 rounded">
                            <span className="font-medium">Error:</span>{' '}
                            {tool.errorMessage}
                        </div>
                    )}

                    {Object.keys(tool.parameters).length > 0 && (
                        <details className="text-xs">
                            <summary className="cursor-pointer text-purple-700 font-medium mb-1">
                                Parameters
                            </summary>
                            <pre className="mt-1 p-2 bg-white rounded text-xs overflow-x-auto">
                                {JSON.stringify(tool.parameters, null, 2)}
                            </pre>
                        </details>
                    )}

                    {tool.result != null && (
                        <details className="text-xs">
                            <summary className="cursor-pointer text-purple-700 font-medium mb-1">
                                Result
                            </summary>
                            <pre className="mt-1 p-2 bg-white rounded text-xs overflow-x-auto">
                                {JSON.stringify(tool.result, null, 2)}
                            </pre>
                        </details>
                    )}

                    {(hasGoalsResult() || getDeepLink()) && (
                        <div className="pt-2">
                            <Link
                                href={getDeepLink()?.href ?? '/app/user/eggs'}
                                className="text-xs text-indigo-600 hover:text-indigo-800 underline font-medium"
                            >
                                {getDeepLink()?.label ?? 'View your goals'} →
                            </Link>
                        </div>
                    )}

                    {tool.executedAt && (
                        <div className="text-xs text-purple-500">
                            Executed at {formatDate(tool.executedAt)}
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}
