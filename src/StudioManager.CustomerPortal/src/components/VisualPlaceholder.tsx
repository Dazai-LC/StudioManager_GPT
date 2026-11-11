'use client';

import React from 'react';
import { Camera, Sparkles, Image as ImageIcon } from 'lucide-react';

interface VisualPlaceholderProps {
  assetId: string;
  title: string;
  subtitle?: string;
  category?: 'package' | 'chamber' | 'resource' | 'auth-bg' | 'fallback';
  aspectRatio?: 'video' | 'square' | 'portrait' | 'wide' | 'fill';
  className?: string;
}

export const VisualPlaceholder: React.FC<VisualPlaceholderProps> = ({
  assetId,
  title,
  subtitle,
  category = 'package',
  aspectRatio = 'video',
  className = '',
}) => {
  const aspectClass = {
    video: 'aspect-[16/10]',
    square: 'aspect-square',
    portrait: 'aspect-[3/4]',
    wide: 'aspect-[21/9]',
    fill: 'h-full w-full min-h-[300px]',
  }[aspectRatio];

  // Specific gradient palettes aligned with Atelier Lumière Warm Editorial theme
  const gradientClass = {
    package: 'from-[#24211E] via-[#352D26] to-[#1C1815]',
    chamber: 'from-[#2C241E] via-[#43352B] to-[#1F1915]',
    resource: 'from-[#1E2328] via-[#2A3138] to-[#161A1E]',
    'auth-bg': 'from-[#1A1614] via-[#2D241E] to-[#120F0D]',
    fallback: 'from-[#2D2A26] via-[#3C3732] to-[#1D1B19]',
  }[category];

  return (
    <div
      className={`relative overflow-hidden rounded-atelier border border-stone-300/40 bg-gradient-to-br ${gradientClass} text-alabaster-50 shadow-sm ${aspectClass} ${className} group`}
    >
      {/* Decorative ambient lighting effect */}
      <div className="absolute -top-12 -right-12 h-44 w-44 rounded-full bg-terracotta-500/15 blur-2xl transition-all duration-700 group-hover:scale-125" />
      <div className="absolute -bottom-12 -left-12 h-44 w-44 rounded-full bg-ochre-600/10 blur-2xl transition-all duration-700 group-hover:scale-125" />

      {/* Grid texture overlay */}
      <div
        className="absolute inset-0 opacity-[0.06] pointer-events-none"
        style={{
          backgroundImage:
            'radial-gradient(circle at 1px 1px, #FAF8F5 1px, transparent 0)',
          backgroundSize: '24px 24px',
        }}
      />

      {/* Asset Identifier Badge */}
      <div className="absolute top-3 left-3 z-10 flex items-center gap-1.5 rounded-full bg-espresso-900/80 px-2.5 py-1 text-[11px] font-mono tracking-wider text-amber-200/90 backdrop-blur-md border border-amber-200/20">
        <Sparkles className="h-3 w-3 text-terracotta-500" />
        <span>{assetId}</span>
      </div>

      {/* Category Indicator */}
      <div className="absolute top-3 right-3 z-10 rounded-full bg-white/10 px-2.5 py-1 text-[10px] font-sans uppercase tracking-widest text-stone-300 backdrop-blur-md">
        {category === 'auth-bg' ? 'Background Editorial' : category === 'chamber' ? 'Không gian' : category === 'resource' ? 'Thiết bị & Phục trang' : 'Gói chụp'}
      </div>

      {/* Center Camera Icon / Vignette */}
      <div className="absolute inset-0 flex flex-col items-center justify-center p-6 text-center">
        <div className="relative mb-3 flex h-14 w-14 items-center justify-center rounded-full bg-alabaster-50/10 backdrop-blur-md border border-white/10 transition-transform duration-500 group-hover:scale-110">
          <Camera className="h-6 w-6 text-terracotta-500" />
        </div>
        <h4 className="font-serif text-lg font-medium text-alabaster-50 drop-shadow-sm max-w-[90%] line-clamp-2">
          {title}
        </h4>
        {subtitle && (
          <p className="mt-1 text-xs text-stone-300 max-w-[85%] line-clamp-2 font-sans font-light">
            {subtitle}
          </p>
        )}
      </div>

      {/* Bottom Editorial Caption */}
      <div className="absolute bottom-0 inset-x-0 bg-gradient-to-t from-espresso-900/90 via-espresso-900/50 to-transparent p-3 pt-6 flex items-center justify-between text-[11px] text-stone-400">
        <span className="flex items-center gap-1">
          <ImageIcon className="h-3 w-3 text-stone-400" />
          <span>Atelier Lumière Asset</span>
        </span>
        <span className="italic font-serif text-stone-400">Phase 06 Visual System</span>
      </div>
    </div>
  );
};

