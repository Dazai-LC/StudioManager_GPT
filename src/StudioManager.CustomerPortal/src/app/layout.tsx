import type { Metadata } from 'next';
import './globals.css';
import { DemoModeBanner } from '@/components/DemoModeBanner';
import { PortalHeader } from '@/components/PortalHeader';
import { Footer } from '@/components/Footer';
import { MobileBottomNav } from '@/components/MobileBottomNav';

export const metadata: Metadata = {
  title: 'Atelier Lumière — Cổng Khách Hàng & Dịch Vụ Nhiếp Ảnh Nghệ Thuật',
  description: 'Trải nghiệm đặt lịch, khám phá không gian studio và quản lý bộ sưu tập ảnh nghệ thuật phong cách Haute Photographie & Warm Editorial.',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="vi" className="scroll-smooth">
      <body className="min-h-screen flex flex-col bg-alabaster-50 text-espresso-900 selection:bg-terracotta-100 selection:text-terracotta-900">
        {/* Sticky Demo Banner */}
        <DemoModeBanner />

        {/* Brand Header */}
        <PortalHeader />

        {/* Page Content */}
        <main className="flex-1 pb-16 md:pb-0">
          {children}
        </main>

        {/* Brand Footer */}
        <Footer />

        {/* Mobile Sticky 5-Destination Navigation */}
        <MobileBottomNav />
      </body>
    </html>
  );
}

