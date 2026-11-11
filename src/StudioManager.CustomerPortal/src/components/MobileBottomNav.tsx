'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Home, Sparkles, Grid, Calendar, UserCheck, User } from 'lucide-react';
import { portalDataProvider } from '@/lib/mock-provider';
import { BookingPreviewModal } from './BookingPreviewModal';

export const MobileBottomNav: React.FC = () => {
  const pathname = usePathname();
  const [isBookingModalOpen, setIsBookingModalOpen] = useState(false);
  const [hasUser, setHasUser] = useState(false);

  useEffect(() => {
    setHasUser(!!portalDataProvider.getCurrentSessionUser());
  }, [pathname]);

  const navItems = [
    {
      label: 'Trang chủ',
      href: '/',
      icon: Home,
      isActive: pathname === '/',
    },
    {
      label: 'Gói chụp',
      href: '/packages',
      icon: Sparkles,
      isActive: pathname.startsWith('/packages'),
    },
    {
      label: 'Không gian',
      href: '/studio-showcase',
      icon: Grid,
      isActive: pathname === '/studio-showcase',
    },
    {
      label: 'Đặt lịch',
      isAction: true,
      icon: Calendar,
      onClick: () => setIsBookingModalOpen(true),
      isActive: false,
    },
    {
      label: hasUser ? 'Tài khoản' : 'Đăng nhập',
      href: hasUser ? '/verify-identity' : '/login',
      icon: hasUser ? UserCheck : User,
      isActive: pathname === '/login' || pathname === '/verify-identity' || pathname === '/register',
    },
  ];

  return (
    <>
      <nav className="md:hidden fixed bottom-0 inset-x-0 z-40 bg-alabaster-50/95 backdrop-blur-md border-t border-stone-200/90 shadow-lg px-2 py-1 safe-area-pb">
        <div className="grid grid-cols-5 items-center">
          {navItems.map((item, idx) => {
            if (item.isAction) {
              return (
                <button
                  key={idx}
                  onClick={item.onClick}
                  className="flex flex-col items-center justify-center py-1 text-terracotta-600 focus:outline-none"
                >
                  <div className="h-8 w-8 rounded-full bg-terracotta-600 text-alabaster-50 flex items-center justify-center shadow-sm">
                    <item.icon className="h-4 w-4" />
                  </div>
                  <span className="text-[10px] font-medium mt-0.5 text-terracotta-700">
                    {item.label}
                  </span>
                </button>
              );
            }

            return (
              <Link
                key={idx}
                href={item.href!}
                className={`flex flex-col items-center justify-center py-1.5 transition-colors ${
                  item.isActive
                    ? 'text-terracotta-600 font-semibold'
                    : 'text-stone-500 hover:text-espresso-900'
                }`}
              >
                <item.icon className="h-5 w-5" />
                <span className="text-[10px] mt-0.5">{item.label}</span>
              </Link>
            );
          })}
        </div>
      </nav>

      <BookingPreviewModal
        isOpen={isBookingModalOpen}
        onClose={() => setIsBookingModalOpen(false)}
      />
    </>
  );
};

