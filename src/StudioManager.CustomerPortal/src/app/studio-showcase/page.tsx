'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import {
  Grid,
  Layers,
  Sparkles,
  Camera,
  ArrowLeft,
  Users,
  Maximize2,
  Palette,
  CheckCircle2,
} from 'lucide-react';
import { portalDataProvider } from '@/lib/mock-provider';
import { ChamberDto, ServiceDto, ResourceDto } from '@/lib/contracts';
import { formatCurrency } from '@/lib/utils';
import { VisualPlaceholder } from '@/components/VisualPlaceholder';

type ShowcaseTab = 'chambers' | 'services' | 'resources';

export default function StudioShowcasePage() {
  const [activeTab, setActiveTab] = useState<ShowcaseTab>('chambers');
  const [chambers, setChambers] = useState<ChamberDto[]>([]);
  const [services, setServices] = useState<ServiceDto[]>([]);
  const [resources, setResources] = useState<ResourceDto[]>([]);

  useEffect(() => {
    portalDataProvider.getChambers().then(setChambers);
    portalDataProvider.getServices().then(setServices);
    portalDataProvider.getResources().then(setResources);
  }, []);

  return (
    <div className="min-h-screen bg-alabaster-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-7xl mx-auto space-y-10">
        {/* Breadcrumb & Header */}
        <div className="space-y-4">
          <Link
            href="/"
            className="inline-flex items-center gap-1.5 text-xs text-stone-500 hover:text-espresso-900 transition-colors"
          >
            <ArrowLeft className="h-3.5 w-3.5" />
            <span>Trang chủ Atelier</span>
          </Link>

          <div>
            <span className="text-xs font-mono uppercase tracking-widest text-terracotta-700 bg-terracotta-50 px-2.5 py-1 rounded border border-terracotta-100">
              CP-PUB-04 — Studio Showcase
            </span>
            <h1 className="font-serif text-3xl sm:text-4xl font-bold text-espresso-900 mt-2">
              Không Gian, Dịch Vụ & Trang Thiết Bị
            </h1>
            <p className="text-sm text-stone-600 mt-1 max-w-2xl font-sans">
              Khám phá toàn bộ cơ sở vật chất, hệ thống máy ảnh full-frame, ánh sáng điện ảnh và danh mục dịch vụ gia tăng chính thức tại Atelier Lumière.
            </p>
          </div>
        </div>

        {/* Tab Selection */}
        <div className="flex items-center gap-3 border-b border-stone-200">
          <button
            onClick={() => setActiveTab('chambers')}
            className={`pb-3 px-2 text-sm font-medium transition-all relative ${
              activeTab === 'chambers'
                ? 'text-terracotta-600 font-semibold'
                : 'text-stone-500 hover:text-espresso-900'
            }`}
          >
            <span>03 Phòng chụp chuẩn Studio (P0001 – P0003)</span>
            {activeTab === 'chambers' && (
              <span className="absolute bottom-0 inset-x-0 h-0.5 bg-terracotta-600 rounded-full" />
            )}
          </button>

          <button
            onClick={() => setActiveTab('services')}
            className={`pb-3 px-2 text-sm font-medium transition-all relative ${
              activeTab === 'services'
                ? 'text-terracotta-600 font-semibold'
                : 'text-stone-500 hover:text-espresso-900'
            }`}
          >
            <span>08 Dịch vụ gia tăng (DV000001 – DV000008)</span>
            {activeTab === 'services' && (
              <span className="absolute bottom-0 inset-x-0 h-0.5 bg-terracotta-600 rounded-full" />
            )}
          </button>

          <button
            onClick={() => setActiveTab('resources')}
            className={`pb-3 px-2 text-sm font-medium transition-all relative ${
              activeTab === 'resources'
                ? 'text-terracotta-600 font-semibold'
                : 'text-stone-500 hover:text-espresso-900'
            }`}
          >
            <span>10 Thiết bị & Phục trang (TN000001 – TN000010)</span>
            {activeTab === 'resources' && (
              <span className="absolute bottom-0 inset-x-0 h-0.5 bg-terracotta-600 rounded-full" />
            )}
          </button>
        </div>

        {/* =========================================================================
            TAB 1: CHAMBERS (P0001..P0003)
            ========================================================================= */}
        {activeTab === 'chambers' && (
          <div className="space-y-12">
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
              {chambers.map((chamber) => (
                <div
                  key={chamber.maPhong}
                  className="bg-alabaster-50 rounded-atelier border border-stone-300 overflow-hidden shadow-sm flex flex-col justify-between"
                >
                  <div>
                    <VisualPlaceholder
                      assetId={chamber.aiAssetId}
                      title={chamber.tenPhong}
                      subtitle={chamber.phongCach}
                      category="chamber"
                      aspectRatio="video"
                    />

                    <div className="p-6 space-y-4">
                      <div className="flex items-center justify-between">
                        <span className="text-xs font-mono font-bold text-terracotta-700 bg-terracotta-50 px-2 py-0.5 rounded border border-terracotta-100">
                          {chamber.maPhong}
                        </span>
                        <div className="flex items-center gap-1.5 text-xs text-stone-600">
                          <Users className="h-3.5 w-3.5 text-stone-400" />
                          <span>Tối đa {chamber.sucChuaToiDa} người</span>
                        </div>
                      </div>

                      <h3 className="font-serif text-xl font-bold text-espresso-900">
                        {chamber.tenPhong}
                      </h3>
                      <p className="text-xs text-stone-600 leading-relaxed">
                        {chamber.moTa}
                      </p>

                      <div className="p-3 bg-alabaster-100 rounded-atelier space-y-2 text-xs border border-stone-200/80">
                        <div className="flex items-center justify-between">
                          <span className="text-stone-500">Diện tích không gian:</span>
                          <span className="font-medium text-espresso-900">{chamber.dienTichM2} m²</span>
                        </div>
                        <div className="flex items-center justify-between">
                          <span className="text-stone-500">Tone màu chủ đạo:</span>
                          <span className="font-medium text-espresso-900 text-[11px]">{chamber.toneMau}</span>
                        </div>
                      </div>

                      <div>
                        <h4 className="text-xs font-semibold text-espresso-800 uppercase tracking-wider mb-2">
                          Hệ thống trang thiết bị cố định:
                        </h4>
                        <ul className="space-y-1.5 text-xs text-stone-600">
                          {chamber.trangThietBiChinh.map((item, idx) => (
                            <li key={idx} className="flex items-start gap-2">
                              <CheckCircle2 className="h-3.5 w-3.5 text-emerald-600 shrink-0 mt-0.5" />
                              <span>{item}</span>
                            </li>
                          ))}
                        </ul>
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* =========================================================================
            TAB 2: ADD-ON SERVICES (DV000001..DV000008)
            ========================================================================= */}
        {activeTab === 'services' && (
          <div className="space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              {services.map((service) => (
                <div
                  key={service.maDichVu}
                  className="bg-alabaster-50 p-6 rounded-atelier border border-stone-200/90 shadow-sm flex flex-col justify-between hover:border-terracotta-300 transition-colors"
                >
                  <div>
                    <div className="flex items-center justify-between mb-3">
                      <span className="text-xs font-mono font-medium text-stone-500">
                        {service.maDichVu}
                      </span>
                      {service.popular && (
                        <span className="text-[10px] bg-terracotta-50 text-terracotta-700 px-2 py-0.5 rounded font-medium border border-terracotta-100">
                          Phổ biến
                        </span>
                      )}
                    </div>

                    <h3 className="font-serif text-base font-bold text-espresso-900">
                      {service.tenDichVu}
                    </h3>
                    <p className="mt-2 text-xs text-stone-600 leading-relaxed">
                      {service.moTa}
                    </p>
                  </div>

                  <div className="mt-6 pt-4 border-t border-stone-200 flex items-baseline justify-between">
                    <span className="text-xs text-stone-500">Đơn giá:</span>
                    <div className="text-right">
                      <span className="font-serif text-lg font-bold text-terracotta-700">
                        {formatCurrency(service.donGia)}
                      </span>
                      <span className="text-xs text-stone-500"> / {service.donViTinh}</span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* =========================================================================
            TAB 3: RESOURCES & WARDROBE (TN000001..TN000010)
            ========================================================================= */}
        {activeTab === 'resources' && (
          <div className="space-y-8">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
              {resources.map((res) => (
                <div
                  key={res.maTaiNguyen}
                  className="bg-alabaster-50 rounded-atelier border border-stone-200/90 overflow-hidden shadow-sm"
                >
                  <VisualPlaceholder
                    assetId={res.aiAssetId}
                    title={res.tenTaiNguyen}
                    subtitle={res.moTa}
                    category="resource"
                    aspectRatio="video"
                  />

                  <div className="p-5 space-y-3">
                    <div className="flex items-center justify-between">
                      <span className="text-xs font-mono font-medium text-stone-500">
                        {res.maTaiNguyen}
                      </span>
                      <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-stone-100 text-stone-700">
                        {res.loaiTaiNguyen}
                      </span>
                    </div>

                    <h3 className="font-serif text-base font-bold text-espresso-900">
                      {res.tenTaiNguyen}
                    </h3>
                    <p className="text-xs text-stone-600 leading-relaxed">
                      {res.moTa}
                    </p>

                    {res.tinhNangChinh && (
                      <div className="pt-2 flex flex-wrap gap-1.5">
                        {res.tinhNangChinh.map((f, i) => (
                          <span
                            key={i}
                            className="text-[10px] px-2 py-0.5 rounded bg-alabaster-100 text-stone-600 border border-stone-200"
                          >
                            {f}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

