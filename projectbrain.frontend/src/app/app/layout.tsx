import SidebarNav from '@/components/sidebar-nav';

const AppLayout = ({ children }: { children: React.ReactNode }) => {
    return (
        <div className="flex h-screen w-full bg-gray-100">
            <SidebarNav />
            <div className="w-full h-full ml-64 p-4">{children}</div>
        </div>
    );
};

export default AppLayout;
