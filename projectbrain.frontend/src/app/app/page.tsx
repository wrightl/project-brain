import { NextPage } from 'next';
import React from 'react';
import Image from 'next/image';
import { getUserProfileData } from '@/services/profile.service';
import { withPageAuthRequired } from '@auth0/nextjs-auth0';
import { getWeatherForecast } from '@/services/weather-forecast.service';

const App: NextPage = withPageAuthRequired(
    async () => {
        const user = await getUserProfileData();
        const forecasts = await getWeatherForecast();

        return (
            <div className="content-layout">
                <h1 id="page-title" className="content__title">
                    App Page
                </h1>
                <div className="content__body">
                    <p id="page-description">
                        <span>
                            <strong>
                                Only authenticated users should access this
                                page.
                            </strong>
                        </span>
                    </p>
                    <div className="profile-grid">
                        <div className="profile__header">
                            <Image
                                src={user.picture}
                                alt="Profile"
                                className="profile__avatar"
                                width={80}
                                height={80}
                            />
                            <div className="profile__headline">
                                <h2 className="profile__title">{user.name}</h2>
                                <span className="profile__description">
                                    {user.email}
                                </span>
                            </div>
                        </div>
                        <div className="profile__details">
                            <ul
                                role="list"
                                className="divide-y divide-gray-100"
                            >
                                {forecasts.map((forecast) => (
                                    // eslint-disable-next-line react/jsx-key
                                    <li className="flex justify-between gap-x-6 py-5">
                                        <div className="flex min-w-0 gap-x-4">
                                            <div className="min-w-0 flex-auto">
                                                <p className="text-sm/6 font-semibold text-gray-900">
                                                    {forecast.summary}
                                                </p>
                                                <p className="mt-1 truncate text-xs/5 text-gray-500">
                                                    <span>
                                                        {forecast.date.toString()}
                                                    </span>
                                                </p>
                                            </div>
                                        </div>
                                        <div className="hidden shrink-0 sm:flex sm:flex-col sm:items-end">
                                            <p className="text-sm/6 text-gray-900">
                                                {forecast.temperatureC}
                                            </p>
                                        </div>
                                    </li>
                                ))}
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        );
    },
    { returnTo: '/app' }
);

export default App;
