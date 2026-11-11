'use client';

import React, { useState, useEffect, useMemo } from 'react';
import Link from 'next/link';
import { Sparkles, Search, Filter, SlidersHorizontal, ArrowLeft } from 'lucide-react';
import { portalDataProvider } from '@/lib/mock-provider';
import { PackageDto, PackageCategory } from '@/lib/contracts';
import { PackageCard } from '@/components/PackageCard';

export default function PackagesDiscoveryPage() {
  const [packages, setPackages] = useState<PackageDto[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<PackageCategory>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [sortBy, setSortBy] = useState<'default' | 'price-asc' | 'price-desc'>('default');

  useEffect(() => {
    portalDataProvider.getPackages().then((data) => {
      setPackages(data);
    });
  }, []);

  const categories: { key: PackageCategory; label: string }[] = [
    { key: 'all', label: 'Tất cả gói chụp (5)' },
    { key: 'portrait', label: 'Chân dung cá nhân' },
    { key: 'family', label: 'Gia đình gắn kết' },
    { key: 'baby', label: 'Thiên thần nhỏ (Em bé)' },
    { key: 'couple', label: 'Cặp đôi & Kỷ niệm' },
    { key: 'event', label: 'Kỷ yếu & Nhóm bạn' },
  ];

  const filteredPackages = useMemo(() => {
    let result = [...packages];

    // Filter by Category
    if (selectedCategory !== 'all') {
      result = result.filter((p) => p.category === selectedCategory);
    }

    // Filter by Search Query
    if (searchQuery.trim()) {
      const q = searchQuery.trim().toLowerCase();
      result = result.filter(
        (p) =>
          p.tenGoi.toLowerCase().includes(q) ||
          p.maGoi.toLowerCase().includes(q) ||
          p.moTa.toLowerCase().includes(q)
      );
    }

    // Sorting
    if (sortBy === 'price-asc') {
      result.sort((a, b) => a.giaCoBan - b.giaCoBan);
    } else if (sortBy === 'price-desc') {
      result.sort((a, b) => b.giaCoBan - a.giaCoBan);
    }

    return result;
  }, [packages, selectedCategory, searchQuery, sortBy]);

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
              CP-PUB-02 — Package Discovery
            </span>
            <h1 className="font-serif text-3xl sm:text-4xl font-bold text-espresso-900 mt-2">
              Bộ Sưu Tập Gói Chụp Nghệ Thuật
            </h1>
            <p className="text-sm text-stone-600 mt-1 max-w-2xl">
              Khám phá đầy đủ 05 gói chụp cơ bản từ kho dữ liệu chuẩn Atelier Lumière (GC000001 – GC000005). Tất cả mức giá đã bao gồm trang thiết bị, phòng chụp và hậu kỳ chuẩn mỹ thuật.
            </p>
          </div>
        </div>

        {/* Filters and Search Bar */}
        <div className="p-4 rounded-atelier bg-alabaster-100 border border-stone-200/80 space-y-4">
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            {/* Search Input */}
            <div className="relative flex-1 max-w-md">
              <Search className="h-4 w-4 text-stone-400 absolute left-3.5 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                placeholder="Tìm gói chụp theo tên hoặc mã (vd: GC000001, Chân dung)..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full pl-10 pr-4 py-2 text-xs rounded-atelier bg-alabaster-50 border border-stone-300 text-espresso-900 focus:outline-none focus:ring-1 focus:ring-terracotta-500 placeholder:text-stone-400"
              />
            </div>

            {/* Sorting selector */}
            <div className="flex items-center gap-2 shrink-0">
              <SlidersHorizontal className="h-4 w-4 text-stone-500" />
              <span className="text-xs text-stone-600 font-medium">Sắp xếp:</span>
              <select
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value as any)}
                className="text-xs py-1.5 px-3 rounded-atelier bg-alabaster-50 border border-stone-300 text-espresso-900 focus:outline-none focus:ring-1 focus:ring-terracotta-500"
              >
                <option value="default">Mặc định (Khuyên dùng)</option>
                <option value="price-asc">Giá từ thấp đến cao</option>
                <option value="price-desc">Giá từ cao đến thấp</option>
              </select>
            </div>
          </div>

          {/* Category Tabs */}
          <div className="flex items-center gap-2 overflow-x-auto pb-1 pt-1 scrollbar-none">
            {categories.map((cat) => {
              const isSelected = selectedCategory === cat.key;
              return (
                <button
                  key={cat.key}
                  onClick={() => setSelectedCategory(cat.key)}
                  className={`px-3.5 py-1.5 rounded-full text-xs font-medium whitespace-nowrap transition-colors ${
                    isSelected
                      ? 'bg-espresso-900 text-alabaster-50 shadow-sm'
                      : 'bg-alabaster-50 text-stone-600 hover:text-espresso-900 border border-stone-300/80 hover:bg-stone-50'
                  }`}
                >
                  {cat.label}
                </button>
              );
            })}
          </div>
        </div>

        {/* Packages Grid */}
        {filteredPackages.length > 0 ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
            {filteredPackages.map((pkg) => (
              <PackageCard key={pkg.maGoi} pkg={pkg} />
            ))}
          </div>
        ) : (
          <div className="text-center py-16 bg-alabaster-100 rounded-atelier border border-dashed border-stone-300 p-8">
            <p className="font-serif text-lg text-espresso-800">Không tìm thấy gói chụp phù hợp</p>
            <p className="text-xs text-stone-500 mt-1">
              Vui lòng thử lại với từ khóa khác hoặc chuyển sang danh mục &ldquo;Tất cả gói chụp&rdquo;.
            </p>
            <button
              onClick={() => {
                setSelectedCategory('all');
                setSearchQuery('');
              }}
              className="mt-4 px-4 py-2 rounded-atelier bg-stone-200 text-espresso-900 text-xs font-medium hover:bg-stone-300 transition-colors"
            >
              Đặt lại bộ lọc
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

