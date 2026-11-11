'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import {
  ArrowLeft,
  Clock,
  Image as ImageIcon,
  CheckCircle2,
  Sparkles,
  ShieldCheck,
  Calendar,
  Layers,
  ChevronRight,
  Plus,
} from 'lucide-react';
import { portalDataProvider } from '@/lib/mock-provider';
import { PackageDto, ChamberDto, ServiceDto } from '@/lib/contracts';
import { formatCurrency, formatDuration } from '@/lib/utils';
import { VisualPlaceholder } from '@/components/VisualPlaceholder';
import { BookingPreviewModal } from '@/components/BookingPreviewModal';

export default function PackageDetailPage() {
  const params = useParams();
  const router = useRouter();
  const code = (params?.code as string)?.toUpperCase() || '';

  const [pkg, setPkg] = useState<PackageDto | null>(null);
  const [recommendedChamber, setRecommendedChamber] = useState<ChamberDto | null>(null);
  const [recommendedAddons, setRecommendedAddons] = useState<ServiceDto[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadData() {
      setLoading(true);
      const packageData = await portalDataProvider.getPackageByCode(code);
      if (packageData) {
        setPkg(packageData);

        // Load recommended chamber
        if (packageData.recommendedChamberCode) {
          const ch = await portalDataProvider.getChamberByCode(packageData.recommendedChamberCode);
          setRecommendedChamber(ch);
        }

        // Load recommended add-ons
        const allServices = await portalDataProvider.getServices();
        const addons = allServices.filter((s) =>
          packageData.recommendedAddonCodes.includes(s.maDichVu)
        );
        setRecommendedAddons(addons);
      }
      setLoading(false);
    }

    if (code) {
      loadData();
    }
  }, [code]);

  if (loading) {
    return (
      <div className="min-h-screen bg-alabaster-50 flex items-center justify-center p-6">
        <div className="flex flex-col items-center gap-3">
          <div className="h-8 w-8 rounded-full border-2 border-terracotta-600 border-t-transparent animate-spin" />
          <p className="text-xs text-stone-500 font-mono">Đang tải thông tin gói chụp...</p>
        </div>
      </div>
    );
  }

  if (!pkg) {
    return (
      <div className="min-h-screen bg-alabaster-50 py-16 px-4">
        <div className="max-w-md mx-auto text-center space-y-4">
          <h2 className="font-serif text-2xl font-bold text-espresso-900">
            Không tìm thấy gói chụp
          </h2>
          <p className="text-xs text-stone-600">
            Mã gói chụp <code className="font-mono bg-stone-100 px-1 py-0.5 rounded">{code}</code> không tồn tại trong hệ thống seed database Atelier Lumière.
          </p>
          <Link
            href="/packages"
            className="inline-flex items-center gap-2 px-4 py-2 rounded-atelier bg-terracotta-600 text-alabaster-50 text-xs font-medium"
          >
            <ArrowLeft className="h-4 w-4" />
            <span>Quay lại danh sách gói chụp</span>
          </Link>
        </div>
      </div>
    );
  }

  const depositAmount = pkg.giaCoBan * (pkg.minDepositPercent / 100);

  return (
    <div className="min-h-screen bg-alabaster-50 py-10 px-4 sm:px-6 lg:px-8">
      <div className="max-w-7xl mx-auto space-y-12">
        {/* Navigation Breadcrumb */}
        <div className="flex items-center justify-between border-b border-stone-200 pb-4">
          <Link
            href="/packages"
            className="inline-flex items-center gap-2 text-xs font-medium text-stone-500 hover:text-espresso-900 transition-colors"
          >
            <ArrowLeft className="h-4 w-4" />
            <span>Quay lại danh mục Gói chụp</span>
          </Link>
          <div className="flex items-center gap-2 text-xs font-mono text-stone-400">
            <span>CP-PUB-03</span>
            <span>•</span>
            <span className="text-terracotta-600 font-semibold">{pkg.maGoi}</span>
          </div>
        </div>

        {/* Hero Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-10 items-start">
          {/* Left Column: Visual Container */}
          <div className="lg:col-span-7 space-y-4">
            <VisualPlaceholder
              assetId={pkg.aiAssetId}
              title={pkg.tenGoi}
              subtitle={pkg.moTa}
              category="package"
              aspectRatio="video"
              className="shadow-xl"
            />

            {/* Quick Specs Badges */}
            <div className="grid grid-cols-3 gap-3 p-4 rounded-atelier bg-alabaster-100 border border-stone-200/80 text-xs">
              <div className="flex flex-col">
                <span className="text-stone-500 text-[11px]">Thời lượng chụp</span>
                <span className="font-semibold text-espresso-900 mt-0.5 flex items-center gap-1">
                  <Clock className="h-3.5 w-3.5 text-terracotta-600" />
                  {formatDuration(pkg.thoiLuongPhut)}
                </span>
              </div>
              <div className="flex flex-col">
                <span className="text-stone-500 text-[11px]">Ảnh chỉnh sửa</span>
                <span className="font-semibold text-espresso-900 mt-0.5 flex items-center gap-1">
                  <ImageIcon className="h-3.5 w-3.5 text-terracotta-600" />
                  {pkg.soAnhChinhSua} ảnh hoàn thiện
                </span>
              </div>
              <div className="flex flex-col">
                <span className="text-stone-500 text-[11px]">Mức cọc tiêu chuẩn</span>
                <span className="font-semibold text-espresso-900 mt-0.5 flex items-center gap-1">
                  <Sparkles className="h-3.5 w-3.5 text-terracotta-600" />
                  {pkg.minDepositPercent}% giá trị gói
                </span>
              </div>
            </div>
          </div>

          {/* Right Column: Pricing & Quick Booking Dossier */}
          <div className="lg:col-span-5 bg-alabaster-100/90 rounded-atelier-lg border border-stone-300 p-6 sm:p-8 space-y-6 shadow-sm">
            <div>
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-mono font-medium text-stone-500 uppercase tracking-widest">
                  {pkg.maGoi}
                </span>
                {pkg.badgeText && (
                  <span className="text-[11px] font-sans text-terracotta-700 bg-terracotta-50 px-2 py-0.5 rounded-full border border-terracotta-100">
                    {pkg.badgeText}
                  </span>
                )}
              </div>
              <h1 className="font-serif text-3xl font-bold text-espresso-900">
                {pkg.tenGoi}
              </h1>
              <p className="mt-2 text-xs text-stone-600 leading-relaxed font-sans">
                {pkg.moTa}
              </p>
            </div>

            {/* Price block */}
            <div className="p-4 rounded-atelier bg-alabaster-50 border border-stone-200">
              <span className="text-[11px] uppercase tracking-wider text-stone-500 font-sans">
                Chi phí trọn gói dịch vụ
              </span>
              <div className="flex items-baseline gap-2 mt-1">
                <span className="font-serif text-3xl font-bold text-terracotta-700">
                  {formatCurrency(pkg.giaCoBan)}
                </span>
                <span className="text-xs text-stone-500">/ buổi chụp</span>
              </div>
              <p className="mt-2 text-[11px] text-stone-500">
                Mức cọc thanh toán trước (VietQR):{' '}
                <strong className="text-espresso-900">{formatCurrency(depositAmount)}</strong>{' '}
                ({pkg.minDepositPercent}%). Số tiền còn lại thanh toán sau khi hoàn thành buổi chụp.
              </p>
            </div>

            {/* Action CTA */}
            <div className="space-y-2.5 pt-2">
              <button
                onClick={() => setIsModalOpen(true)}
                className="w-full py-3.5 px-6 rounded-atelier bg-terracotta-600 hover:bg-terracotta-700 text-alabaster-50 font-medium text-sm transition-colors flex items-center justify-center gap-2 shadow-md"
              >
                <Calendar className="h-4 w-4" />
                <span>Đặt lịch gói chụp này</span>
              </button>
              <p className="text-[11px] text-center text-stone-400">
                Bản thử nghiệm giao diện Phase 02 • Quy trình đặt cọc trực tuyến tại Phase 03
              </p>
            </div>

            {/* Trust highlights */}
            <div className="pt-4 border-t border-stone-200 text-xs text-stone-600 space-y-2">
              <div className="flex items-center gap-2">
                <ShieldCheck className="h-4 w-4 text-emerald-600 shrink-0" />
                <span>Bàn giao toàn bộ file ảnh gốc độ phân giải 4K</span>
              </div>
              <div className="flex items-center gap-2">
                <ShieldCheck className="h-4 w-4 text-emerald-600 shrink-0" />
                <span>Bảo lưu lịch chụp miễn phí trước 48 giờ</span>
              </div>
            </div>
          </div>
        </div>

        {/* Detailed Sections: Inclusions, Recommended Chamber & Add-ons */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-10">
          {/* Detailed Inclusions & Highlights */}
          <div className="lg:col-span-7 space-y-8">
            {/* Inclusions */}
            <div className="bg-alabaster-50 p-6 rounded-atelier border border-stone-200">
              <h3 className="font-serif text-xl font-bold text-espresso-900 mb-4 flex items-center gap-2">
                <Sparkles className="h-5 w-5 text-terracotta-600" />
                <span>Chi tiết dịch vụ bao gồm trong gói</span>
              </h3>
              <ul className="space-y-3 text-xs sm:text-sm text-stone-700">
                {pkg.inclusions.map((item, idx) => (
                  <li key={idx} className="flex items-start gap-2.5">
                    <CheckCircle2 className="h-4 w-4 text-emerald-600 shrink-0 mt-0.5" />
                    <span>{item}</span>
                  </li>
                ))}
              </ul>
            </div>

            {/* Highlights */}
            <div className="bg-alabaster-100 p-6 rounded-atelier border border-stone-200/80">
              <h3 className="font-serif text-lg font-bold text-espresso-900 mb-3">
                Đặc quyền & Điểm nổi bật
              </h3>
              <ul className="space-y-2 text-xs sm:text-sm text-stone-600">
                {pkg.highlights.map((item, idx) => (
                  <li key={idx} className="flex items-start gap-2">
                    <span className="h-1.5 w-1.5 rounded-full bg-terracotta-500 shrink-0 mt-2" />
                    <span>{item}</span>
                  </li>
                ))}
              </ul>
            </div>
          </div>

          {/* Recommended Chamber & Add-on Suggestions */}
          <div className="lg:col-span-5 space-y-8">
            {/* Recommended Chamber Card */}
            {recommendedChamber && (
              <div className="bg-alabaster-50 p-6 rounded-atelier border border-stone-200 space-y-4">
                <div className="flex items-center justify-between">
                  <span className="text-[11px] font-mono uppercase tracking-wider text-stone-500">
                    Phòng chụp khuyên dùng
                  </span>
                  <span className="text-xs font-mono font-medium text-terracotta-600">
                    {recommendedChamber.maPhong}
                  </span>
                </div>

                <VisualPlaceholder
                  assetId={recommendedChamber.aiAssetId}
                  title={recommendedChamber.tenPhong}
                  subtitle={recommendedChamber.phongCach}
                  category="chamber"
                  aspectRatio="video"
                />

                <div>
                  <h4 className="font-serif text-base font-bold text-espresso-900">
                    {recommendedChamber.tenPhong}
                  </h4>
                  <p className="text-xs text-stone-600 mt-1 leading-relaxed">
                    {recommendedChamber.moTa}
                  </p>
                </div>

                <Link
                  href="/studio-showcase"
                  className="inline-flex items-center gap-1.5 text-xs font-medium text-terracotta-700 hover:text-terracotta-800"
                >
                  <span>Khám phá 3 phòng chụp tại Studio</span>
                  <ChevronRight className="h-3.5 w-3.5" />
                </Link>
              </div>
            )}

            {/* Recommended Add-ons */}
            {recommendedAddons.length > 0 && (
              <div className="bg-alabaster-50 p-6 rounded-atelier border border-stone-200 space-y-3">
                <div className="flex items-center justify-between">
                  <h4 className="font-serif text-base font-bold text-espresso-900">
                    Dịch vụ gia tăng gợi ý
                  </h4>
                  <span className="text-[10px] font-mono text-stone-400">DV000001..08</span>
                </div>

                <div className="space-y-2.5">
                  {recommendedAddons.map((addon) => (
                    <div
                      key={addon.maDichVu}
                      className="p-3 rounded-atelier bg-alabaster-100 border border-stone-200/80 flex items-center justify-between gap-3 text-xs"
                    >
                      <div>
                        <p className="font-medium text-espresso-900">{addon.tenDichVu}</p>
                        <p className="text-[11px] text-stone-500 mt-0.5 line-clamp-1">{addon.moTa}</p>
                      </div>
                      <div className="text-right shrink-0">
                        <span className="font-semibold text-terracotta-700">
                          {formatCurrency(addon.donGia)}
                        </span>
                        <span className="text-[10px] text-stone-500 block">/{addon.donViTinh}</span>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Booking Preview Modal */}
      <BookingPreviewModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        selectedPackageTitle={pkg.tenGoi}
        selectedPackageCode={pkg.maGoi}
      />
    </div>
  );
}

