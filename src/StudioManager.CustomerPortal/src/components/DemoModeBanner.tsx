'use client';

import React, { useState } from 'react';
import { AlertCircle, X, ChevronDown, ChevronUp, UserCheck, ShieldAlert } from 'lucide-react';
import { portalDataProvider } from '@/lib/mock-provider';

export const DemoModeBanner: React.FC = () => {
  const [isMinimized, setIsMinimized] = useState(false);
  const [isDismissed, setIsDismissed] = useState(false);
  const currentUser = portalDataProvider.getCurrentSessionUser();

  if (isDismissed) return null;

  if (isMinimized) {
    return (
      <div className="bg-espresso-900 border-b border-ochre-600/40 text-stone-200 text-xs px-4 py-1.5 flex items-center justify-between z-50">
        <div className="flex items-center gap-2">
          <span className="inline-block h-2 w-2 rounded-full bg-ochre-600 animate-pulse" />
          <span className="font-medium text-ochre-200">DEMO MODE ACTIVE</span>
          <span className="text-stone-400">| Phase 02 Frontend Shell</span>
        </div>
        <button
          onClick={() => setIsMinimized(false)}
          className="text-stone-400 hover:text-stone-100 flex items-center gap-1 text-xs"
        >
          <span>Chi tiết</span>
          <ChevronDown className="h-3.5 w-3.5" />
        </button>
      </div>
    );
  }

  return (
    <div className="bg-espresso-900/95 backdrop-blur-md border-b border-ochre-600/40 text-alabaster-50 px-4 py-2.5 text-xs transition-all z-50">
      <div className="max-w-7xl mx-auto flex flex-col md:flex-row md:items-center md:justify-between gap-2">
        <div className="flex items-start md:items-center gap-2.5">
          <div className="p-1 rounded bg-ochre-600/20 text-ochre-200 shrink-0 mt-0.5 md:mt-0">
            <AlertCircle className="h-4 w-4" />
          </div>
          <div>
            <span className="font-semibold text-ochre-200 tracking-wide uppercase text-[11px] mr-2 px-1.5 py-0.5 rounded bg-ochre-600/30 border border-ochre-600/40">
              Frontend Demo
            </span>
            <span className="text-stone-300">
              Giao diện mô phỏng độc lập — Dữ liệu đồng bộ từ Seed Database, hoàn toàn không phụ thuộc SQL Server hay backend API.
            </span>
          </div>
        </div>

        <div className="flex items-center justify-between md:justify-end gap-3 shrink-0 pt-1 md:pt-0 border-t border-stone-800 md:border-t-0">
          {currentUser ? (
            <div className="flex items-center gap-1.5 text-emerald-400 text-[11px] bg-emerald-950/60 px-2 py-0.5 rounded border border-emerald-800/60">
              <UserCheck className="h-3 w-3" />
              <span>{currentUser.fullName} ({currentUser.linkedStatus === 'VERIFIED' ? 'Đã liên kết' : 'Tài khoản mới'})</span>
            </div>
          ) : (
            <div className="flex items-center gap-1.5 text-stone-400 text-[11px]">
              <ShieldAlert className="h-3 w-3 text-stone-500" />
              <span>Chưa đăng nhập (Khách vãng lai)</span>
            </div>
          )}

          <div className="flex items-center gap-1 ml-2">
            <button
              onClick={() => setIsMinimized(true)}
              title="Thu nhỏ thanh thông báo"
              className="p-1 text-stone-400 hover:text-stone-200 transition-colors"
            >
              <ChevronUp className="h-3.5 w-3.5" />
            </button>
            <button
              onClick={() => setIsDismissed(true)}
              title="Đóng thông báo"
              className="p-1 text-stone-400 hover:text-stone-200 transition-colors"
            >
              <X className="h-3.5 w-3.5" />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

