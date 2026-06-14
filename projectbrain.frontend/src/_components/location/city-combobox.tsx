'use client';

import { useEffect, useMemo, useState } from 'react';
import {
    Combobox,
    ComboboxButton,
    ComboboxInput,
    ComboboxOption,
    ComboboxOptions,
    Label,
} from '@headlessui/react';
import { CheckIcon, ChevronUpDownIcon } from '@heroicons/react/24/solid';
import { apiClient } from '@/_lib/api-client';
import type { CityOption } from '@/_lib/location-types';

function classNames(...classes: Array<string | false | undefined | null>) {
    return classes.filter(Boolean).join(' ');
}

type CityComboboxProps = {
    id: string;
    label: string;
    countryCode: string | null;
    value: CityOption | null;
    onChange: (value: CityOption | null) => void;
    placeholder?: string;
    disabled?: boolean;
};

export function CityCombobox({
    id,
    label,
    countryCode,
    value,
    onChange,
    placeholder = 'Start typing a city…',
    disabled = false,
}: CityComboboxProps) {
    const [query, setQuery] = useState('');
    const [options, setOptions] = useState<CityOption[]>([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        const q = query.trim();
        if (!countryCode || q.length < 2) return;

        let cancelled = false;

        const handle = window.setTimeout(() => {
            setLoading(true);
            apiClient<CityOption[]>(
                `/api/locations/cities?q=${encodeURIComponent(
                    q
                )}&countryCode=${encodeURIComponent(countryCode)}`
            )
                .then((data) => {
                    if (cancelled) return;
                    setOptions(data);
                })
                .catch((err) => {
                    console.error('Failed to load cities', err);
                    if (cancelled) return;
                    setOptions([]);
                })
                .finally(() => {
                    if (cancelled) return;
                    setLoading(false);
                });
        }, 250);

        return () => {
            cancelled = true;
            window.clearTimeout(handle);
        };
    }, [countryCode, query]);

    const effectiveDisabled = disabled || !countryCode;

    const displayValue = useMemo(() => {
        if (!value) return '';
        return [value.city, value.stateProvince, value.country]
            .filter(Boolean)
            .join(', ');
    }, [value]);

    return (
        <div>
            <Combobox value={value} onChange={onChange} disabled={effectiveDisabled}>
                <Label
                    htmlFor={id}
                    className="block text-sm font-medium text-gray-700"
                >
                    {label}
                </Label>

                <div className="relative mt-1">
                    <ComboboxInput
                        id={id}
                        disabled={effectiveDisabled}
                        className={classNames(
                            'block w-full rounded-md border-gray-300 bg-white shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm pr-10',
                            effectiveDisabled && 'bg-gray-50 cursor-not-allowed'
                        )}
                        displayValue={() => displayValue}
                        onChange={(event) => {
                            const next = event.target.value;
                            setQuery(next);
                            if (next.trim().length < 2) {
                                setOptions([]);
                                setLoading(false);
                            }
                        }}
                        placeholder={
                            effectiveDisabled
                                ? 'Select a country first'
                                : placeholder
                        }
                    />

                    <ComboboxButton className="absolute inset-y-0 right-0 flex items-center rounded-r-md px-2 focus:outline-none">
                        {loading ? (
                            <span className="text-xs text-gray-400">
                                Loading…
                            </span>
                        ) : (
                            <ChevronUpDownIcon
                                className="h-5 w-5 text-gray-400"
                                aria-hidden="true"
                            />
                        )}
                    </ComboboxButton>

                    {options.length > 0 && (
                        <ComboboxOptions className="absolute z-10 mt-1 max-h-60 w-full overflow-auto rounded-md bg-white py-1 text-base shadow-lg ring-1 ring-black/5 focus:outline-none sm:text-sm">
                            {options.map((opt) => (
                                <ComboboxOption
                                    key={opt.placeId}
                                    value={opt}
                                    className={({ active }) =>
                                        classNames(
                                            'relative cursor-default select-none py-2 pl-3 pr-9',
                                            active
                                                ? 'bg-indigo-600 text-white'
                                                : 'text-gray-900'
                                        )
                                    }
                                >
                                    {({ selected, active }) => (
                                        <>
                                            <span
                                                className={classNames(
                                                    'block truncate',
                                                    selected && 'font-semibold'
                                                )}
                                            >
                                                {opt.city}
                                                {opt.stateProvince
                                                    ? `, ${opt.stateProvince}`
                                                    : ''}
                                            </span>
                                            <span
                                                className={classNames(
                                                    'block truncate text-xs',
                                                    active
                                                        ? 'text-indigo-100'
                                                        : 'text-gray-500'
                                                )}
                                            >
                                                {opt.formattedAddress}
                                            </span>
                                            {selected && (
                                                <span
                                                    className={classNames(
                                                        'absolute inset-y-0 right-0 flex items-center pr-4',
                                                        active
                                                            ? 'text-white'
                                                            : 'text-indigo-600'
                                                    )}
                                                >
                                                    <CheckIcon
                                                        className="h-5 w-5"
                                                        aria-hidden="true"
                                                    />
                                                </span>
                                            )}
                                        </>
                                    )}
                                </ComboboxOption>
                            ))}
                        </ComboboxOptions>
                    )}
                </div>
            </Combobox>
        </div>
    );
}

