'use client';

import React from 'react';
import Link from 'next/link';
import { ArrowLeft, Sparkles, ShieldCheck } from 'lucide-react';
import { VisualPlaceholder } from './VisualPlaceholder';

interface PortalAuthLayoutProps {
  children: React.ReactNode;
  title: string;
  subtitle: string;
  bgAssetId: string; // e.g. "BG-AUTH-02", "BG-AUTH-03", "BG-AUTH-04"
  bgTitle: string;
  bgSubtitle: string;
}

export const PortalAuthLayout: React.FC<PortalAuthLayoutProps> = ({
  children,
  title,
  subtitle,
  bgAssetId,
  bgTitle,
  bgSubtitle,
}) => {
  return (
    <div className="min-h-screen bg-alabaster-100 flex flex-col justify-center py-10 px-4 sm:px-6 lg:px-8">
      {/* Return Home Link */}
      <div className="max-w-5xl mx-auto w-full mb-6 flex items-center justify-between">
        <Link
          href="/"
          className="inline-flex items-center gap-2 text-xs font-medium text-stone-500 hover:text-espresso-900 transition-colors"
        >
          <ArrowLeft className="h-4 w-4" />
          <span>Quay lại Trang chủ Atelier Lumière</span>
        </Link>
        <span className="text-xs font-mono text-stone-400">DEC-03 Customer Identity Isolation</span>
      </div>

      {/* Main Container */}
      <div className="max-w-5xl mx-auto w-full bg-alabaster-50 rounded-atelier-lg border border-stone-300 shadow-xl overflow-hidden grid grid-cols-1 lg:grid-cols-12 min-h-[640px]">
        {/* Left Column: IMG-02 Visual Container (Hidden on small mobile, visible on desktop/tablet) */}
        <div className="hidden lg:flex lg:col-span-5 bg-espresso-900 text-alabaster-50 flex-col justify-between p-8 relative overflow-hidden">
          {/* Visual Asset Container */}
          <div className="relative z-10 space-y-4">
            <div className="inline-flex items-center gap-2 px-2.5 py-1 rounded-full bg-alabaster-50/10 border border-white/10 text-stone-300 text-xs font-mono">
              <Sparkles className="h-3.5 w-3.5 text-terracotta-500" />
              <span>{bgAssetId}</span>
            </div>

            <h2 className="font-serif text-3xl font-medium tracking-tight text-alabaster-50 leading-snug">
              {bgTitle}
            </h2>

            <p className="text-stone-300 text-sm font-light leading-relaxed">
              {bgSubtitle}
            </p>
          </div>

          {/* Center Visual Mock Graphic */}
          <div className="relative z-10 my-6">
            <VisualPlaceholder
              assetId={bgAssetId}
              title={bgTitle}
              subtitle={bgSubtitle}
              category="auth-bg"
              aspectRatio="square"
              className="max-w-[320px] mx-auto shadow-2xl border-white/10"
            />
          </div>

          {/* Bottom Security / Brand Footer */}
          <div className="relative z-10 pt-4 border-t border-stone-800 text-xs text-stone-400 flex items-center justify-between">
            <span className="flex items-center gap-1.5">
              <ShieldCheck className="h-4 w-4 text-emerald-500" />
              <span>Bảo mật danh tính khách hàng</span>
            </span>
            <span className="font-serif italic text-stone-400">Atelier Lumière</span>
          </div>

          {/* Decorative ambient lights */}
          <div className="absolute top-0 right-0 -mr-16 -mt-16 w-64 h-64 rounded-full bg-terracotta-500/10 blur-3xl pointer-events-none" />
          <div className="absolute bottom-0 left-0 -ml-16 -mb-16 w-64 h-64 rounded-full bg-ochre-600/10 blur-3xl pointer-events-none" />
        </div>

        {/* Right Column: Interactive Form */}
        <div className="lg:col-span-7 p-6 sm:p-10 flex flex-col justify-between">
          <div>
            <div className="mb-6">
              <span className="text-[11px] font-mono uppercase tracking-widest text-terracotta-700 bg-terracotta-50 px-2 py-0.5 rounded border border-terracotta-100">
                Atelier Lumière Customer Portal
              </span>
              <h1 className="font-serif text-2xl sm:text-3xl font-semibold text-espresso-900 mt-2">
                {title}
              </h1>
              <p className="mt-1 text-sm text-stone-500 font-sans">
                {subtitle}
              </p>
            </div>

            {children}
          </div>

          <div className="mt-8 pt-4 border-t border-stone-200/60 text-center text-xs text-stone-400">
            <span>© 2026 Atelier Lumière. Bản quyền thuộc về Hệ thống Quản trị StudioManager v2.2.</span>
          </div>
        </div>
      </div>
    </div>
  );
};

