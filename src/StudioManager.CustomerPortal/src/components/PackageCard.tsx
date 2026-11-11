'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { Clock, Image as ImageIcon, Sparkles, ArrowRight, Check } from 'lucide-react';
import { PackageDto } from '@/lib/contracts';
import { formatCurrency, formatDuration } from '@/lib/utils';
import { VisualPlaceholder } from './VisualPlaceholder';
import { BookingPreviewModal } from './BookingPreviewModal';

interface PackageCardProps {
  pkg: PackageDto;
}

export const PackageCard: React.FC<PackageCardProps> = ({ pkg }) => {
  const [isModalOpen, setIsModalOpen] = useState(false);

  return (
    <>
      <div className="flex flex-col bg-alabaster-50 rounded-atelier border border-stone-200/90 overflow-hidden shadow-sm hover:shadow-md transition-all duration-300 group">
        {/* Card Visual Container */}
        <div className="relative">
          <VisualPlaceholder
            assetId={pkg.aiAssetId}
            title={pkg.tenGoi}
            subtitle={pkg.moTa}
            category="package"
            aspectRatio="video"
          />

          {pkg.badgeText && (
            <div className="absolute bottom-3 left-3 z-20">
              <span className="px-2.5 py-1 rounded-full text-[11px] font-medium bg-terracotta-600 text-alabaster-50 shadow-sm border border-terracotta-500">
                {pkg.badgeText}
              </span>
            </div>
          )}
        </div>

        {/* Content Section */}
        <div className="p-6 flex-1 flex flex-col justify-between">
          <div>
            {/* Code & Category Tag */}
            <div className="flex items-center justify-between mb-2">
              <span className="text-xs font-mono font-medium text-stone-500 uppercase tracking-wider">
                {pkg.maGoi}
              </span>
              <span className="text-[11px] font-sans text-terracotta-700 bg-terracotta-50 px-2 py-0.5 rounded-full border border-terracotta-100">
                Cọc {pkg.minDepositPercent}% ({formatCurrency(pkg.giaCoBan * (pkg.minDepositPercent / 100))})
              </span>
            </div>

            {/* Title & Description */}
            <h3 className="font-serif text-xl font-bold text-espresso-900 group-hover:text-terracotta-600 transition-colors">
              <Link href={`/packages/${pkg.maGoi.toLowerCase()}`}>
                {pkg.tenGoi}
              </Link>
            </h3>
            <p className="mt-2 text-xs text-stone-600 line-clamp-2 leading-relaxed">
              {pkg.moTa}
            </p>

            {/* Key Specs Row */}
            <div className="mt-4 grid grid-cols-2 gap-2 py-2.5 px-3 bg-alabaster-100 rounded-atelier text-xs text-espresso-900 border border-stone-200/60">
              <div className="flex items-center gap-1.5">
                <Clock className="h-4 w-4 text-terracotta-600 shrink-0" />
                <span className="font-medium">{formatDuration(pkg.thoiLuongPhut)}</span>
              </div>
              <div className="flex items-center gap-1.5">
                <ImageIcon className="h-4 w-4 text-terracotta-600 shrink-0" />
                <span className="font-medium">{pkg.soAnhChinhSua} ảnh chỉnh sửa</span>
              </div>
            </div>

            {/* Inclusions preview */}
            <ul className="mt-4 space-y-1.5 text-xs text-stone-600">
              {pkg.inclusions.slice(0, 3).map((inc, idx) => (
                <li key={idx} className="flex items-start gap-2">
                  <Check className="h-3.5 w-3.5 text-emerald-600 shrink-0 mt-0.5" />
                  <span className="line-clamp-1">{inc}</span>
                </li>
              ))}
            </ul>
          </div>

          {/* Pricing & Actions */}
          <div className="mt-6 pt-4 border-t border-stone-200 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
            <div>
              <span className="text-[10px] uppercase font-sans text-stone-500 tracking-wider">
                Giá dịch vụ trọn gói
              </span>
              <p className="font-serif text-xl font-bold text-terracotta-700">
                {formatCurrency(pkg.giaCoBan)}
              </p>
            </div>

            <div className="flex items-center gap-2">
              <Link
                href={`/packages/${pkg.maGoi.toLowerCase()}`}
                className="px-3.5 py-2 rounded-atelier text-xs font-medium text-stone-700 hover:text-espresso-900 hover:bg-stone-200/70 border border-stone-300 transition-colors"
              >
                Chi tiết
              </Link>
              <button
                onClick={() => setIsModalOpen(true)}
                className="px-3.5 py-2 rounded-atelier text-xs font-medium bg-terracotta-600 hover:bg-terracotta-700 text-alabaster-50 transition-colors flex items-center gap-1 shadow-sm"
              >
                <span>Đặt lịch</span>
                <ArrowRight className="h-3.5 w-3.5" />
              </button>
            </div>
          </div>
        </div>
      </div>

      <BookingPreviewModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        selectedPackageTitle={pkg.tenGoi}
        selectedPackageCode={pkg.maGoi}
      />
    </>
  );
};

