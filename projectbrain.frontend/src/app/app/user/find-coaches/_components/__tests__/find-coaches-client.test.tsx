import userEvent from '@testing-library/user-event';
import { render, screen, waitFor } from '@/_lib/test-utils';
import { AppRoles } from '@/_lib/roles';
import FindCoachesClient from '../find-coaches-client';
import type { Coach } from '@/_lib/types';

const mockPush = jest.fn();
let mockCurrentSearchParams = new URLSearchParams('restore=1');
const mockReplace = jest.fn(() => {
    mockCurrentSearchParams = new URLSearchParams();
});

jest.mock('next/navigation', () => ({
    useRouter: () => ({
        push: mockPush,
        replace: mockReplace,
    }),
    useSearchParams: () => mockCurrentSearchParams,
}));

jest.mock('@/_components/location/country-combobox', () => ({
    CountryCombobox: () => <div data-testid="country-combobox" />,
}));

jest.mock('@/_components/location/city-combobox', () => ({
    CityCombobox: () => <div data-testid="city-combobox" />,
}));

jest.mock('@/_components/maps/coach-results-map', () => ({
    CoachResultsMap: () => <div data-testid="coach-results-map" />,
}));

const searchStateKey = 'projectbrain.findCoachesSearchState.v1';

function buildCoach(overrides: Partial<Coach> = {}): Coach {
    return {
        id: 'coach-user-1',
        coachProfileId: 'coach-profile-1',
        email: 'coach@example.com',
        fullName: 'Taylor Coach',
        isOnboarded: true,
        roles: [AppRoles.Coach],
        lastActivityAt: new Date().toISOString(),
        qualifications: ['Certified Coach'],
        specialisms: ['ADHD'],
        ageGroups: ['Adults (26-40)'],
        availabilityStatus: 'Available',
        averageRating: 5,
        ratingCount: 1,
        ...overrides,
    };
}

function restoreFindCoachesState(coaches: Coach[]) {
    sessionStorage.setItem(
        searchStateKey,
        JSON.stringify({
            savedAt: Date.now(),
            searchParams: {
                country: 'United Kingdom',
                city: '',
                stateProvince: '',
                ageGroups: [],
                specialisms: [],
            },
            selectedCountry: null,
            selectedCity: null,
            useMyLocation: false,
            distanceMiles: 25,
            searchCenter: null,
            coaches,
            connectionStatuses: Object.fromEntries(
                coaches.map((coach) => [
                    coach.coachProfileId,
                    { status: 'none' },
                ]),
            ),
            highlightedCoachId: null,
        }),
    );
}

describe('FindCoachesClient', () => {
    beforeEach(() => {
        mockPush.mockClear();
        mockReplace.mockClear();
        mockCurrentSearchParams = new URLSearchParams('restore=1');
        sessionStorage.clear();
    });

    afterEach(() => {
        jest.restoreAllMocks();
    });

    it('uses the returned connection id when contacting a coach after connecting', async () => {
        restoreFindCoachesState([buildCoach()]);
        jest.spyOn(global, 'fetch').mockResolvedValue(
            new Response(
                JSON.stringify({
                    id: 'connection-123',
                    status: 'connected',
                    requestedAt: '2026-06-12T18:00:00Z',
                }),
                {
                    status: 201,
                    headers: { 'Content-Type': 'application/json' },
                },
            ),
        );

        render(<FindCoachesClient defaultCountryName="United Kingdom" />);

        expect(await screen.findByText('Taylor Coach')).toBeInTheDocument();

        await userEvent.click(screen.getByRole('button', { name: 'Connect' }));

        const contactButton = await screen.findByRole('button', {
            name: 'Contact',
        });
        await waitFor(() => expect(contactButton).toBeEnabled());

        await userEvent.click(contactButton);

        expect(mockPush).toHaveBeenCalledWith(
            '/app/user/messages/connection-123',
        );
    });
});
