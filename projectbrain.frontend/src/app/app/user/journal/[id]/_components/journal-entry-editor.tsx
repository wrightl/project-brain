'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import {
    useJournalEntry,
    useCreateJournalEntry,
    useUpdateJournalEntry,
    useDeleteJournalEntry,
} from '@/_hooks/queries/use-journals';
import { useTags, useGetOrCreateTag } from '@/_hooks/queries/use-tags';
import { JournalEntry, JournalTag } from '@/_services/journal-service';
import { Tag } from '@/_services/tag-service';
import {
    TrashIcon,
    ClockIcon,
    XMarkIcon,
    ArrowLeftIcon,
} from '@heroicons/react/24/outline';
import toast from 'react-hot-toast';

interface JournalEntryEditorProps {
    entryId?: string;
}

export default function JournalEntryEditor({
    entryId,
}: JournalEntryEditorProps) {
    const router = useRouter();
    const isNew = !entryId;

    const { data: entry, isLoading: loadingEntry } = useJournalEntry(
        entryId || ''
    );
    const { data: tags } = useTags();
    const createMutation = useCreateJournalEntry();
    const updateMutation = useUpdateJournalEntry();
    const deleteMutation = useDeleteJournalEntry();
    const getOrCreateTag = useGetOrCreateTag();

    const [content, setContent] = useState('');
    const [selectedTagIds, setSelectedTagIds] = useState<string[]>([]);
    const [newTagName, setNewTagName] = useState('');
    const [isEditing, setIsEditing] = useState(isNew);
    const [isSaving, setIsSaving] = useState(false);

    useEffect(() => {
        if (entry) {
            setContent(entry.content);
            setSelectedTagIds(entry.tags?.map((t) => t.id) || []);
        }
    }, [entry]);

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-GB', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        });
    };

    const handleSave = async () => {
        if (!content.trim()) {
            toast.error('Content is required');
            return;
        }

        setIsSaving(true);
        try {
            if (isNew) {
                const newEntry = (await createMutation.mutateAsync({
                    content: content.trim(),
                    tagIds:
                        selectedTagIds.length > 0 ? selectedTagIds : undefined,
                })) as JournalEntry;
                toast.success('Journal entry created successfully');
                router.push(`/app/user/journal/${newEntry.id}`);
            } else {
                await updateMutation.mutateAsync({
                    id: entryId!,
                    request: {
                        content: content.trim(),
                        tagIds:
                            selectedTagIds.length > 0
                                ? selectedTagIds
                                : undefined,
                    },
                });
                toast.success('Journal entry updated successfully');
                setIsEditing(false);
            }
        } catch (error) {
            toast.error(
                error instanceof Error
                    ? error.message
                    : 'Failed to save journal entry'
            );
        } finally {
            setIsSaving(false);
        }
    };

    const handleDelete = async () => {
        if (!entryId) return;

        if (!confirm('Are you sure you want to delete this journal entry?')) {
            return;
        }

        try {
            await deleteMutation.mutateAsync(entryId);
            toast.success('Journal entry deleted successfully');
            router.push('/app/user/journal');
        } catch (error) {
            toast.error(
                error instanceof Error
                    ? error.message
                    : 'Failed to delete journal entry'
            );
        }
    };

    const handleTagToggle = (tagId: string) => {
        setSelectedTagIds((prev) =>
            prev.includes(tagId)
                ? prev.filter((id) => id !== tagId)
                : [...prev, tagId]
        );
    };

    const handleCreateTag = async () => {
        if (!newTagName.trim()) return;

        try {
            const tag = await getOrCreateTag.mutateAsync(newTagName.trim());
            if (tag && tag.id) {
                setSelectedTagIds((prev) => [...prev, tag.id]);
                setNewTagName('');
                toast.success('Tag created and added');
            } else {
                throw new Error('Tag was created but no ID was returned');
            }
        } catch (error) {
            toast.error(
                error instanceof Error ? error.message : 'Failed to create tag'
            );
        }
    };

    if (loadingEntry && !isNew) {
        return (
            <div className="flex justify-center items-center py-12">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600" />
            </div>
        );
    }

    const availableTags = tags || [];
    const selectedTags = availableTags.filter((t) =>
        selectedTagIds.includes(t.id)
    );

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">
                        {isNew ? 'New Journal Entry' : 'Journal Entry'}
                    </h1>
                    {!isNew && entry && (
                        <div className="mt-1 flex items-center text-sm text-gray-500">
                            <ClockIcon className="h-4 w-4 mr-1" />
                            Created: {formatDate(entry.createdAt)}
                        </div>
                    )}
                </div>
                <div className="flex items-center space-x-2">
                    {!isNew && !isEditing && (
                        <>
                            <button
                                onClick={() => setIsEditing(true)}
                                className="inline-flex items-center px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
                            >
                                Edit
                            </button>
                            <button
                                onClick={handleDelete}
                                disabled={deleteMutation.isPending}
                                className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-red-600 hover:bg-red-700 disabled:opacity-50"
                            >
                                <TrashIcon className="h-5 w-5 mr-2" />
                                Delete
                            </button>
                        </>
                    )}
                    {isEditing && (
                        <button
                            onClick={() => {
                                if (isNew) {
                                    router.push('/app/user/journal');
                                } else {
                                    setIsEditing(false);
                                    if (entry) {
                                        setContent(entry.content);
                                        setSelectedTagIds(
                                            entry.tags?.map((t) => t.id) || []
                                        );
                                    }
                                }
                            }}
                            className="inline-flex items-center px-4 py-2 border border-gray-300 shadow-sm text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
                        >
                            Cancel
                        </button>
                    )}
                </div>
            </div>
            <Link
                href="/app/user/journal"
                className="inline-flex items-center text-sm font-medium text-gray-500 hover:text-gray-700"
            >
                <ArrowLeftIcon className="h-5 w-5 mr-1" />
                Back to List
            </Link>

            <div className="bg-white shadow rounded-lg p-6">
                {/* Tags Section */}
                <div className="mb-6">
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                        Tags
                    </label>
                    <div className="space-y-3">
                        {/* Selected Tags */}
                        {selectedTags.length > 0 && (
                            <div className="flex flex-wrap gap-2">
                                {selectedTags.map((tag) => (
                                    <span
                                        key={tag.id}
                                        className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-indigo-100 text-indigo-800"
                                    >
                                        {tag.name}
                                        {isEditing && (
                                            <button
                                                onClick={() =>
                                                    handleTagToggle(tag.id)
                                                }
                                                className="ml-2 text-indigo-600 hover:text-indigo-800"
                                            >
                                                <XMarkIcon className="h-4 w-4" />
                                            </button>
                                        )}
                                    </span>
                                ))}
                            </div>
                        )}

                        {/* Available Tags */}
                        {isEditing && (
                            <div className="space-y-2">
                                <div className="flex flex-wrap gap-2">
                                    {availableTags
                                        .filter(
                                            (t) =>
                                                !selectedTagIds.includes(t.id)
                                        )
                                        .map((tag) => (
                                            <button
                                                key={tag.id}
                                                onClick={() =>
                                                    handleTagToggle(tag.id)
                                                }
                                                className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-gray-100 text-gray-700 hover:bg-gray-200"
                                            >
                                                {tag.name}
                                            </button>
                                        ))}
                                </div>

                                {/* Create New Tag */}
                                <div className="flex items-center space-x-2">
                                    <input
                                        type="text"
                                        value={newTagName}
                                        onChange={(e) =>
                                            setNewTagName(e.target.value)
                                        }
                                        onKeyDown={(e) => {
                                            if (e.key === 'Enter') {
                                                e.preventDefault();
                                                handleCreateTag();
                                            }
                                        }}
                                        placeholder="Create new tag..."
                                        className="block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                                    />
                                    <button
                                        onClick={handleCreateTag}
                                        disabled={!newTagName.trim()}
                                        className="inline-flex items-center px-3 py-2 border border-transparent text-sm leading-4 font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed"
                                    >
                                        Add
                                    </button>
                                </div>
                            </div>
                        )}
                    </div>
                </div>

                {/* Content Section */}
                <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                        Journal Entry
                    </label>
                    {isEditing ? (
                        <textarea
                            value={content}
                            onChange={(e) => setContent(e.target.value)}
                            rows={15}
                            className="block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                            placeholder="Write your journal entry here..."
                        />
                    ) : (
                        <div className="prose max-w-none">
                            <div className="whitespace-pre-wrap text-gray-900 bg-gray-50 rounded-md p-4 min-h-[300px]">
                                {entry?.content || 'No content'}
                            </div>
                        </div>
                    )}
                </div>

                {/* Summary (read-only) */}
                {!isNew && entry?.summary && (
                    <div className="mt-6">
                        <label className="block text-sm font-medium text-gray-700 mb-2">
                            Summary
                        </label>
                        <p className="text-sm text-gray-600 bg-gray-50 rounded-md p-3">
                            {entry.summary}
                        </p>
                    </div>
                )}

                {/* Save Button */}
                {isEditing && (
                    <div className="mt-6 flex justify-end">
                        <button
                            onClick={handleSave}
                            disabled={isSaving || !content.trim()}
                            className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                            {isSaving ? 'Saving...' : 'Save'}
                        </button>
                    </div>
                )}
            </div>
        </div>
    );
}
