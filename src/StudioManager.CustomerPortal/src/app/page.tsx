'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import {
  Sparkles,
  Camera,
  ArrowRight,
  ShieldCheck,
  Clock,
  Heart,
  Layers,
  ChevronRight,
  CheckCircle2,
} from 'lucide-react';
import { portalDataProvider } from '@/lib/mock-provider';
import { PackageDto, ChamberDto } from '@/lib/contracts';
import { PackageCard } from '@/components/PackageCard';
import { VisualPlaceholder } from '@/components/VisualPlaceholder';
import { BookingPreviewModal } from '@/components/BookingPreviewModal';

export default function HomePage() {
  const [packages, setPackages] = useState<PackageDto[]>([]);
  const [chambers, setChambers] = useState<ChamberDto[]>([]);
  const [isBookingModalOpen, setIsBookingModalOpen] = useState(false);

  useEffect(() => {
    portalDataProvider.getPackages().then((data) => {
      // Filter featured packages for homepage
      setPackages(data.filter((p) => p.featured));
    });
    portalDataProvider.getChambers().then((data) => {
      setChambers(data);
    });
  }, []);

  return (
    <div className="space-y-24">
      {/* =========================================================================
          HERO SECTION (Editorial Atmosphere, Cinematic Warm Minimalist)
          ========================================================================= */}
      <section className="relative bg-gradient-to-b from-alabaster-100 via-alabaster-50 to-alabaster-50 pt-16 pb-20 md:pt-24 md:pb-28 border-b border-stone-200/60 overflow-hidden">
        {/* Ambient background glows */}
        <div className="absolute top-1/4 -left-20 w-96 h-96 rounded-full bg-terracotta-500/10 blur-3xl pointer-events-none" />
        <div className="absolute top-1/3 -right-20 w-96 h-96 rounded-full bg-ochre-600/10 blur-3xl pointer-events-none" />

        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 relative z-10">
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-12 items-center">
            {/* Left Content */}
            <div className="lg:col-span-7 space-y-6 text-center lg:text-left">
              <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full bg-terracotta-50 border border-terracotta-200/80 text-terracotta-800 text-xs font-mono tracking-wide">
                <Sparkles className="h-3.5 w-3.5 text-terracotta-600" />
                <span>ATELIER LUMIÈRE — HAUTE PHOTOGRAPHIE</span>
              </div>

              <h1 className="font-serif text-4xl sm:text-5xl lg:text-6xl font-bold tracking-tight text-espresso-900 leading-[1.15]">
                Ánh sáng nghệ thuật, <br />
                <span className="italic font-normal text-terracotta-600">
                  kể câu chuyện của bạn.
                </span>
              </h1>

              <p className="text-base sm:text-lg text-stone-600 font-sans font-light leading-relaxed max-w-2xl mx-auto lg:mx-0">
                Studio nhiếp ảnh duy mỹ kết hợp phong cách Warm Editorial và ánh sáng điện ảnh đương đại. Nơi từng khung hình là một tác phẩm nghệ thuật tôn vinh cảm xúc tự nhiên, vĩnh cửu.
              </p>

              {/* Action Buttons */}
              <div className="flex flex-col sm:flex-row items-center justify-center lg:justify-start gap-4 pt-4">
                <button
                  onClick={() => setIsBookingModalOpen(true)}
                  className="w-full sm:w-auto px-8 py-3.5 rounded-atelier bg-terracotta-600 hover:bg-terracotta-700 text-alabaster-50 font-medium text-sm shadow-md hover:shadow-lg transition-all flex items-center justify-center gap-2"
                >
                  <Camera className="h-4 w-4" />
                  <span>Đặt lịch trải nghiệm</span>
                </button>

                <Link
                  href="/packages"
                  className="w-full sm:w-auto px-8 py-3.5 rounded-atelier bg-alabaster-50 hover:bg-stone-100 text-espresso-900 font-medium text-sm border border-stone-300 transition-all flex items-center justify-center gap-2"
                >
                  <span>Khám phá 5 gói chụp</span>
                  <ArrowRight className="h-4 w-4 text-stone-500" />
                </Link>
              </div>

              {/* Trust Indicators */}
              <div className="pt-6 grid grid-cols-3 gap-4 border-t border-stone-200/70 text-xs text-stone-600 max-w-lg mx-auto lg:mx-0">
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="h-4 w-4 text-emerald-600 shrink-0" />
                  <span>3 Phòng chụp chuẩn</span>
                </div>
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="h-4 w-4 text-emerald-600 shrink-0" />
                  <span>Máy ảnh Full-frame R6/A7</span>
                </div>
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="h-4 w-4 text-emerald-600 shrink-0" />
                  <span>Hỗ trợ trả góp / Cọc 30%</span>
                </div>
              </div>
            </div>

            {/* Right Visual Hero Container (IMG-02 Showcase) */}
            <div className="lg:col-span-5 relative">
              <div className="relative mx-auto max-w-md lg:max-w-none">
                <VisualPlaceholder
                  assetId="AI-AST-01"
                  title="Chân dung nghệ thuật cá nhân"
                  subtitle="Ánh sáng chuẩn điện ảnh Atelier Lumière"
                  category="package"
                  aspectRatio="portrait"
                  className="shadow-2xl border-stone-300"
                />

                {/* Floating Aesthetic Card */}
                <div className="absolute -bottom-6 -left-6 bg-alabaster-50/95 backdrop-blur-md p-4 rounded-atelier border border-stone-300/80 shadow-lg text-xs max-w-[240px] hidden sm:block">
                  <div className="flex items-center gap-2 text-terracotta-600 font-semibold mb-1">
                    <Sparkles className="h-4 w-4" />
                    <span>Lumière Concept</span>
                  </div>
                  <p className="text-stone-600 text-[11px] leading-snug">
                    Không gian vòm kính 4m đón trọn ánh sáng tự nhiên tinh khiết.
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* =========================================================================
          FEATURED PACKAGES SECTION (GC000001, GC000002, GC000004)
          ========================================================================= */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col md:flex-row md:items-end justify-between mb-12">
          <div>
            <span className="text-xs font-mono uppercase tracking-widest text-terracotta-700 bg-terracotta-50 px-2.5 py-1 rounded border border-terracotta-100">
              Gói dịch vụ tiêu biểu
            </span>
            <h2 className="font-serif text-3xl sm:text-4xl font-bold text-espresso-900 mt-3">
              Bộ sưu tập dịch vụ nhiếp ảnh
            </h2>
            <p className="text-sm text-stone-600 mt-2 max-w-xl">
              Được thiết kế tỉ mỉ từ thời lượng chụp, số lượng ảnh hậu kỳ đến sản phẩm in ấn cao cấp, mang lại trải nghiệm trọn vẹn nhất.
            </p>
          </div>

          <Link
            href="/packages"
            className="mt-4 md:mt-0 inline-flex items-center gap-1.5 text-sm font-medium text-terracotta-700 hover:text-terracotta-800 transition-colors"
          >
            <span>Xem toàn bộ 5 gói chụp</span>
            <ChevronRight className="h-4 w-4" />
          </Link>
        </div>

        {/* 3 Featured Packages Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
          {packages.map((pkg) => (
            <PackageCard key={pkg.maGoi} pkg={pkg} />
          ))}
        </div>
      </section>

      {/* =========================================================================
          STUDIO CHAMBERS SHOWCASE (P0001, P0002, P0003)
          ========================================================================= */}
      <section className="bg-alabaster-100 py-20 border-y border-stone-200/80">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center max-w-2xl mx-auto mb-16">
            <span className="text-xs font-mono uppercase tracking-widest text-terracotta-700 bg-terracotta-50 px-2.5 py-1 rounded border border-terracotta-100">
              Không gian sáng tạo
            </span>
            <h2 className="font-serif text-3xl sm:text-4xl font-bold text-espresso-900 mt-3">
              3 Phòng chụp được thiết kế riêng biệt
            </h2>
            <p className="text-sm text-stone-600 mt-2 font-sans font-light">
              Mỗi căn phòng mang một phong cách và hệ thống ánh sáng chuyên biệt, sẵn sàng phục vụ đa dạng nhu cầu từ chân dung nghệ thuật đến ảnh gia đình và trẻ nhỏ.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {chambers.map((chamber) => (
              <div
                key={chamber.maPhong}
                className="bg-alabaster-50 rounded-atelier border border-stone-300 overflow-hidden shadow-sm hover:shadow-md transition-shadow group flex flex-col justify-between"
              >
                <div>
                  <VisualPlaceholder
                    assetId={chamber.aiAssetId}
                    title={chamber.tenPhong}
                    subtitle={chamber.phongCach}
                    category="chamber"
                    aspectRatio="video"
                  />
                  <div className="p-6">
                    <div className="flex items-center justify-between mb-2">
                      <span className="text-xs font-mono font-medium text-stone-500">
                        {chamber.maPhong}
                      </span>
                      <span className="text-[11px] font-sans text-stone-600 bg-stone-100 px-2 py-0.5 rounded">
                        Sức chứa tối đa: {chamber.sucChuaToiDa} người
                      </span>
                    </div>

                    <h3 className="font-serif text-lg font-bold text-espresso-900 group-hover:text-terracotta-600 transition-colors">
                      {chamber.tenPhong}
                    </h3>
                    <p className="mt-2 text-xs text-stone-600 leading-relaxed">
                      {chamber.moTa}
                    </p>

                    <div className="mt-4 pt-3 border-t border-stone-200/70">
                      <span className="text-[11px] font-medium text-espresso-800">
                        Thiết bị chính:
                      </span>
                      <div className="flex flex-wrap gap-1.5 mt-1.5">
                        {chamber.trangThietBiChinh.map((item, idx) => (
                          <span
                            key={idx}
                            className="text-[10px] px-2 py-0.5 rounded bg-alabaster-100 text-stone-700 border border-stone-200"
                          >
                            {item}
                          </span>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>

                <div className="p-6 pt-0">
                  <Link
                    href="/studio-showcase"
                    className="w-full inline-flex items-center justify-center gap-1.5 px-4 py-2 rounded-atelier border border-stone-300 text-xs font-medium text-stone-700 hover:text-espresso-900 hover:bg-stone-100 transition-colors"
                  >
                    <span>Xem chi tiết không gian</span>
                    <ArrowRight className="h-3.5 w-3.5" />
                  </Link>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* =========================================================================
          BRAND PHILOSOPHY & TRUST MANIFESTO
          ========================================================================= */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 pb-12">
        <div className="bg-espresso-900 rounded-atelier-lg text-alabaster-50 p-8 sm:p-14 relative overflow-hidden">
          <div className="relative z-10 grid grid-cols-1 lg:grid-cols-12 gap-8 items-center">
            <div className="lg:col-span-8 space-y-4">
              <span className="text-xs font-mono uppercase tracking-widest text-amber-200/90 bg-amber-200/10 px-2.5 py-1 rounded border border-amber-200/20">
                Atelier Lumière Manifesto
              </span>
              <h2 className="font-serif text-3xl sm:text-4xl font-bold leading-tight">
                Mỗi bức ảnh là một di sản cảm xúc, <br />
                không đơn thuần là một tệp dữ liệu số.
              </h2>
              <p className="text-sm text-stone-300 font-light leading-relaxed max-w-2xl">
                Chúng tôi trân trọng từng khoảnh khắc xuất thần của bạn. Quy trình làm việc chuyên nghiệp, hợp đồng minh bạch, thiết bị chuẩn điện ảnh và hệ thống quản trị StudioManager hiện đại bảo đảm bộ ảnh của bạn được lưu trữ an toàn trọn đời.
              </p>
            </div>

            <div className="lg:col-span-4 flex flex-col sm:flex-row lg:flex-col gap-3 justify-center">
              <Link
                href="/register"
                className="px-6 py-3.5 rounded-atelier bg-terracotta-600 hover:bg-terracotta-700 text-alabaster-50 text-center font-medium text-sm transition-colors shadow-md"
              >
                Đăng ký tài khoản Cổng Khách Hàng
              </Link>
              <Link
                href="/about"
                className="px-6 py-3.5 rounded-atelier bg-alabaster-50/10 hover:bg-alabaster-50/20 text-alabaster-50 text-center font-medium text-sm border border-white/20 transition-colors"
              >
                Tìm hiểu thêm về Atelier
              </Link>
            </div>
          </div>

          {/* Ambient lighting inside dark card */}
          <div className="absolute top-0 right-0 -mr-16 -mt-16 w-80 h-80 rounded-full bg-terracotta-500/15 blur-3xl pointer-events-none" />
        </div>
      </section>

      {/* Booking Preview Modal */}
      <BookingPreviewModal
        isOpen={isBookingModalOpen}
        onClose={() => setIsBookingModalOpen(false)}
      />
    </div>
  );
}

