export type CountryOption = {
    name: string;
    code: string; // ISO 3166-1 alpha-2
};

export type CityOption = {
    city: string;
    stateProvince?: string;
    country?: string;
    latitude: number;
    longitude: number;
    placeId: string;
    formattedAddress: string;
};

