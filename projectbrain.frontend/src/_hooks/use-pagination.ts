import { useState, useMemo } from 'react';

export interface UsePaginationOptions {
    totalItems: number;
    itemsPerPage: number;
    initialPage?: number;
}

export interface UsePaginationReturn {
    currentPage: number;
    totalPages: number;
    itemsPerPage: number;
    startIndex: number;
    endIndex: number;
    goToPage: (page: number) => void;
    nextPage: () => void;
    previousPage: () => void;
    canGoNext: boolean;
    canGoPrevious: boolean;
}

/**
 * Hook for managing pagination state
 * @param options - Pagination options
 * @returns Pagination state and controls
 */
export function usePagination({
    totalItems,
    itemsPerPage,
    initialPage = 1,
}: UsePaginationOptions): UsePaginationReturn {
    const [currentPage, setCurrentPage] = useState(initialPage);

    const totalPages = useMemo(
        () => Math.ceil(totalItems / itemsPerPage),
        [totalItems, itemsPerPage]
    );

    const startIndex = useMemo(
        () => (currentPage - 1) * itemsPerPage,
        [currentPage, itemsPerPage]
    );

    const endIndex = useMemo(
        () => Math.min(startIndex + itemsPerPage, totalItems),
        [startIndex, itemsPerPage, totalItems]
    );

    const goToPage = (page: number) => {
        const pageNumber = Math.max(1, Math.min(page, totalPages));
        setCurrentPage(pageNumber);
    };

    const nextPage = () => {
        if (currentPage < totalPages) {
            setCurrentPage((prev) => prev + 1);
        }
    };

    const previousPage = () => {
        if (currentPage > 1) {
            setCurrentPage((prev) => prev - 1);
        }
    };

    const canGoNext = currentPage < totalPages;
    const canGoPrevious = currentPage > 1;

    return {
        currentPage,
        totalPages,
        itemsPerPage,
        startIndex,
        endIndex,
        goToPage,
        nextPage,
        previousPage,
        canGoNext,
        canGoPrevious,
    };
}

