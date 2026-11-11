'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import {
  Menu,
  X,
  Camera,
  User,
  LogOut,
  Sparkles,
  ShieldCheck,
  ShieldAlert,
  Calendar,
} from 'lucide-react';
import { portalDataProvider } from '@/lib/mock-provider';
import { CustomerAuthDto } from '@/lib/contracts';
import { BookingPreviewModal } from './BookingPreviewModal';

export const PortalHeader: React.FC = () => {
  const pathname = usePathname();
  const router = useRouter();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [currentUser, setCurrentUser] = useState<CustomerAuthDto | null>(null);
  const [isBookingModalOpen, setIsBookingModalOpen] = useState(false);

  useEffect(() => {
    // Sync current session user
    setCurrentUser(portalDataProvider.getCurrentSessionUser());
  }, [pathname]);

  const handleLogout = () => {
    portalDataProvider.setSessionUser(null);
    setCurrentUser(null);
    router.push('/');
    router.refresh();
  };

  const navLinks = [
    { href: '/', label: 'Trang chủ' },
    { href: '/packages', label: 'Gói chụp' },
    { href: '/studio-showcase', label: 'Không gian & Thiết bị' },
    { href: '/about', label: 'Về Atelier' },
  ];

  return (
    <>
      <header className="sticky top-0 z-40 bg-alabaster-50/95 backdrop-blur-md border-b border-stone-200/80 transition-all">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-20">
            {/* Brand Logo */}
            <Link href="/" className="flex items-center gap-3 group">
              <div className="h-10 w-10 rounded-atelier bg-espresso-900 text-alabaster-50 flex items-center justify-center transition-transform group-hover:scale-105 shadow-sm">
                <Camera className="h-5 w-5 text-terracotta-500" />
              </div>
              <div className="flex flex-col">
                <span className="font-serif text-xl sm:text-2xl font-bold tracking-tight text-espresso-900 group-hover:text-terracotta-600 transition-colors">
                  ATELIER LUMIÈRE
                </span>
                <span className="text-[10px] uppercase font-sans tracking-widest text-stone-500 -mt-0.5">
                  Haute Photographie & Art Studio
                </span>
              </div>
            </Link>

            {/* Desktop Navigation */}
            <nav className="hidden md:flex items-center gap-8">
              {navLinks.map((link) => {
                const isActive = pathname === link.href;
                return (
                  <Link
                    key={link.href}
                    href={link.href}
                    className={`text-sm font-medium tracking-wide transition-colors relative py-1 ${
                      isActive
                        ? 'text-terracotta-600 font-semibold'
                        : 'text-stone-600 hover:text-espresso-900'
                    }`}
                  >
                    {link.label}
                    {isActive && (
                      <span className="absolute bottom-0 inset-x-0 h-0.5 bg-terracotta-600 rounded-full" />
                    )}
                  </Link>
                );
              })}
            </nav>

            {/* Desktop Right Side Actions */}
            <div className="hidden md:flex items-center gap-3">
              {currentUser ? (
                <div className="flex items-center gap-3">
                  <Link
                    href="/verify-identity"
                    title="Trạng thái định danh khách hàng (DEC-03)"
                    className={`flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-medium border transition-colors ${
                      currentUser.linkedStatus === 'VERIFIED'
                        ? 'bg-emerald-50 text-emerald-800 border-emerald-200 hover:bg-emerald-100'
                        : currentUser.linkedStatus === 'PENDING_VERIFICATION'
                        ? 'bg-amber-50 text-amber-800 border-amber-200 hover:bg-amber-100'
                        : 'bg-stone-100 text-stone-700 border-stone-300 hover:bg-stone-200'
                    }`}
                  >
                    {currentUser.linkedStatus === 'VERIFIED' ? (
                      <ShieldCheck className="h-3.5 w-3.5 text-emerald-600" />
                    ) : (
                      <ShieldAlert className="h-3.5 w-3.5 text-amber-600" />
                    )}
                    <span>
                      {currentUser.linkedStatus === 'VERIFIED'
                        ? 'Đã liên kết hồ sơ'
                        : currentUser.linkedStatus === 'PENDING_VERIFICATION'
                        ? 'Đang chờ duyệt'
                        : 'Chưa liên kết'}
                    </span>
                  </Link>

                  <div className="flex items-center gap-2 pl-2 border-l border-stone-200">
                    <div className="flex flex-col text-right">
                      <span className="text-xs font-semibold text-espresso-900">
                        {currentUser.fullName}
                      </span>
                      <span className="text-[10px] text-stone-500 font-mono">
                        {currentUser.phoneNumber}
                      </span>
                    </div>

                    <button
                      onClick={handleLogout}
                      title="Đăng xuất khỏi Cổng Khách Hàng"
                      className="p-2 rounded-atelier text-stone-400 hover:text-crimson-600 hover:bg-stone-100 transition-colors"
                    >
                      <LogOut className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              ) : (
                <div className="flex items-center gap-2">
                  <Link
                    href="/login"
                    className="px-4 py-2 text-sm font-medium text-stone-700 hover:text-espresso-900 transition-colors"
                  >
                    Đăng nhập
                  </Link>
                  <Link
                    href="/register"
                    className="px-4 py-2 text-sm font-medium rounded-atelier border border-stone-300 text-espresso-900 hover:bg-stone-100 transition-colors"
                  >
                    Đăng ký
                  </Link>
                </div>
              )}

              <button
                onClick={() => setIsBookingModalOpen(true)}
                className="ml-2 inline-flex items-center gap-1.5 px-4 py-2 rounded-atelier bg-terracotta-600 hover:bg-terracotta-700 text-alabaster-50 text-sm font-medium shadow-sm transition-all hover:shadow"
              >
                <Calendar className="h-4 w-4" />
                <span>Đặt lịch ngay</span>
              </button>
            </div>

            {/* Mobile Hamburger Button */}
            <div className="flex md:hidden items-center gap-2">
              <button
                onClick={() => setIsBookingModalOpen(true)}
                className="p-2 rounded-atelier bg-terracotta-600 text-alabaster-50 text-xs font-medium"
              >
                Đặt lịch
              </button>

              <button
                onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
                className="p-2 rounded-atelier text-stone-700 hover:text-espresso-900 hover:bg-stone-100"
                aria-label="Mở menu"
              >
                {isMobileMenuOpen ? <X className="h-6 w-6" /> : <Menu className="h-6 w-6" />}
              </button>
            </div>
          </div>
        </div>

        {/* Mobile Navigation Drawer */}
        {isMobileMenuOpen && (
          <div className="md:hidden border-t border-stone-200 bg-alabaster-50 px-4 pt-3 pb-6 space-y-3 animate-in slide-in-from-top duration-200">
            <nav className="space-y-1">
              {navLinks.map((link) => {
                const isActive = pathname === link.href;
                return (
                  <Link
                    key={link.href}
                    href={link.href}
                    onClick={() => setIsMobileMenuOpen(false)}
                    className={`block px-3 py-2.5 rounded-atelier text-sm font-medium ${
                      isActive
                        ? 'bg-terracotta-50 text-terracotta-700 font-semibold'
                        : 'text-stone-700 hover:bg-stone-100'
                    }`}
                  >
                    {link.label}
                  </Link>
                );
              })}
            </nav>

            <div className="pt-3 border-t border-stone-200 space-y-2">
              {currentUser ? (
                <div className="space-y-2">
                  <div className="px-3 py-2 rounded-atelier bg-alabaster-100 text-xs">
                    <p className="font-semibold text-espresso-900">{currentUser.fullName}</p>
                    <p className="text-stone-500 font-mono">{currentUser.phoneNumber}</p>
                  </div>
                  <Link
                    href="/verify-identity"
                    onClick={() => setIsMobileMenuOpen(false)}
                    className="block px-3 py-2 text-xs text-center rounded-atelier bg-stone-100 text-stone-700 font-medium"
                  >
                    Xác thực danh tính (DEC-03)
                  </Link>
                  <button
                    onClick={() => {
                      setIsMobileMenuOpen(false);
                      handleLogout();
                    }}
                    className="w-full px-3 py-2 text-xs text-center rounded-atelier text-crimson-600 bg-crimson-50 font-medium"
                  >
                    Đăng xuất
                  </button>
                </div>
              ) : (
                <div className="grid grid-cols-2 gap-2">
                  <Link
                    href="/login"
                    onClick={() => setIsMobileMenuOpen(false)}
                    className="px-4 py-2.5 text-center text-xs font-medium rounded-atelier border border-stone-300 text-espresso-900"
                  >
                    Đăng nhập
                  </Link>
                  <Link
                    href="/register"
                    onClick={() => setIsMobileMenuOpen(false)}
                    className="px-4 py-2.5 text-center text-xs font-medium rounded-atelier bg-terracotta-600 text-alabaster-50"
                  >
                    Đăng ký
                  </Link>
                </div>
              )}
            </div>
          </div>
        )}
      </header>

      {/* Booking Preview Modal */}
      <BookingPreviewModal
        isOpen={isBookingModalOpen}
        onClose={() => setIsBookingModalOpen(false)}
      />
    </>
  );
};

