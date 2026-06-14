'use client';

import { useEffect, useMemo, useRef, useState } from 'react';
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
import type { CountryOption } from '@/_lib/location-types';

function classNames(...classes: Array<string | false | undefined | null>) {
    return classes.filter(Boolean).join(' ');
}

type CountryComboboxProps = {
    id: string;
    label: string;
    value: CountryOption | null;
    onChange: (value: CountryOption | null) => void;
    initialCountryName?: string;
    placeholder?: string;
    disabled?: boolean;
};

export function CountryCombobox({
    id,
    label,
    value,
    onChange,
    initialCountryName,
    placeholder = 'Select country',
    disabled = false,
}: CountryComboboxProps) {
    const [countries, setCountries] = useState<CountryOption[]>([]);
    const [query, setQuery] = useState('');
    const [loading, setLoading] = useState(true);
    const hasAppliedInitial = useRef(false);

    useEffect(() => {
        let isMounted = true;
        apiClient<CountryOption[]>('/api/locations/countries')
            .then((data) => {
                if (!isMounted) return;
                setCountries(data);
            })
            .catch((err) => {
                console.error('Failed to load countries', err);
            })
            .finally(() => {
                if (!isMounted) return;
                setLoading(false);
            });
        return () => {
            isMounted = false;
        };
    }, []);

    useEffect(() => {
        if (hasAppliedInitial.current) return;
        if (value) return;
        if (!initialCountryName) return;
        if (countries.length === 0) return;

        const match = countries.find(
            (c) =>
                c.name.toLowerCase() === initialCountryName.trim().toLowerCase()
        );
        if (match) {
            hasAppliedInitial.current = true;
            onChange(match);
        }
    }, [countries, initialCountryName, onChange, value]);

    const filtered = useMemo(() => {
        const q = query.trim().toLowerCase();
        if (!q) return countries;
        return countries.filter(
            (c) =>
                c.name.toLowerCase().includes(q) ||
                c.code.toLowerCase().includes(q)
        );
    }, [countries, query]);

    return (
        <div>
            <Combobox value={value} onChange={onChange} disabled={disabled}>
                <Label
                    htmlFor={id}
                    className="block text-sm font-medium text-gray-700"
                >
                    {label}
                </Label>
                <div className="relative mt-1">
                    <ComboboxInput
                        id={id}
                        disabled={disabled}
                        className={classNames(
                            'block w-full rounded-md border-gray-300 bg-white shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm pr-10',
                            disabled && 'bg-gray-50 cursor-not-allowed'
                        )}
                        displayValue={(c: CountryOption) => c?.name ?? ''}
                        onChange={(event) => {
                            if (disabled) return;
                            setQuery(event.target.value);
                        }}
                        placeholder={placeholder}
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

                    {filtered.length > 0 && (
                        <ComboboxOptions className="absolute z-10 mt-1 max-h-60 w-full overflow-auto rounded-md bg-white py-1 text-base shadow-lg ring-1 ring-black/5 focus:outline-none sm:text-sm">
                            {filtered.map((country) => (
                                <ComboboxOption
                                    key={country.code}
                                    value={country}
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
                                            <div className="flex items-center justify-between">
                                                <span
                                                    className={classNames(
                                                        'truncate',
                                                        selected && 'font-semibold'
                                                    )}
                                                >
                                                    {country.name}
                                                </span>
                                                <span
                                                    className={classNames(
                                                        'ml-2 text-xs',
                                                        active
                                                            ? 'text-indigo-100'
                                                            : 'text-gray-500'
                                                    )}
                                                >
                                                    {country.code}
                                                </span>
                                            </div>

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

