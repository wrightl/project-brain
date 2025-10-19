import { CardProps } from '@/components/card';
import { Column } from '@/components/column';
import { getAllEggs } from '@/services/eggs.service';
// import { getUserProfileData } from '@/services/profile.service';
import { withPageAuthRequired } from '@auth0/nextjs-auth0';
import { NextPage } from 'next';

const EggsPage: NextPage = withPageAuthRequired(async () => {
    // const user = await getUserProfileData();
    const eggs = await getAllEggs();
    const incompleteEggs = eggs
        .filter((e) => !e.isComplete)
        .map<CardProps>((e) => ({ title: e.title, id: e.id }));
    const completeEggs = eggs
        .filter((e) => e.isComplete)
        .map<CardProps>((e) => ({ title: e.title, id: e.id }));

    return (
        <div className="h-full flex-col">
            <h2 className="text-2xl/7 mb-6 font-bold text-gray-900 sm:truncate sm:text-3xl sm:tracking-tight text-center">
                Eggs
            </h2>
            <div className="flex flex-row grow">
                <Column title={'To do'} cards={incompleteEggs}></Column>
                {/* <ul
                                    role="list"
                                    className="divide-y divide-gray-100"
                                >
                                    {incompleteEggs.map((egg) => (
                                        <Card
                                            id={egg.id}
                                            title={egg.title}
                                            key={egg.id}
                                            onDragStart={() => {}}
                                        >
                                            {egg.title}
                                        </Card>
                                        // <li
                                        //     key={egg.id}
                                        //     className="flex justify-between gap-x-6 py-5"
                                        // >
                                        //          <div className="flex min-w-0 gap-x-4">
                                        //     <div className="min-w-0 flex-auto">
                                        //         <p className="text-sm/6 font-semibold text-gray-900">
                                        //             {egg.title}
                                        //         </p>
                                        //         <p className="mt-1 truncate text-xs/5 text-gray-500">
                                        //             {egg.title}
                                        //         </p>
                                        //     </div>
                                        // </div>

                                        //     <div className="max-w-sm p-6 bg-white border border-gray-200 rounded-lg shadow dark:bg-gray-800 dark:border-gray-700">
                                        //         <a href="#">
                                        //             <h5 className="mb-2 text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
                                        //                 {egg.title}
                                        //             </h5>
                                        //         </a>
                                        //         <p className="mb-3 font-normal text-gray-700 dark:text-gray-400">
                                        //             {egg.title}
                                        //         </p>
                                        //         <a
                                        //             href="#"
                                        //             className="inline-flex items-center px-3 py-2 text-sm font-medium text-center text-white bg-blue-700 rounded-lg hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800"
                                        //         >
                                        //             Read more
                                        //             <svg
                                        //                 className="rtl:rotate-180 w-3.5 h-3.5 ms-2"
                                        //                 aria-hidden="true"
                                        //                 xmlns="http://www.w3.org/2000/svg"
                                        //                 fill="none"
                                        //                 viewBox="0 0 14 10"
                                        //             >
                                        //                 <path
                                        //                     stroke="currentColor"
                                        //                     stroke-linecap="round"
                                        //                     stroke-linejoin="round"
                                        //                     stroke-width="2"
                                        //                     d="M1 5h12m0 0L9 1m4 4L9 9"
                                        //                 />
                                        //             </svg>
                                        //         </a>
                                        //     </div>
                                        // </li>
                                    ))}
                                </ul> */}
                <Column title={'Completed'} cards={completeEggs}></Column>
            </div>
        </div>
    );
});

export default EggsPage;
