'use client';

import { useEffect, useMemo, useRef, useState } from 'react';
import type { Coach } from '@/_lib/types';
import env from '@/_lib/env';

declare global {
    interface Window {
        google?: any;
    }
}

function hasLatLng(
    coach: Coach,
): coach is Coach & { latitude: number; longitude: number } {
    return (
        typeof coach.latitude === 'number' &&
        Number.isFinite(coach.latitude) &&
        typeof coach.longitude === 'number' &&
        Number.isFinite(coach.longitude)
    );
}

async function loadGoogleMaps(apiKey: string): Promise<void> {
    if (window.google?.maps) return;

    await new Promise<void>((resolve, reject) => {
        const existing = document.querySelector(
            'script[data-projectbrain-google-maps="true"]',
        ) as HTMLScriptElement | null;

        if (existing) {
            existing.addEventListener('load', () => resolve());
            existing.addEventListener('error', () =>
                reject(new Error('Failed to load Google Maps script')),
            );
            return;
        }

        const script = document.createElement('script');
        script.setAttribute('data-projectbrain-google-maps', 'true');
        script.async = true;
        script.defer = true;
        script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(
            apiKey,
        )}&v=weekly`;
        script.onload = () => resolve();
        script.onerror = () =>
            reject(new Error('Failed to load Google Maps script'));
        document.head.appendChild(script);
    });
}

type SearchOrigin = { latitude: number; longitude: number };

export function CoachResultsMap({
    coaches,
    searchOrigin,
    searchRadiusMiles,
    onSelectCoach,
}: {
    coaches: Coach[];
    searchOrigin?: SearchOrigin | null;
    searchRadiusMiles?: number | null;
    onSelectCoach?: (coachProfileId: string) => void;
}) {
    const mapDivRef = useRef<HTMLDivElement | null>(null);
    const mapRef = useRef<any>(null);
    const markersRef = useRef<any[]>([]);
    const originMarkerRef = useRef<any>(null);
    const radiusCircleRef = useRef<any>(null);
    const [scriptError, setScriptError] = useState<string | null>(null);

    const mappableCoaches = useMemo(() => coaches.filter(hasLatLng), [coaches]);
    const hasSearchOrigin = useMemo(() => {
        return (
            typeof searchOrigin?.latitude === 'number' &&
            Number.isFinite(searchOrigin.latitude) &&
            typeof searchOrigin?.longitude === 'number' &&
            Number.isFinite(searchOrigin.longitude)
        );
    }, [searchOrigin]);

    useEffect(() => {
        const apiKey = env.NEXT_PUBLIC_GOOGLE_MAPS_API_KEY;
        if (!apiKey) {
            setScriptError(
                'Missing NEXT_PUBLIC_GOOGLE_MAPS_API_KEY (required for map view).',
            );
            return;
        }

        if (!mapDivRef.current) return;

        let cancelled = false;

        loadGoogleMaps(apiKey)
            .then(() => {
                if (cancelled) return;
                if (!mapDivRef.current) return;
                if (!window.google?.maps) {
                    setScriptError(
                        'Google Maps loaded, but API was unavailable.',
                    );
                    return;
                }

                if (!mapRef.current) {
                    mapRef.current = new window.google.maps.Map(
                        mapDivRef.current,
                        {
                            center: { lat: 51.5072, lng: -0.1276 },
                            zoom: 4,
                            mapTypeControl: false,
                            streetViewControl: false,
                            fullscreenControl: true,
                        },
                    );
                }

                // Clear old markers
                for (const m of markersRef.current) {
                    m.setMap(null);
                }
                markersRef.current = [];

                // Clear previous origin + circle
                if (originMarkerRef.current) {
                    originMarkerRef.current.setMap(null);
                    originMarkerRef.current = null;
                }
                if (radiusCircleRef.current) {
                    radiusCircleRef.current.setMap(null);
                    radiusCircleRef.current = null;
                }

                const bounds = new window.google.maps.LatLngBounds();
                const infoWindow = new window.google.maps.InfoWindow();

                for (const coach of mappableCoaches) {
                    const position = {
                        lat: coach.latitude,
                        lng: coach.longitude,
                    };
                    bounds.extend(position);
                    const marker = new window.google.maps.Marker({
                        map: mapRef.current,
                        position,
                        title: coach.fullName,
                    });

                    marker.addListener('click', () => {
                        const root = document.createElement('div');
                        root.style.cursor = onSelectCoach
                            ? 'pointer'
                            : 'default';
                        root.style.maxWidth = '260px';

                        const title = document.createElement('div');
                        title.textContent = coach.fullName;
                        title.style.fontWeight = '600';
                        title.style.marginBottom = '4px';
                        root.appendChild(title);

                        const subtitle = document.createElement('div');
                        subtitle.textContent = [coach.city, coach.country]
                            .filter(Boolean)
                            .join(', ');
                        subtitle.style.fontSize = '12px';
                        subtitle.style.opacity = '0.75';
                        subtitle.style.marginBottom = '6px';
                        root.appendChild(subtitle);

                        if (
                            typeof coach.averageRating === 'number' &&
                            Number.isFinite(coach.averageRating) &&
                            typeof coach.ratingCount === 'number'
                        ) {
                            const rating = document.createElement('div');
                            rating.textContent = `Rating: ${coach.averageRating.toFixed(
                                1,
                            )} (${coach.ratingCount})`;
                            rating.style.fontSize = '12px';
                            rating.style.opacity = '0.8';
                            rating.style.marginBottom = '6px';
                            root.appendChild(rating);
                        }

                        if (onSelectCoach) {
                            const cta = document.createElement('div');
                            cta.textContent = 'View in list';
                            cta.style.fontSize = '12px';
                            cta.style.fontWeight = '600';
                            cta.style.color = '#2563eb';
                            root.appendChild(cta);

                            root.addEventListener('click', () => {
                                infoWindow.close();
                                onSelectCoach(coach.coachProfileId);
                            });
                        }

                        infoWindow.setContent(root);
                        infoWindow.open({
                            map: mapRef.current,
                            anchor: marker,
                        });
                    });

                    markersRef.current.push(marker);
                }

                if (hasSearchOrigin) {
                    const originPosition = {
                        lat: searchOrigin!.latitude,
                        lng: searchOrigin!.longitude,
                    };

                    bounds.extend(originPosition);

                    originMarkerRef.current = new window.google.maps.Marker({
                        map: mapRef.current,
                        position: originPosition,
                        title: 'Search origin',
                        zIndex: 999,
                        icon: {
                            path: window.google.maps.SymbolPath.CIRCLE,
                            scale: 8,
                            fillColor: '#2563eb',
                            fillOpacity: 1,
                            strokeColor: '#ffffff',
                            strokeOpacity: 1,
                            strokeWeight: 2,
                        },
                    });

                    if (
                        typeof searchRadiusMiles === 'number' &&
                        Number.isFinite(searchRadiusMiles) &&
                        searchRadiusMiles > 0
                    ) {
                        radiusCircleRef.current = new window.google.maps.Circle(
                            {
                                map: mapRef.current,
                                center: originPosition,
                                radius: searchRadiusMiles * 1609.344, // miles -> meters
                                strokeColor: '#2563eb',
                                strokeOpacity: 0.75,
                                strokeWeight: 2,
                                fillColor: '#3b82f6',
                                fillOpacity: 0.12,
                                clickable: false,
                            },
                        );

                        const circleBounds =
                            radiusCircleRef.current.getBounds?.();
                        if (circleBounds) {
                            bounds.union(circleBounds);
                        }
                    }
                }

                if (!bounds.isEmpty()) {
                    mapRef.current.fitBounds(bounds, 48);

                    // Keep a sensible max zoom so the radius area stays visible.
                    window.google.maps.event.addListenerOnce(
                        mapRef.current,
                        'idle',
                        () => {
                            const z = mapRef.current?.getZoom?.();
                            if (typeof z === 'number' && z > 14) {
                                mapRef.current.setZoom(14);
                            }
                        },
                    );
                }
            })
            .catch((err) => {
                console.error(err);
                if (cancelled) return;
                setScriptError(
                    err instanceof Error
                        ? err.message
                        : 'Failed to load Google Maps',
                );
            });

        return () => {
            cancelled = true;
        };
    }, [hasSearchOrigin, mappableCoaches, searchOrigin, searchRadiusMiles]);

    if (scriptError) {
        return (
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
                <p className="text-sm text-red-800">{scriptError}</p>
            </div>
        );
    }

    if (mappableCoaches.length === 0 && !hasSearchOrigin) {
        return (
            <div className="bg-white border border-gray-200 rounded-lg p-6 text-sm text-gray-600">
                No coaches in these results have map coordinates yet.
            </div>
        );
    }

    return (
        <div className="bg-white border border-gray-200 rounded-lg overflow-hidden">
            <div ref={mapDivRef} className="h-[520px] w-full" />
        </div>
    );
}
