'use client';

import React from 'react';
import Link from 'next/link';
import { ShieldAlert, ArrowLeft, LogIn, Home } from 'lucide-react';

export default function AccessDeniedPage() {
  return (
    <div className="min-h-screen bg-alabaster-50 flex items-center justify-center py-16 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full text-center space-y-6 bg-alabaster-100 p-8 sm:p-10 rounded-atelier-lg border border-stone-300 shadow-sm">
        {/* Error Icon */}
        <div className="h-16 w-16 rounded-full bg-red-50 text-red-600 border border-red-200 flex items-center justify-center mx-auto shadow-sm">
          <ShieldAlert className="h-8 w-8" />
        </div>

        <div>
          <span className="text-xs font-mono uppercase tracking-widest text-red-700 bg-red-100/60 px-2 py-0.5 rounded border border-red-200">
            CP-ERR-01 • 403 Access Denied
          </span>
          <h1 className="font-serif text-2xl sm:text-3xl font-bold text-espresso-900 mt-3">
            Quyền Truy Cập Bị Giới Hạn
          </h1>
          <p className="text-xs sm:text-sm text-stone-600 mt-2 leading-relaxed font-sans">
            Khu vực này yêu cầu đăng nhập bằng tài khoản Cổng Khách Hàng hoặc bạn chưa hoàn tất định danh liên kết theo chính sách bảo mật DEC-03.
          </p>
        </div>

        {/* Action Buttons */}
        <div className="pt-2 flex flex-col sm:flex-row items-center justify-center gap-3">
          <Link
            href="/login"
            className="w-full sm:w-auto px-5 py-2.5 rounded-atelier bg-terracotta-600 hover:bg-terracotta-700 text-alabaster-50 text-xs font-medium transition-colors flex items-center justify-center gap-1.5 shadow-sm"
          >
            <LogIn className="h-4 w-4" />
            <span>Đăng nhập ngay</span>
          </Link>
          <Link
            href="/"
            className="w-full sm:w-auto px-5 py-2.5 rounded-atelier bg-alabaster-50 hover:bg-stone-100 text-stone-700 border border-stone-300 text-xs font-medium transition-colors flex items-center justify-center gap-1.5"
          >
            <Home className="h-4 w-4" />
            <span>Về Trang chủ</span>
          </Link>
        </div>
      </div>
    </div>
  );
}

