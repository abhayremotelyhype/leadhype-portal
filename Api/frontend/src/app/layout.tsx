import type { Metadata } from 'next';
import { Inter } from 'next/font/google';
import './globals.css';
import { Providers } from '@/components/providers';
import { ToastProvider } from '@/hooks/use-toast';
import { Toaster } from '@/components/ui/sonner';
import { AuthenticatedLayout } from '@/components/authenticated-layout';

const inter = Inter({ subsets: ['latin'] });

export const metadata: Metadata = {
  title: 'LeadHype - Email Campaign Management',
  description: 'Manage your email campaigns, accounts, and clients with LeadHype',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body className={inter.className} suppressHydrationWarning>
        <Providers>
          <ToastProvider>
            <AuthenticatedLayout>
              {children}
            </AuthenticatedLayout>
            <Toaster />
          </ToastProvider>
        </Providers>
      </body>
    </html>
  );
}